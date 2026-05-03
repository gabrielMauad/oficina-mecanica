using System.Reflection;
using Cadastro.Domain.Cliente;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadastro.Infrastructure.Persistence.Configurations;

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    private static readonly ConstructorInfo _cpfCtor =
        typeof(Cpf).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null)!;

    private static readonly ConstructorInfo _cnpjCtor =
        typeof(Cnpj).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null)!;

    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("cliente", t =>
            t.HasCheckConstraint("CK_cliente_documento_digits", "documento ~ '^[0-9]+$'"));


        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new ClienteId(value));

        builder.Property(c => c.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Documento)
            .HasColumnName("documento")
            .HasMaxLength(14)
            .IsRequired()
            .HasConversion(
                d => d.Numero,
                n => n.Length <= 11
                    ? (Documento)(Cpf)_cpfCtor.Invoke(new object[] { n })
                    : (Documento)(Cnpj)_cnpjCtor.Invoke(new object[] { n }));

        builder.HasIndex(c => c.Documento).IsUnique();

        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Telefone)
            .HasColumnName("telefone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(c => c.CadastradoEm)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.AtualizadoEm)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
