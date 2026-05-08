using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PecasInsumos.Domain;
using System.Reflection;

namespace PecasInsumos.Infrastructure.Persistence;

internal sealed class PecaInsumoConfiguration : IEntityTypeConfiguration<PecaInsumo>
{
    private static readonly ConstructorInfo _dinheiroCtor =
        typeof(Dinheiro).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(decimal) }, null)!;

    public void Configure(EntityTypeBuilder<PecaInsumo> builder)
    {
        builder.ToTable("peca_insumo");

        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new PecaInsumoId(value));

        builder.Property(pi => pi.Nome)
            .HasColumnName("nome")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pi => pi.Descricao)
            .HasColumnName("descricao");

        builder.Property(pi => pi.PrecoUnitario)
            .HasColumnName("preco_unitario")
            .HasPrecision(10, 2)
            .IsRequired()
            .HasConversion(
                v => v.Valor,
                v => (Dinheiro)_dinheiroCtor.Invoke(new object[] { v }));

        builder.Property(pi => pi.QuantidadeEmEstoque)
            .HasColumnName("quantidade_estoque")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(pi => pi.UnidadeDeMedida)
            .HasColumnName("unidade_medida")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

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

