using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdensServico.Adapters.DataSources.Records;

namespace OrdensServico.Infrastructure.Persistence.Configurations;

internal sealed class ItemServicoConfiguration : IEntityTypeConfiguration<ItemServicoRecord>
{
    public void Configure(EntityTypeBuilder<ItemServicoRecord> builder)
    {
        builder.ToTable("os_servico");

        builder.HasKey(is_ => is_.Id);
        builder.Property(is_ => is_.Id)
            .HasColumnName("id");

        builder.Property(is_ => is_.ServicoId)
            .HasColumnName("servico_id")
            .IsRequired();

        builder.Property(is_ => is_.Quantidade)
            .HasColumnName("quantidade")
            .IsRequired();

        builder.Property(is_ => is_.PrecoUnitarioSnapshot)
            .HasColumnName("preco_unitario_snapshot")
            .HasPrecision(10, 2)
            .IsRequired();
    }
}
