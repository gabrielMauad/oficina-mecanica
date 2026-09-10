using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdensServico.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaStatusAlteradoEm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Coluna criada como nullable para permitir o backfill das linhas já existentes antes
            // de aplicar a restrição NOT NULL — não existe, no histórico, um valor melhor do que
            // updated_at para aproximar "quando a OS entrou no status atual".
            migrationBuilder.AddColumn<DateTime>(
                name: "status_alterado_em",
                schema: "ordem_servico",
                table: "ordem_servico",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE ordem_servico.ordem_servico
                SET status_alterado_em = updated_at
                WHERE status_alterado_em IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "status_alterado_em",
                schema: "ordem_servico",
                table: "ordem_servico",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status_alterado_em",
                schema: "ordem_servico",
                table: "ordem_servico");
        }
    }
}
