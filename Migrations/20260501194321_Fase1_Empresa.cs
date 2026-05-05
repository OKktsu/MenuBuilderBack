using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuBuilderBack.Migrations
{
    /// <inheritdoc />
    public partial class Fase1_Empresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Menus_Users_UserId",
                table: "Menus");

            migrationBuilder.DropIndex(
                name: "IX_Menus_UserId",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Menus");

            migrationBuilder.AddColumn<int>(
                name: "CargoId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOwner",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Menus",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CodigoConvite = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cargos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cargos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CargoPermissoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargoId = table.Column<int>(type: "integer", nullable: false),
                    Permissao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoPermissoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargoPermissoes_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConvitesEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    CargoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvitesEmpresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConvitesEmpresa_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConvitesEmpresa_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CargoId",
                table: "Users",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmpresaId",
                table: "Users",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_EmpresaId",
                table: "Menus",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CargoPermissoes_CargoId",
                table: "CargoPermissoes",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cargos_EmpresaId",
                table: "Cargos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesEmpresa_CargoId",
                table: "ConvitesEmpresa",
                column: "CargoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesEmpresa_EmpresaId",
                table: "ConvitesEmpresa",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesEmpresa_Token",
                table: "ConvitesEmpresa",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_CodigoConvite",
                table: "Empresas",
                column: "CodigoConvite",
                unique: true);

            // Menus do sistema antigo (por usuário) não podem ser migrados — limpa antes de criar o FK
            migrationBuilder.Sql("DELETE FROM \"Menus\";");
            migrationBuilder.Sql("DELETE FROM \"Categories\";");

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_Empresas_EmpresaId",
                table: "Menus",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Cargos_CargoId",
                table: "Users",
                column: "CargoId",
                principalTable: "Cargos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Empresas_EmpresaId",
                table: "Users",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Menus_Empresas_EmpresaId",
                table: "Menus");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Cargos_CargoId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Empresas_EmpresaId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "CargoPermissoes");

            migrationBuilder.DropTable(
                name: "ConvitesEmpresa");

            migrationBuilder.DropTable(
                name: "Cargos");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropIndex(
                name: "IX_Users_CargoId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmpresaId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Menus_EmpresaId",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "CargoId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsOwner",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Menus");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Menus",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_UserId",
                table: "Menus",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_Users_UserId",
                table: "Menus",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
