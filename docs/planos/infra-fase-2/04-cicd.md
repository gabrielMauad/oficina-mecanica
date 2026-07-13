# 04 — CI/CD (GitHub Actions)

> Pré-requisito de leitura: `00`, `01`, `03`.

Objetivo: pipeline que roda **de ponta a ponta no GitHub Actions** e cumpre as etapas exigidas:
build da aplicação → testes → build da imagem Docker → deploy no cluster Kubernetes → deploy do
banco → aplicação dos manifestos. O deploy usa um **cluster kind efêmero criado dentro do
runner GitHub-hosted** via Terraform (mesmo `/infra` do `03`).

> **Por que funciona sem runner self-hosted:** o cluster kind é criado como containers Docker
> **dentro do próprio runner** (o Docker já vem instalado no `ubuntu-latest`). O cluster vive no
> runner, então não há problema de rede. Ao fim do job o runner é destruído junto com o cluster
> — é o modelo efêmero que o fórum aceita; o que é avaliado é o **histórico verde** da execução.

> **Providers Terraform são autocontidos no CI:** `tehcyx/kind`, `gavinbunney/kubectl` e
> `hashicorp/helm` falam com Docker/API/Helm via bibliotecas — **não** exigem os binários `kind`,
> `kubectl` ou `helm` instalados. O runner só precisa de **Terraform + Docker** (Docker já está
> presente). `kubectl` também já vem no `ubuntu-latest` e é usado só para o smoke test.

## 1. Secrets do repositório (criar antes de rodar)

Em **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Valor |
|---|---|
| `DOCKERHUB_USERNAME` | seu usuário do Docker Hub |
| `DOCKERHUB_TOKEN` | um **Access Token** do Docker Hub (Account Settings → Security → New Access Token), não a senha |

Crie no Docker Hub um repositório público chamado `oficina-mecanica-api`.

> Os segredos da aplicação (JWT, senha do banco) estão no manifesto `k8s/base/02-secret.yaml`
> (via `stringData`) e são aplicados pelo Terraform — não precisam ser injetados pelo CI.
> Documente essa escolha (e a ressalva de que em produção usaria secret manager) no README.

## 2. Ajustar o CI existente — `.github/workflows/ci.yml`

O `ci.yml` atual roda em `push` e `pull_request` na `main`. Para não duplicar build com a nova
pipeline, deixe o `ci.yml` **apenas para pull requests** (validação de PR). Altere o bloco `on:`:

```yaml
on:
  pull_request:
    branches: [ main ]
```

(Remova o gatilho de `push`.) O restante do `ci.yml` permanece igual (build + test).

## 3. Nova pipeline — `.github/workflows/ci-cd.yml`

Roda em `push` na `main` (e permite execução manual pelo botão, útil para gravar o vídeo).

```yaml
name: CI/CD

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  # 1) Build + testes automatizados
  build-test:
    name: Build & Test
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore OficinaMecanica.slnx

      - name: Build
        run: dotnet build OficinaMecanica.slnx --no-restore --configuration Release

      - name: Test
        run: dotnet test OficinaMecanica.slnx --no-build --configuration Release --verbosity normal

  # 2) Build + push da imagem Docker (Docker Hub)
  docker-image:
    name: Build & Push image
    needs: build-test
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Docker login
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}

      - name: Build and push
        uses: docker/build-push-action@v6
        with:
          context: .
          file: src/Bootstrap/Api/Dockerfile
          push: true
          tags: |
            docker.io/${{ secrets.DOCKERHUB_USERNAME }}/oficina-mecanica-api:latest
            docker.io/${{ secrets.DOCKERHUB_USERNAME }}/oficina-mecanica-api:${{ github.sha }}

  # 3) Deploy: cria cluster kind efêmero + aplica tudo via Terraform + smoke test
  deploy:
    name: Deploy to Kubernetes (kind)
    needs: docker-image
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Terraform
        uses: hashicorp/setup-terraform@v3
        with:
          terraform_version: '1.9.5'

      - name: Terraform Init
        working-directory: infra
        run: terraform init -input=false

      - name: Terraform Apply (cria kind + deploy do banco e da app)
        working-directory: infra
        run: |
          terraform apply -auto-approve -input=false \
            -var="api_image=docker.io/${{ secrets.DOCKERHUB_USERNAME }}/oficina-mecanica-api:${{ github.sha }}"

      - name: Smoke test (/healthz via NodePort)
        run: |
          echo "aguardando a API responder em localhost:30080..."
          for i in $(seq 1 30); do
            if curl -fsS http://localhost:30080/healthz; then
              echo "OK: API respondeu 200"; exit 0
            fi
            sleep 10
          done
          echo "FALHA: API nao respondeu a tempo"; exit 1

      - name: Estado do cluster (evidência nos logs)
        if: always()
        env:
          KUBECONFIG: ${{ github.workspace }}/infra/kubeconfig.yaml
        run: |
          kubectl get pods,svc,hpa -n oficina-mecanica || true
          kubectl top pods -n oficina-mecanica || true

      - name: Terraform Destroy (limpa o cluster efêmero)
        if: always()
        working-directory: infra
        run: terraform destroy -auto-approve -input=false || true
```

### Notas de correção (evitam furos)

- **`context: .` + `file: src/Bootstrap/Api/Dockerfile`** — o build precisa do contexto na raiz
  (o Dockerfile faz `COPY src/ ...`). Não mude para `context: src/...`.
- **Duas tags** (`latest` e o `github.sha`): o deploy usa a tag imutável `:${{ github.sha }}`,
  garantindo que o cluster puxe exatamente a imagem recém-publicada (evita cache de `latest`).
- **NodePort 30080** funciona no runner porque o `kind_config` mapeia `host_port = 30080`
  (ver `03`); por isso o smoke test chama `localhost:30080` direto.
- **`terraform destroy` no fim** é opcional (o runner morre de qualquer forma), mas deixa o job
  limpo e comprova que o ciclo completo funciona. `if: always()` garante que rode mesmo se o
  smoke test falhar.
- **Estado do Terraform no CI é efêmero** (arquivo local no runner, descartado depois) — isso é
  aceitável para o Tech Challenge, pois o cluster também é efêmero. Não configure backend remoto.
- Se o `terraform apply` falhar por timeout de pods, aumente o `wait`/`failureThreshold` das
  probes (ver `02`) ou rode o `kubectl get pods` do passo de evidência para diagnosticar.

## 4. Como validar

1. Crie os secrets (`DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN`) e o repositório público no Docker Hub.
2. Faça commit dos manifestos (`/k8s`), do `/infra` e dos workflows, e `push` na `main`.
3. Abra a aba **Actions** e acompanhe a execução do workflow **CI/CD**. Todos os jobs
   (`build-test` → `docker-image` → `deploy`) devem ficar **verdes**.
4. O log do job `deploy` deve mostrar o smoke test respondendo 200 e o `kubectl get pods,hpa`
   com os recursos criados.

Esta etapa está pronta quando o workflow `CI/CD` conclui verde de ponta a ponta no GitHub Actions.

## 5. Validar a escalabilidade automática (HPA)

A escala do HPA é melhor validada **localmente** (o cluster do CI é efêmero e some ao fim do
job). Com o sistema no ar via Terraform local (ver `03`):

```bash
kubectl get hpa -n oficina-mecanica -w        # janela 1: observa TARGETS e réplicas

# janela 2: gera carga contra a API
for i in $(seq 1 5000); do curl -s http://localhost:30080/healthz > /dev/null; done
```

Ao subir o uso de CPU acima de 50%, o HPA aumenta as réplicas de `oficina-api` (até 5); ao
cessar a carga, volta a 1 após o período de estabilização. Isso comprova que o requisito de
autoscaling por CPU está funcional.
