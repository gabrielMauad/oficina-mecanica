using FluentValidation;

namespace OrdensServico.Application.Ordens.Commands.ExecutarOrdemServico;

public sealed class ExecutarOrdemServicoValidator : AbstractValidator<ExecutarOrdemServicoCommand>
{
    public ExecutarOrdemServicoValidator()
    {
        RuleFor(x => x.OrdemServicoId)
            .NotEmpty()
            .WithMessage("O ID da ordem de serviço não pode ser vazio.");
    }
}

