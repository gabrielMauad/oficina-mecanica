using FluentValidation;

namespace OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;

public sealed class ObterOrdemServicoPorIdValidator : AbstractValidator<ObterOrdemServicoPorIdQuery>
{
    public ObterOrdemServicoPorIdValidator()
    {
        RuleFor(x => x.OrdemServicoId)
            .NotEmpty()
            .WithMessage("O ID da ordem de serviço não pode ser vazio.");
    }
}
