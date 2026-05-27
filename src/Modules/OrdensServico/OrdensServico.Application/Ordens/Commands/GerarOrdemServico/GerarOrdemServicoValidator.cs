using FluentValidation;

namespace OrdensServico.Application.Ordens.Commands.GerarOrdemServico;

public sealed class GerarOrdemServicoValidator : AbstractValidator<GerarOrdemServicoCommand>
{
    public GerarOrdemServicoValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty()
            .WithMessage("ClienteId é obrigatório.");
        RuleFor(x => x.VeiculoId)
            .NotEmpty()
            .WithMessage("VeiculoId é obrigatório.");
    }
}
