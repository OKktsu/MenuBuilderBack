using MenuBuilderBack.Models;
using MenuBuilderBack.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MenuBuilderBack.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<CargoPermissao> CargoPermissoes { get; set; }
        public DbSet<ConviteEmpresa> ConvitesEmpresa { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<MenuItemOverride> MenuItemOverrides { get; set; }
        public DbSet<SessaoMesa> SessoesMesa { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoItem> PedidoItens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");

            // ── Empresa ────────────────────────────────────────────────────
            modelBuilder.Entity<Empresa>()
                .HasIndex(e => e.CodigoConvite)
                .IsUnique();

            modelBuilder.Entity<Empresa>()
                .HasIndex(e => e.Slug)
                .IsUnique();

            // Auto-referência: empresa mãe → filhas
            modelBuilder.Entity<Empresa>()
                .HasOne(e => e.EmpresaMae)
                .WithMany(e => e.Filhas)
                .HasForeignKey(e => e.EmpresaMaeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Empresa>()
                .HasMany(e => e.Funcionarios)
                .WithOne(u => u.Empresa)
                .HasForeignKey(u => u.EmpresaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Empresa>()
                .HasMany(e => e.Cargos)
                .WithOne(c => c.Empresa)
                .HasForeignKey(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Cargo ───────────────────────────────────────────────────────
            modelBuilder.Entity<Cargo>()
                .HasMany(c => c.Permissoes)
                .WithOne(p => p.Cargo)
                .HasForeignKey(p => p.CargoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cargo>()
                .HasMany(c => c.Usuarios)
                .WithOne(u => u.Cargo)
                .HasForeignKey(u => u.CargoId)
                .OnDelete(DeleteBehavior.SetNull);

            // Persiste enum como string legível no banco
            modelBuilder.Entity<CargoPermissao>()
                .Property(cp => cp.Permissao)
                .HasConversion<string>();

            // ── ConviteEmpresa ──────────────────────────────────────────────
            modelBuilder.Entity<ConviteEmpresa>()
                .HasIndex(c => c.Token)
                .IsUnique();

            modelBuilder.Entity<ConviteEmpresa>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ConviteEmpresa>()
                .HasOne(c => c.Empresa)
                .WithMany(e => e.Convites)
                .HasForeignKey(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConviteEmpresa>()
                .HasOne(c => c.Cargo)
                .WithMany()
                .HasForeignKey(c => c.CargoId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Menu ────────────────────────────────────────────────────────
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Empresa)
                .WithMany(e => e.Menus)
                .HasForeignKey(m => m.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Menu>()
                .HasMany(m => m.Categories)
                .WithOne(c => c.Menu)
                .HasForeignKey(c => c.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Category >--< MenuItem (Many-to-Many) ───────────────────────
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Items)
                .WithMany(i => i.Categories)
                .UsingEntity("CategoryMenuItem");

            modelBuilder.Entity<MenuItem>()
                .HasOne(i => i.Empresa)
                .WithMany()
                .HasForeignKey(i => i.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItem>()
                .Property(i => i.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<MenuItem>()
                .Property(i => i.Tags)
                .HasColumnType("text[]");

            // ── MenuItemOverride ────────────────────────────────────────────
            // Uma única sobrescrita por item por empresa
            modelBuilder.Entity<MenuItemOverride>()
                .HasIndex(o => new { o.EmpresaId, o.MenuItemId })
                .IsUnique();

            modelBuilder.Entity<MenuItemOverride>()
                .HasOne(o => o.Empresa)
                .WithMany()
                .HasForeignKey(o => o.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItemOverride>()
                .HasOne(o => o.MenuItem)
                .WithMany()
                .HasForeignKey(o => o.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItemOverride>()
                .Property(o => o.Price)
                .HasColumnType("decimal(10,2)");

            // ── SessaoMesa ──────────────────────────────────────────────────
            modelBuilder.Entity<SessaoMesa>()
                .HasIndex(s => s.Token)
                .IsUnique();

            modelBuilder.Entity<SessaoMesa>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<SessaoMesa>()
                .HasOne(s => s.Empresa)
                .WithMany()
                .HasForeignKey(s => s.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessaoMesa>()
                .HasMany(s => s.Pedidos)
                .WithOne(p => p.SessaoMesa)
                .HasForeignKey(p => p.SessaoMesaId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Pedido ──────────────────────────────────────────────────────
            modelBuilder.Entity<Pedido>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Pedido>()
                .HasMany(p => p.Itens)
                .WithOne(i => i.Pedido)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── PedidoItem ──────────────────────────────────────────────────
            modelBuilder.Entity<PedidoItem>()
                .Property(i => i.PrecoUnitario)
                .HasColumnType("decimal(10,2)");
        }
    }
}