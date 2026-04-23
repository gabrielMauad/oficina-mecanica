using FluentValidation;

namespace Cadastro.Application.Clientes.Commands.CadastrarCliente;

public sealed class CadastrarClienteValidator : AbstractValidator<CadastrarClienteCommand>
{
    public CadastrarClienteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^[\p{L}\s]+$")
            .WithMessage("Nome inválido. Não são permitidos números ou caracteres especiais.");

        RuleFor(x => x.Documento)
            .NotEmpty()
            .Matches(@"^[\d.\-/]+$")
            .WithMessage("Documento inválido.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
            .WithMessage("Email inválido.");

        RuleFor(x => x.Telefone)
            .NotEmpty()
            .Matches(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$")
            .WithMessage("Telefone inválido. Formatos aceitos: (11) 91234-5678 ou (11) 1234-5678.");
    }
}
