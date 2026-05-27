using FluentValidation;

namespace OrdensServico.Application.Ordens.Commands.FinalizarOrdemServico;

public sealed class FinalizarOrdemServicoValidator : AbstractValidator<FinalizarOrdemServicoCommand>
{
    public FinalizarOrdemServicoValidator()
    {
        RuleFor(x => x.OrdemServicoId)
            .NotEmpty()
            .WithMessage("O ID da ordem de serviço não pode ser vazio.");
    }
}

