using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Infrastructure.Persistence.Configurations;

internal sealed class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("orcamento");

        builder.HasKey(or => or.Id);
        builder.Property(or => or.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new OrcamentoId(value));

        builder.Property(or => or.ValorTotal)
            .HasColumnName("valor_total")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(or => or.Status)
            .HasColumnName("status")
            .HasConversion<string>()
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
