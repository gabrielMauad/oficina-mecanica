using DocumentValidator;
using SharedKernel.Domain;

namespace Cadastro.Domain.Cliente;
public sealed class Cnpj : Documento
{
    private Cnpj(string numero) : base(numero) { }

    public static Result<Cnpj> Criar(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return Error.Validation("CNPJ.Invalido", "CNPJ é obrigatório.");

        var cnpj = new Cnpj(numero);

        if (!cnpj.ValidarDocumento(numero))
            return Error.Validation("CNPJ.Invalido", "CNPJ inválido.");

        return cnpj;
    }

    protected override bool ValidarDocumento(string numero)
    {
        return CnpjValidation.Validate(numero);
    }
}
