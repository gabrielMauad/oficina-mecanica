using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrdensServico.Domain.OrdemServico;

namespace OrdensServico.Infrastructure.Persistence.Configurations;

internal sealed class OrdemServicoConfiguration : IEntityTypeConfiguration<OrdemServico>
{
    public void Configure(EntityTypeBuilder<OrdemServico> builder)
    {
        builder.ToTable("ordem_servico");

        builder.HasKey(os => os.Id);
        builder.Property(os => os.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new OrdemServicoId(value));

        builder.Property(os => os.ClienteId)
            .HasColumnName("cliente_id")
            .IsRequired();

        builder.Property(os => os.VeiculoId)
            .HasColumnName("veiculo_id")
            .IsRequired();

        builder.Property(os => os.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(os => os.DescricaoDiagnostico)
            .HasColumnName("descricao_diagnostico");

        builder.Property(os => os.NotificadoEm)
            .HasColumnName("notificado_em");

        builder.Property(os => os.EntregueEm)
            .HasColumnName("entregue_em");

        builder.Property(s => s.CriadoEm)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.AtualizadoEm)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasMany(os => os.ItensServico)
            .WithOne()
            .HasForeignKey("ordem_servico_id")
            .IsRequired();

        builder.Navigation(os => os.ItensServico)
            .HasField("_itensServico")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(os => os.ItensPeca)
            .WithOne()
            .HasForeignKey("ordem_servico_id")
            .IsRequired();

        builder.Navigation(os => os.ItensPeca)
            .HasField("_itensPeca")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(os => os.Orcamentos)
            .WithOne()
            .HasForeignKey("ordem_servico_id")
            .IsRequired();

        builder.Navigation(os => os.Orcamentos)
            .HasField("_orcamentos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
