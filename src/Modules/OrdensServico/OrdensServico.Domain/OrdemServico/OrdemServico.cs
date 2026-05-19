using OrdensServico.Domain.OrdemServico.Events;
using SharedKernel.Domain;

namespace OrdensServico.Domain.OrdemServico;

public sealed class OrdemServico : AggregateRoot<OrdemServicoId>
{
    private readonly List<ItemPeca> _itensPeca = [];
    private readonly List<ItemServico> _itensServico = [];
    private readonly List<Orcamento> _orcamentos = [];

    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public StatusOrdemServico Status { get; private set; }
    public string? DescricaoDiagnostico { get; private set; }
    public DateTime? NotificadoEm { get; private set; }
    public DateTime? EntregueEm { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    public IReadOnlyList<ItemPeca> ItensPeca => _itensPeca.AsReadOnly();
    public IReadOnlyList<ItemServico> ItensServico => _itensServico.AsReadOnly();
    public IReadOnlyList<Orcamento> Orcamentos => _orcamentos.AsReadOnly();

    private OrdemServico(
        OrdemServicoId id,
        Guid clienteId,
        Guid veiculoId
    ) : base(id)
    {
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOrdemServico.Recebida;
        CriadoEm = DateTime.UtcNow;
        AtualizadoEm = DateTime.UtcNow;
    }

    public static Result<OrdemServico> Criar(Guid clienteId, Guid veiculoId)
    {
        if (clienteId == Guid.Empty)
            return Error.Validation("OrdemServico.ClienteIdVazio", "ClienteId Ã© obrigatÃ³rio.");
        if (veiculoId == Guid.Empty)
            return Error.Validation("OrdemServico.VeiculoIdVazio", "VeiculoId Ã© obrigatÃ³rio.");

        var ordemServico = new OrdemServico(OrdemServicoId.Novo(), clienteId, veiculoId);
        return ordemServico;
    }

    public Result<OrdemServico> IniciarDiagnostico()
    {
        if (Status != StatusOrdemServico.Recebida)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode entrar em diagnÃ³stico quando estÃ¡ Recebida.");

        Status = StatusOrdemServico.EmDiagnostico;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> RegistrarDiagnostico(
        string descricaoDiagnostico,
        IEnumerable<ItemServicoInput> servicos,
        IEnumerable<ItemPecaInput> pecas)
    {
        if (Status != StatusOrdemServico.EmDiagnostico && Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode registrar diagnÃ³stico quando estÃ¡ Em DiagnÃ³stico ou Aguardando AprovaÃ§Ã£o.");
        if (_orcamentos.Any(x => x.Status != StatusOrcamento.Rejeitado))
            return Error.Validation("OrdemServico.OrcamentoExistente", "JÃ¡ existe um orÃ§amento ativo para esta ordem de serviÃ§o.");
        if (string.IsNullOrWhiteSpace(descricaoDiagnostico))
            return Error.Validation("OrdemServico.DiagnosticoVazio", "DescriÃ§Ã£o do diagnÃ³stico Ã© obrigatÃ³ria.");

        var servicosList = servicos.ToList();
        var pecasList = pecas.ToList();

        if (!servicosList.Any())
            return Error.Validation("OrdemServico.OrcamentoSemServicos", "NÃ£o Ã© possÃ­vel gerar orÃ§amento sem itens de serviÃ§os.");
        if (!pecasList.Any())
            return Error.Validation("OrdemServico.OrcamentoSemPecas", "NÃ£o Ã© possÃ­vel gerar orÃ§amento sem itens de peÃ§as.");

        _itensServico.Clear();
        _itensPeca.Clear();

        foreach (var input in servicosList)
        {
            var itemResult = ItemServico.Criar(input.ServicoId, input.Quantidade, input.PrecoUnitario);
            if (itemResult.IsFailure) return itemResult.Error;
            _itensServico.Add(itemResult.Value);
        }

        foreach (var input in pecasList)
        {
            var itemResult = ItemPeca.Criar(input.PecaInsumoId, input.Quantidade, input.PrecoUnitario);
            if (itemResult.IsFailure) return itemResult.Error;
            _itensPeca.Add(itemResult.Value);
        }

        var valorTotal = _itensPeca.Sum(x => x.Quantidade * x.PrecoUnitarioSnapshot)
                       + _itensServico.Sum(x => x.Quantidade * x.PrecoUnitarioSnapshot);

        var orcamento = Orcamento.Criar(valorTotal);
        _orcamentos.Add(orcamento);

        DescricaoDiagnostico = descricaoDiagnostico;
        AtualizadoEm = DateTime.UtcNow;

        var servicosSnapshot = _itensServico
            .Select(x => new ItemServicoSnapshot(x.ServicoId, x.Quantidade, x.PrecoUnitarioSnapshot))
            .ToList();
        var pecasSnapshot = _itensPeca
            .Select(x => new ItemPecaSnapshot(x.PecaInsumoId, x.Quantidade, x.PrecoUnitarioSnapshot))
            .ToList();

        AddDomainEvent(new DiagnosticoConcluido(Id, orcamento.Id, descricaoDiagnostico, servicosSnapshot, pecasSnapshot, valorTotal, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> EnviarOrcamento(DateTime dataEnvio)
    {
        if (Status != StatusOrdemServico.EmDiagnostico && Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode enviar orÃ§amento quando estÃ¡ Em DiagnÃ³stico ou Aguardando AprovaÃ§Ã£o.");

        var orcamento = _orcamentos.FirstOrDefault(x => x.Status == StatusOrcamento.Pendente);
        if (orcamento == null)
            return Error.Validation("OrdemServico.OrcamentoNaoEncontrado", "Nenhum orÃ§amento pendente encontrado para esta ordem de serviÃ§o.");

        var resultado = orcamento.Enviar(dataEnvio);
        if (resultado.IsFailure) return resultado.Error;

        Status = StatusOrdemServico.AguardandoAprovacao;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> AprovarOrcamento()
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode aprovar orÃ§amento quando estÃ¡ Aguardando AprovaÃ§Ã£o.");

        var orcamento = _orcamentos.FirstOrDefault(x => x.Status == StatusOrcamento.Enviado);
        if (orcamento == null)
            return Error.Validation("OrdemServico.OrcamentoNaoEncontrado", "Nenhum orÃ§amento enviado encontrado para esta ordem de serviÃ§o.");

        var resultado = orcamento.Aprovar();
        if (resultado.IsFailure)
            return resultado.Error;

        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> RejeitarOrcamento()
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode rejeitar orÃ§amento quando estÃ¡ Aguardando AprovaÃ§Ã£o.");

        var orcamento = _orcamentos.FirstOrDefault(x => x.Status == StatusOrcamento.Enviado);
        if (orcamento == null)
            return Error.Validation("OrdemServico.OrcamentoNaoEncontrado", "Nenhum orÃ§amento enviado encontrado para esta ordem de serviÃ§o.");

        var resultado = orcamento.Rejeitar();
        if (resultado.IsFailure)
            return resultado.Error;

        var pecasSnapshot = _itensPeca
            .Select(x => new ItemPecaSnapshot(x.PecaInsumoId, x.Quantidade, x.PrecoUnitarioSnapshot))
            .ToList();

        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrcamentoRejeitado(Id, orcamento.Id, pecasSnapshot, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> Executar()
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode iniciar execuÃ§Ã£o quando estÃ¡ Aguardando AprovaÃ§Ã£o.");
        if (!_orcamentos.Any(x => x.Status == StatusOrcamento.Aprovado))
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode iniciar execuÃ§Ã£o quando o orÃ§amento estÃ¡ Aprovado.");

        Status = StatusOrdemServico.EmExecucao;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> Finalizar()
    {
        if (Status != StatusOrdemServico.EmExecucao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode finalizar quando estÃ¡ Em ExecuÃ§Ã£o.");

        Status = StatusOrdemServico.Finalizada;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrdemServicoFinalizada(Id, ClienteId, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> NotificarCliente(DateTime dataNotificacao)
    {
        if (Status != StatusOrdemServico.Finalizada)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode notificar cliente quando estÃ¡ Finalizada.");

        NotificadoEm = dataNotificacao;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> Concluir(DateTime dataEntrega)
    {
        if (Status != StatusOrdemServico.Finalizada || NotificadoEm == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de ServiÃ§o sÃ³ pode ser concluÃ­da quando estÃ¡ Finalizada e o cliente foi notificado.");

        EntregueEm = dataEntrega;
        Status = StatusOrdemServico.Entregue;
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }
}
