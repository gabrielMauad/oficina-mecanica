using FluentValidation;

namespace OrdensServico.Application.Ordens.Commands.RejeitarOrcamento;

public sealed class RejeitarOrcamentoValidator : AbstractValidator<RejeitarOrcamentoCommand>
{
    public RejeitarOrcamentoValidator()
    {
        RuleFor(x => x.OrdemServicoId)
            .NotEmpty()
            .WithMessage("O ID da ordem de serviço não pode estar vazio.");
    }
}
