using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdensServico.Adapters.DataSources.Records;

namespace OrdensServico.Infrastructure.Persistence.Configurations;

internal sealed class OrcamentoConfiguration : IEntityTypeConfiguration<OrcamentoRecord>
{
    public void Configure(EntityTypeBuilder<OrcamentoRecord> builder)
    {
        builder.ToTable("orcamento");

        builder.HasKey(or => or.Id);
        builder.Property(or => or.Id)
            .HasColumnName("id");

        builder.Property(or => or.ValorTotal)
            .HasColumnName("valor_total")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(or => or.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(os => os.DataGeracao)
            .HasColumnName("data_geracao")
            .IsRequired();

        builder.Property(os => os.DataEnvio)
            .HasColumnName("data_envio");

        builder.Property(os => os.DataAprovacao)
            .HasColumnName("data_aprovacao");
    }
}
