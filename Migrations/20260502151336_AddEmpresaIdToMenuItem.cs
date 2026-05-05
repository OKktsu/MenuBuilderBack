using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuBuilderBack.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaIdToMenuItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Itens sem empresa associada (dados de teste) não podem ser migrados
            migrationBuilder.Sql("DELETE FROM \"CategoryMenuItem\";");
            migrationBuilder.Sql("DELETE FROM \"MenuItems\";");

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "MenuItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_EmpresaId",
                table: "MenuItems",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Empresas_EmpresaId",
                table: "MenuItems",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Empresas_EmpresaId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_EmpresaId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "MenuItems");
        }
    }
}
