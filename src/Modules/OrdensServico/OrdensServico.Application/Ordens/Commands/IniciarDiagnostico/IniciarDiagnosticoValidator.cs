using FluentValidation;

namespace OrdensServico.Application.Ordens.Commands.IniciarDiagnostico;

public sealed class IniciarDiagnosticoValidator : AbstractValidator<IniciarDiagnosticoCommand>
{
    public IniciarDiagnosticoValidator()
    {
        RuleFor(x => x.OrdemServicoId)
            .NotEmpty()
            .WithMessage("OrdemServicoId é obrigatório.");
    }
}
