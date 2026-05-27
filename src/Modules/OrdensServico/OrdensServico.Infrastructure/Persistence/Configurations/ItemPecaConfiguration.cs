using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Infrastructure.Persistence.Configurations;

internal sealed class ItemPecaConfiguration : IEntityTypeConfiguration<ItemPeca>
{
    public void Configure(EntityTypeBuilder<ItemPeca> builder)
    {
        builder.ToTable("os_peca");

        builder.HasKey(ip => ip.Id);
        builder.Property(ip => ip.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new ItemPecaId(value));

        builder.Property(ip => ip.PecaInsumoId)
            .HasColumnName("peca_insumo_id")
            .IsRequired();

        builder.Property(ip => ip.Quantidade)
            .HasColumnName("quantidade")
            .IsRequired();

        builder.Property(ip => ip.PrecoUnitarioSnapshot)
            .HasColumnName("preco_unitario_snapshot")
            .HasPrecision(10, 2)
            .IsRequired();
    }
}
