using Cadastro.Domain.Cliente;
using Cadastro.Domain.Veiculo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection;

namespace Cadastro.Infrastructure.Persistence.Configurations;

internal sealed class VeiculoConfiguration : IEntityTypeConfiguration<Veiculo>
{
    private static readonly ConstructorInfo _placaCtor =
        typeof(Placa).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null)!;

    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.ToTable("veiculo");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new VeiculoId(value));

        builder.Property(v => v.Placa)
            .HasColumnName("placa")
            .HasMaxLength(8)
            .IsRequired()
            .HasConversion(
                p => p.Numero,
                n => (Placa)_placaCtor.Invoke(new object[] { n }));

        builder.HasIndex(v => v.Placa).IsUnique();

        builder.Property(v => v.Modelo)
            .HasColumnName("modelo")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Marca)
            .HasColumnName("marca")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.Ano)
            .HasColumnName("ano")
            .IsRequired();

        builder.Property(v => v.ClienteId)
            .HasColumnName("cliente_id")
            .HasConversion(id => id.Value, value => new ClienteId(value))
            .IsRequired();

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .IsRequired();

        builder.Property(v => v.CadastradoEm)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(v => v.AtualizadoEm)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
