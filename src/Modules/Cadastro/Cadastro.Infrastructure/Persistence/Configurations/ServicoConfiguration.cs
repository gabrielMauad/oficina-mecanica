using System.Reflection;
using Cadastro.Domain.Servico;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadastro.Infrastructure.Persistence.Configurations;

internal sealed class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    private static readonly ConstructorInfo _dinheiroCtor =
        typeof(Dinheiro).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(decimal) }, null)!;

    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("servico");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new ServicoId(value));

        builder.Property(s => s.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Descricao)
            .HasColumnName("descricao");

        builder.Property(s => s.PrecoBase)
            .HasColumnName("preco_base")
            .HasPrecision(10, 2)
            .IsRequired()
            .HasConversion(
                d => d.Valor,
                v => (Dinheiro)_dinheiroCtor.Invoke(new object[] { v }));

        builder.Property(s => s.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(s => s.CadastradoEm)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.AtualizadoEm)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
