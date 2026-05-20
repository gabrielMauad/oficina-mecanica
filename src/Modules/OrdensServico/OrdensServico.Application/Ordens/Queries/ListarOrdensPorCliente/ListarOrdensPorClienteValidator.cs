using FluentValidation;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

public sealed class ListarOrdensPorClienteValidator : AbstractValidator<ListarOrdensPorClienteQuery>
{
    public ListarOrdensPorClienteValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty()
            .WithMessage("O ID do cliente não pode ser vazio.");
    }
}
