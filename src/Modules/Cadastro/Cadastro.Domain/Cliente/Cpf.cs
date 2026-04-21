using SharedKernel.Domain;
using DocumentValidator;

namespace Cadastro.Domain.Cliente;
public sealed class Cpf : Documento
{

    private Cpf(string numero) : base(numero) { }

    public static Result<Cpf> Criar(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return Error.Validation("CPF.Invalido", "CPF é obrigatório.");

        var cpf = new Cpf(numero);

        if (!cpf.ValidarDocumento(numero))
            return Error.Validation("CPF.Invalido", "CPF inválido.");

        return cpf;
    }

    protected override bool ValidarDocumento(string numero)
    {
        return CpfValidation.Validate(numero);
    }
}
