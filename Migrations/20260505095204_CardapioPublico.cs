using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuBuilderBack.Migrations
{
    /// <inheritdoc />
    public partial class CardapioPublico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Compartilhado",
                table: "Menus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaMaeId",
                table: "Empresas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Empresas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MenuItemOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    MenuItemId = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemOverrides_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuItemOverrides_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_EmpresaMaeId",
                table: "Empresas",
                column: "EmpresaMaeId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Slug",
                table: "Empresas",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemOverrides_EmpresaId_MenuItemId",
                table: "MenuItemOverrides",
                columns: new[] { "EmpresaId", "MenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemOverrides_MenuItemId",
                table: "MenuItemOverrides",
                column: "MenuItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Empresas_Empresas_EmpresaMaeId",
                table: "Empresas",
                column: "EmpresaMaeId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empresas_Empresas_EmpresaMaeId",
                table: "Empresas");

            migrationBuilder.DropTable(
                name: "MenuItemOverrides");

            migrationBuilder.DropIndex(
                name: "IX_Empresas_EmpresaMaeId",
                table: "Empresas");

            migrationBuilder.DropIndex(
                name: "IX_Empresas_Slug",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Compartilhado",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "EmpresaMaeId",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Empresas");
        }
    }
}
