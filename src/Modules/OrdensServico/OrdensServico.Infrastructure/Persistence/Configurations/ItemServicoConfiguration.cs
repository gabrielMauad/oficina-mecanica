using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Infrastructure.Persistence.Configurations;

internal sealed class ItemServicoConfiguration : IEntityTypeConfiguration<ItemServico>
{
    public void Configure(EntityTypeBuilder<ItemServico> builder)
    {
        builder.ToTable("os_servico");

        builder.HasKey(is_ => is_.Id);
        builder.Property(is_ => is_.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new ItemServicoId(value));

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
