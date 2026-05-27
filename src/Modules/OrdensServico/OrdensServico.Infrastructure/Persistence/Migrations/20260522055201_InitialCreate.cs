using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdensServico.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ordem_servico");

            migrationBuilder.CreateTable(
                name: "ordem_servico",
                schema: "ordem_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    veiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    descricao_diagnostico = table.Column<string>(type: "text", nullable: true),
                    notificado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    entregue_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordem_servico", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orcamento",
                schema: "ordem_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_geracao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_envio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_aprovacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento", x => x.id);
                    table.ForeignKey(
                        name: "FK_orcamento_ordem_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalSchema: "ordem_servico",
                        principalTable: "ordem_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "os_peca",
                schema: "ordem_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    peca_insumo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_unitario_snapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_os_peca", x => x.id);
                    table.ForeignKey(
                        name: "FK_os_peca_ordem_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalSchema: "ordem_servico",
                        principalTable: "ordem_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "os_servico",
                schema: "ordem_servico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    servico_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    preco_unitario_snapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ordem_servico_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_os_servico", x => x.id);
                    table.ForeignKey(
                        name: "FK_os_servico_ordem_servico_ordem_servico_id",
                        column: x => x.ordem_servico_id,
                        principalSchema: "ordem_servico",
                        principalTable: "ordem_servico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orcamento_ordem_servico_id",
                schema: "ordem_servico",
                table: "orcamento",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "IX_os_peca_ordem_servico_id",
                schema: "ordem_servico",
                table: "os_peca",
                column: "ordem_servico_id");

            migrationBuilder.CreateIndex(
                name: "IX_os_servico_ordem_servico_id",
                schema: "ordem_servico",
                table: "os_servico",
                column: "ordem_servico_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orcamento",
                schema: "ordem_servico");

            migrationBuilder.DropTable(
                name: "os_peca",
                schema: "ordem_servico");

            migrationBuilder.DropTable(
                name: "os_servico",
                schema: "ordem_servico");

            migrationBuilder.DropTable(
                name: "ordem_servico",
                schema: "ordem_servico");
        }
    }
}
