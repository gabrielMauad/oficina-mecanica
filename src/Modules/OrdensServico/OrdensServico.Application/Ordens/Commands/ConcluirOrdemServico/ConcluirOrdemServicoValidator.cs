using FluentValidation;

namespace OrdensServico.Application.Ordens.Commands.ConcluirOrdemServico;

public sealed class ConcluirOrdemServicoValidator : AbstractValidator<ConcluirOrdemServicoCommand>
{
    public ConcluirOrdemServicoValidator()
    {
        RuleFor(x => x.OrdemServicoId)
            .NotEmpty()
            .WithMessage("O ID da ordem de serviço não pode ser vazio.");
    }
}

