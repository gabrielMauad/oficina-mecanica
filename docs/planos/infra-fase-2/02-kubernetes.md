# 02 — Kubernetes (manifestos em `/k8s`)

> Pré-requisito de leitura: `00-visao-geral.md`.

Objetivo: criar todos os manifestos que compõem o sistema no cluster: namespace, ConfigMap,
Secret, banco PostgreSQL (com persistência), aplicação (Deployment + Service) e HPA.

Crie os arquivos exatamente nos caminhos indicados (ver árvore na seção 5 do `00`). Os
manifestos são aplicáveis tanto via `kubectl apply -f k8s/ -R` (para testar rápido) quanto via
Terraform (ver `03`, que é o caminho oficial do deploy). **A ordem importa** — o `03` cuida da
ordenação; para aplicar manual, siga a numeração dos prefixos (00 → 22).

> **Sobre a imagem da API:** os manifestos referenciam `IMAGE_PLACEHOLDER`. No fluxo Terraform
> (`03`) o deployment da API é renderizado via `templatefile` e o placeholder é substituído pela
> variável `var.api_image`. Se for aplicar via `kubectl` puro para teste local, troque
> manualmente `IMAGE_PLACEHOLDER` por `oficina-mecanica-api:local` (após `kind load`, ver `03`).

---

## `k8s/base/00-namespace.yaml`

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: oficina-mecanica
  labels:
    app.kubernetes.io/part-of: oficina-mecanica
```

## `k8s/base/01-configmap.yaml`

Variáveis **não sensíveis** (ver mapa na seção 2.1 do `00`):

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: oficina-config
  namespace: oficina-mecanica
  labels:
    app.kubernetes.io/part-of: oficina-mecanica
data:
  ASPNETCORE_ENVIRONMENT: "Development"
  ASPNETCORE_URLS: "http://+:8080"
  Auth__AdminEmail: "admin@oficina.com"
```

## `k8s/base/02-secret.yaml`

Variáveis **sensíveis**. Usa `stringData` (texto plano; o Kubernetes codifica em base64 ao
salvar). A senha do Postgres aparece em dois lugares e **tem de ser idêntica**: em
`POSTGRES_PASSWORD` (consumida pelo container do banco) e embutida em `ConnectionStrings__Default`
(consumida pela API).

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: oficina-secrets
  namespace: oficina-mecanica
  labels:
    app.kubernetes.io/part-of: oficina-mecanica
type: Opaque
stringData:
  # credenciais do banco (consumidas pelo container do Postgres)
  POSTGRES_USER: "oficina"
  POSTGRES_PASSWORD: "oficina-cluster-pass-000"
  POSTGRES_DB: "oficina_mecanica"
  # connection string da API (embute a MESMA senha acima)
  ConnectionStrings__Default: "Host=oficina-postgres;Port=5432;Username=oficina;Password=oficina-cluster-pass-000;Database=oficina_mecanica;SSL Mode=Disable"
  # segredos da aplicação
  Jwt__Secret: "cluster-jwt-secret-key-min-32-chars-000"
  Auth__AdminSenha: "admin123"
```

> Em produção real isto iria para um secret manager (Azure Key Vault, AWS Secrets Manager) e
> nunca versionado. Para o Tech Challenge, `stringData` versionado é aceitável — documente essa
> ressalva no README (ver `05`).

---

## Banco de dados — PostgreSQL

A persistência usa **PVC com provisionamento dinâmico**. O kind já traz uma StorageClass padrão
(`standard`, do local-path-provisioner), então **basta um PVC** — o PersistentVolume é criado
automaticamente (não é preciso declarar um PV manual).

### `k8s/database/10-postgres-pvc.yaml`

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: oficina-postgres-pvc
  namespace: oficina-mecanica
  labels:
    app.kubernetes.io/part-of: oficina-mecanica
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 1Gi
```

### `k8s/database/11-postgres-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: oficina-postgres
  namespace: oficina-mecanica
  labels:
    app: oficina-postgres
    app.kubernetes.io/part-of: oficina-mecanica
spec:
  replicas: 1
  strategy:
    type: Recreate            # banco com volume RWO: não pode haver 2 pods montando o mesmo PVC
  selector:
    matchLabels:
      app: oficina-postgres
  template:
    metadata:
      labels:
        app: oficina-postgres
    spec:
      containers:
        - name: postgres
          image: postgres:16
          ports:
            - containerPort: 5432
          env:
            - name: POSTGRES_USER
              valueFrom:
                secretKeyRef:
                  name: oficina-secrets
                  key: POSTGRES_USER
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: oficina-secrets
                  key: POSTGRES_PASSWORD
            - name: POSTGRES_DB
              valueFrom:
                secretKeyRef:
                  name: oficina-secrets
                  key: POSTGRES_DB
            # o Postgres exige subdiretório próprio quando o mount é a raiz do volume
            - name: PGDATA
              value: /var/lib/postgresql/data/pgdata
          resources:
            requests:
              cpu: "100m"
              memory: "256Mi"
            limits:
              cpu: "500m"
              memory: "512Mi"
          readinessProbe:
            exec:
              command: ["sh", "-c", "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB"]
            initialDelaySeconds: 10
            periodSeconds: 10
          livenessProbe:
            exec:
              command: ["sh", "-c", "pg_isready -U $POSTGRES_USER -d $POSTGRES_DB"]
            initialDelaySeconds: 30
            periodSeconds: 20
          volumeMounts:
            - name: postgres-data
              mountPath: /var/lib/postgresql/data
      volumes:
        - name: postgres-data
          persistentVolumeClaim:
            claimName: oficina-postgres-pvc
```

### `k8s/database/12-postgres-service.yaml`

Service `ClusterIP` (banco só é acessado de dentro do cluster, pela API):

```yaml
apiVersion: v1
kind: Service
metadata:
  name: oficina-postgres
  namespace: oficina-mecanica
  labels:
    app.kubernetes.io/part-of: oficina-mecanica
spec:
  type: ClusterIP
  selector:
    app: oficina-postgres
  ports:
    - port: 5432
      targetPort: 5432
```

> O nome do Service (`oficina-postgres`) é o DNS interno usado no `ConnectionStrings__Default`
> (Host=oficina-postgres). Se renomear um, renomeie o outro.

---

## Aplicação — API .NET

### `k8s/app/20-api-deployment.yaml`

Pontos críticos: **initContainer** esperando o Postgres (a API roda migrations no boot e
quebra se o banco não estiver pronto); env vinda de ConfigMap + Secret via `envFrom`; probes no
`/healthz`; e **`resources.requests.cpu` definido** (obrigatório para o HPA calcular).

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: oficina-api
  namespace: oficina-mecanica
  labels:
    app: oficina-api
    app.kubernetes.io/part-of: oficina-mecanica
spec:
  replicas: 1                 # o HPA assume o controle do número de réplicas depois
  selector:
    matchLabels:
      app: oficina-api
  template:
    metadata:
      labels:
        app: oficina-api
    spec:
      initContainers:
        - name: wait-for-postgres
          image: busybox:1.36
          command:
            - sh
            - -c
            - |
              echo "aguardando o postgres em oficina-postgres:5432..."
              until nc -z oficina-postgres 5432; do sleep 2; done
              echo "postgres disponivel."
      containers:
        - name: api
          image: IMAGE_PLACEHOLDER        # ver nota sobre a imagem no topo deste doc
          imagePullPolicy: IfNotPresent
          ports:
            - containerPort: 8080
          envFrom:
            - configMapRef:
                name: oficina-config
            - secretRef:
                name: oficina-secrets
          resources:
            requests:
              cpu: "100m"                 # base para o cálculo do HPA
              memory: "256Mi"
            limits:
              cpu: "500m"
              memory: "512Mi"
          startupProbe:                   # dá tempo das migrations rodarem no boot
            httpGet:
              path: /healthz
              port: 8080
            failureThreshold: 30
            periodSeconds: 5
          readinessProbe:
            httpGet:
              path: /healthz
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /healthz
              port: 8080
            initialDelaySeconds: 15
            periodSeconds: 20
```

> A Secret `oficina-secrets` contém também `POSTGRES_*` (usadas pelo banco). Injetá-las na API
> via `envFrom` é inofensivo (a API ignora env vars que não conhece). Se preferir higiene
> estrita, separe em duas Secrets (uma do banco, uma da app) — opcional.

### `k8s/app/21-api-service.yaml`

Service `NodePort` para acesso externo (host → cluster). A faixa de NodePort é 30000–32767;
usamos `30080` (casado com o `extra_port_mappings` do kind em `03`).

```yaml
apiVersion: v1
kind: Service
metadata:
  name: oficina-api
  namespace: oficina-mecanica
  labels:
    app.kubernetes.io/part-of: oficina-mecanica
spec:
  type: NodePort
  selector:
    app: oficina-api
  ports:
    - port: 80
      targetPort: 8080
      nodePort: 30080
```

### `k8s/app/22-api-hpa.yaml`

HPA por CPU. Escala de 1 a 5 réplicas com alvo de 50% de utilização média de CPU sobre o `requests.cpu` (100m) definido no Deployment.

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: oficina-api-hpa
  namespace: oficina-mecanica
  labels:
    app.kubernetes.io/part-of: oficina-mecanica
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: oficina-api
  minReplicas: 1
  maxReplicas: 5
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 50
```

> O HPA só funciona com o **metrics-server** instalado no cluster e saudável. No kind isso exige
> a flag `--kubelet-insecure-tls` — instalado via Terraform em `03`. Sem ele, o `TARGETS` fica
> `<unknown>` e o HPA não escala.

---

## Como validar (aplicando manual, sem Terraform)

> Este bloco é só para teste rápido dos manifestos. O deploy oficial é via Terraform (`03`), que
> também instala o metrics-server. Para validar o HPA de fato, use o fluxo do `03`.

```bash
# pré-requisito: um cluster kind existente e a imagem carregada (ver 03 para criar/carregar)
# troque IMAGE_PLACEHOLDER por oficina-mecanica-api:local nos manifestos antes de aplicar

kubectl apply -f k8s/base/
kubectl apply -f k8s/database/
kubectl apply -f k8s/app/

kubectl get pods -n oficina-mecanica -w      # esperar tudo Running
kubectl port-forward -n oficina-mecanica svc/oficina-api 8080:80 &
curl -i http://localhost:8080/healthz        # 200
```

Esta etapa está pronta quando todos os pods do namespace ficam `Running` e `/healthz` responde
200. A validação do HPA (metrics + escala) acontece no `03`.
