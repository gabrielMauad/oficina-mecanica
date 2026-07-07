using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdensServico.Adapters.DataSources.Records;

namespace OrdensServico.Infrastructure.Persistence.Configurations;

internal sealed class ItemPecaConfiguration : IEntityTypeConfiguration<ItemPecaRecord>
{
    public void Configure(EntityTypeBuilder<ItemPecaRecord> builder)
    {
        builder.ToTable("os_peca");

        builder.HasKey(ip => ip.Id);
        builder.Property(ip => ip.Id)
            .HasColumnName("id");

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
