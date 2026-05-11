using OrdemServico.Domain.OrdemServico.Events;
using SharedKernel.Domain;

namespace OrdemServico.Domain.OrdemServico;

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
            return Error.Validation("OrdemServico.ClienteIdVazio", "ClienteId é obrigatório.");
        if (veiculoId == Guid.Empty)
            return Error.Validation("OrdemServico.VeiculoIdVazio", "VeiculoId é obrigatório.");

        var ordemServico = new OrdemServico(OrdemServicoId.Novo(), clienteId, veiculoId);
        ordemServico.AddDomainEvent(new OrdemServicoGerada(ordemServico.Id, clienteId, veiculoId, DateTime.UtcNow));
        return ordemServico;
    }

    public Result<OrdemServico> IniciarDiagnostico()
    {
        if (Status != StatusOrdemServico.Recebida)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode entrar em diagnóstico quando está Recebida.");

        Status = StatusOrdemServico.EmDiagnostico;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new DiagnosticoIniciado(Id, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> RegistrarDiagnostico(string descricaoDiagnostico)
    {
        if (Status != StatusOrdemServico.EmDiagnostico)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode registrar diagnóstico quando está Em Diagnóstico.");

        DescricaoDiagnostico = descricaoDiagnostico;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new DiagnosticoRegistrado(Id, DescricaoDiagnostico, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> AdicionarPecaInsumo(Guid pecaInsumoId, int quantidade, decimal precoUnitario)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Peças e insumos só podem ser adicionados quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemPecaResult = ItemPeca.Criar(pecaInsumoId, quantidade, precoUnitario);
        if (itemPecaResult.IsFailure)
            return itemPecaResult.Error;

        _itensPeca.Add(itemPecaResult.Value);
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> AtualizarQuantidadePecaInsumo(Guid itemPecaId, int novaQuantidade)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Peças e insumos só podem ser atualizadas quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemPeca = _itensPeca.FirstOrDefault(x => x.Id.Value == itemPecaId);
        if (itemPeca == null)
            return Error.Validation("OrdemServico.ItemPecaNaoEncontrado", "Item de peça não encontrado.");

        var resultado = itemPeca.AtualizarQuantidade(novaQuantidade);
        if (resultado.IsFailure) return resultado.Error;

        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> AtualizarPrecoUnitarioPecaInsumo(Guid itemPecaId, decimal novoPrecoUnitario)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Peças e insumos só podem ser atualizadas quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemPeca = _itensPeca.FirstOrDefault(x => x.Id.Value == itemPecaId);
        if (itemPeca == null)
            return Error.Validation("OrdemServico.ItemPecaNaoEncontrado", "Item de peça não encontrado.");

        var resultado = itemPeca.AtualizarPrecoUnitarioSnapshot(novoPrecoUnitario);
        if (resultado.IsFailure) return resultado.Error;

        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> RemoverPecaInsumo(Guid itemPecaId)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Peças e insumos só podem ser removidas quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemPeca = _itensPeca.FirstOrDefault(x => x.Id.Value == itemPecaId);
        if (itemPeca == null)
            return Error.Validation("OrdemServico.ItemPecaNaoEncontrado", "Item de peça não encontrado.");

        _itensPeca.Remove(itemPeca);
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> AdicionarServico(Guid servicoId, int quantidade, decimal precoUnitario)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Serviços só podem ser adicionados quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemServicoResult = ItemServico.Criar(servicoId, quantidade, precoUnitario);
        if (itemServicoResult.IsFailure)
            return itemServicoResult.Error;

        _itensServico.Add(itemServicoResult.Value);
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> AtualizarQuantidadeServico(Guid itemServicoId, int novaQuantidade)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Serviços só podem ser atualizados quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemServico = _itensServico.FirstOrDefault(x => x.Id.Value == itemServicoId);
        if (itemServico == null)
            return Error.Validation("OrdemServico.ItemServicoNaoEncontrado", "Item de serviço não encontrado.");

        var resultado = itemServico.AtualizarQuantidade(novaQuantidade);
        if (resultado.IsFailure) return resultado.Error;

        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> AtualizarPrecoUnitarioServico(Guid itemServicoId, decimal novoPrecoUnitario)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Serviços só podem ser atualizados quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemServico = _itensServico.FirstOrDefault(x => x.Id.Value == itemServicoId);
        if (itemServico == null)
            return Error.Validation("OrdemServico.ItemServicoNaoEncontrado", "Item de serviço não encontrado.");

        var resultado = itemServico.AtualizarPrecoUnitarioSnapshot(novoPrecoUnitario);
        if (resultado.IsFailure) return resultado.Error;

        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> RemoverServico(Guid itemServicoId)
    {
        if (Status != StatusOrdemServico.EmDiagnostico || DescricaoDiagnostico == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Serviços só podem ser removidos quando a ordem de serviço está Em Diagnóstico com diagnóstico registrado.");

        var itemServico = _itensServico.FirstOrDefault(x => x.Id.Value == itemServicoId);
        if (itemServico == null)
            return Error.Validation("OrdemServico.ItemServicoNaoEncontrado", "Item de serviço não encontrado.");

        _itensServico.Remove(itemServico);
        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> GerarOrcamento()
    {
        if (Status != StatusOrdemServico.EmDiagnostico && Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode gerar orçamento quando está Em Diagnóstico ou Aguardando Aprovação.");
        if (_orcamentos.Any(x => x.Status != StatusOrcamento.Rejeitado))
            return Error.Validation("OrdemServico.OrcamentoExistente", "Já existe um orçamento pendente ou aprovado para esta ordem de serviço.");
        if (!_itensServico.Any())
            return Error.Validation("OrdemServico.OrcamentoSemServicos", "Não é possível gerar orçamento sem itens de serviços.");
        if (!_itensPeca.Any())
            return Error.Validation("OrdemServico.OrcamentoSemPecas", "Não é possível gerar orçamento sem itens de peças.");

        var precoTotalPecas = _itensPeca.Sum(x => x.Quantidade * x.PrecoUnitarioSnapshot);
        var precoTotalServicos = _itensServico.Sum(x => x.Quantidade * x.PrecoUnitarioSnapshot);
        var precoTotal = precoTotalPecas + precoTotalServicos;

        var orcamento = Orcamento.Criar(precoTotal);
        _orcamentos.Add(orcamento);
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrcamentoGerado(Id, orcamento.Id, orcamento.ValorTotal, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> EnviarOrcamento(DateTime dataEnvio)
    {
        if (Status != StatusOrdemServico.EmDiagnostico && Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode enviar orçamento quando está Em Diagnóstico ou Aguardando Aprovação.");

        var orcamento = _orcamentos.FirstOrDefault(x => x.Status == StatusOrcamento.Pendente);
        if (orcamento == null)
            return Error.Validation("OrdemServico.OrcamentoNaoEncontrado", "Nenhum orçamento pendente encontrado para esta ordem de serviço.");

        var resultadoEnvio = orcamento.Enviar(dataEnvio);
        if (resultadoEnvio.IsFailure)
            return resultadoEnvio.Error;

        Status = StatusOrdemServico.AguardandoAprovacao;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrcamentoEnviado(Id, orcamento.Id, dataEnvio, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> AprovarOrcamento()
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode aprovar orçamento quando está Aguardando Aprovação.");

        var orcamento = _orcamentos.FirstOrDefault(x => x.Status == StatusOrcamento.Enviado);
        if (orcamento == null)
            return Error.Validation("OrdemServico.OrcamentoNaoEncontrado", "Nenhum orçamento enviado encontrado para esta ordem de serviço.");

        var resultado = orcamento.Aprovar();
        if (resultado.IsFailure)
            return resultado.Error;

        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrcamentoAprovado(Id, orcamento.Id, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> RejeitarOrcamento()
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode rejeitar orçamento quando está Aguardando Aprovação.");

        var orcamento = _orcamentos.FirstOrDefault(x => x.Status == StatusOrcamento.Enviado);
        if (orcamento == null)
            return Error.Validation("OrdemServico.OrcamentoNaoEncontrado", "Nenhum orçamento enviado encontrado para esta ordem de serviço.");

        var resultado = orcamento.Rejeitar();
        if (resultado.IsFailure)
            return resultado.Error;

        AtualizadoEm = DateTime.UtcNow;
        return this;
    }

    public Result<OrdemServico> Executar()
    {
        if (Status != StatusOrdemServico.AguardandoAprovacao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode iniciar execução quando está Aguardando Aprovação.");
        if (!_orcamentos.Any(x => x.Status == StatusOrcamento.Aprovado))
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode iniciar execução quando o orçamento está Aprovado.");

        Status = StatusOrdemServico.EmExecucao;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrdemServicoEmExecucao(Id, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> Finalizar()
    {
        if (Status != StatusOrdemServico.EmExecucao)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode finalizar quando está Em Execução.");

        Status = StatusOrdemServico.Finalizada;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrdemServicoFinalizada(Id, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> NotificarCliente(DateTime dataNotificacao)
    {
        if (Status != StatusOrdemServico.Finalizada)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode notificar cliente quando está Finalizada.");

        NotificadoEm = dataNotificacao;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new ClienteNotificado(Id, dataNotificacao, DateTime.UtcNow));
        return this;
    }

    public Result<OrdemServico> Concluir(DateTime dataEntrega)
    {
        if (Status != StatusOrdemServico.Finalizada || NotificadoEm == null)
            return Error.Validation("OrdemServico.TransicaoInvalida", "Ordem de Serviço só pode ser concluída quando está Finalizada e o cliente foi notificado.");

        EntregueEm = dataEntrega;
        Status = StatusOrdemServico.Entregue;
        AtualizadoEm = DateTime.UtcNow;
        AddDomainEvent(new OrdemServicoConcluida(Id, dataEntrega, DateTime.UtcNow));
        return this;
    }
}
