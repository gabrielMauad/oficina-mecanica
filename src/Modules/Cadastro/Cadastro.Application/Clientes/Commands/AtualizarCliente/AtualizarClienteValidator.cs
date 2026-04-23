using FluentValidation;

namespace Cadastro.Application.Clientes.Commands.AtualizarCliente;

public sealed class AtualizarClienteValidator : AbstractValidator<AtualizarClienteCommand>
{
    public AtualizarClienteValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Nome)
            .MaximumLength(200)
            .Matches(@"^[\p{L}\s]+$")
            .WithMessage("Nome inválido. Não são permitidos números ou caracteres especiais.")
            .When(x => x.Nome is not null);

        RuleFor(x => x.Telefone)
            .Matches(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$")
            .WithMessage("Telefone inválido. Formatos aceitos: (11) 91234-5678 ou (11) 1234-5678.")
            .When(x => x.Telefone is not null);
    }
}

