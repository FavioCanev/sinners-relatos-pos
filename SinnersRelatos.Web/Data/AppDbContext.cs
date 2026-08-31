using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<GrupoModificador> GruposModificadores => Set<GrupoModificador>();
    public DbSet<OpcionModificador> OpcionesModificadores => Set<OpcionModificador>();
    public DbSet<ProductoGrupoModificador> ProductosGruposModificadores => Set<ProductoGrupoModificador>();
    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();
    public DbSet<DetallePedidoModificador> DetallesPedidoModificadores => Set<DetallePedidoModificador>();
    public DbSet<PedidoMesa> PedidosMesas => Set<PedidoMesa>();
    public DbSet<Ingrediente> Ingredientes => Set<Ingrediente>();
    public DbSet<RecetaProducto> RecetasProducto => Set<RecetaProducto>();
    public DbSet<RecetaOpcionModificador> RecetasOpcionModificador => Set<RecetaOpcionModificador>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(u => u.NombreUsuario).IsUnique();
            entity.HasIndex(u => u.EmpleadoId).IsUnique();

            entity.HasOne(u => u.Empleado)
                .WithOne(e => e.Usuario)
                .HasForeignKey<Usuario>(u => u.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.Property(p => p.Precio).HasPrecision(10, 2);

            entity.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GrupoModificador>(entity =>
        {
            entity.HasIndex(g => g.Nombre).IsUnique();
        });

        modelBuilder.Entity<OpcionModificador>(entity =>
        {
            entity.Property(o => o.PrecioAdicional).HasPrecision(10, 2);

            entity.HasOne(o => o.GrupoModificador)
                .WithMany(g => g.Opciones)
                .HasForeignKey(o => o.GrupoModificadorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductoGrupoModificador>(entity =>
        {
            entity.HasKey(pg => new { pg.ProductoId, pg.GrupoModificadorId });

            entity.HasOne(pg => pg.Producto)
                .WithMany(p => p.GruposModificadores)
                .HasForeignKey(pg => pg.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pg => pg.GrupoModificador)
                .WithMany(g => g.Productos)
                .HasForeignKey(pg => pg.GrupoModificadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Mesa>(entity =>
        {
            entity.HasIndex(m => new { m.Marca, m.Tipo, m.Numero }).IsUnique();
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoMesa>(entity =>
        {
            entity.HasKey(pm => new { pm.PedidoId, pm.MesaId });

            entity.HasOne(pm => pm.Pedido)
                .WithMany(p => p.Mesas)
                .HasForeignKey(pm => pm.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pm => pm.Mesa)
                .WithMany(m => m.Pedidos)
                .HasForeignKey(pm => pm.MesaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetallePedido>(entity =>
        {
            entity.Property(d => d.PrecioUnitario).HasPrecision(10, 2);

            entity.HasOne(d => d.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetallePedidoModificador>(entity =>
        {
            entity.HasKey(dm => new { dm.DetallePedidoId, dm.OpcionModificadorId });
            entity.Property(dm => dm.PrecioAdicional).HasPrecision(10, 2);

            entity.HasOne(dm => dm.DetallePedido)
                .WithMany(d => d.Modificadores)
                .HasForeignKey(dm => dm.DetallePedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dm => dm.OpcionModificador)
                .WithMany()
                .HasForeignKey(dm => dm.OpcionModificadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ingrediente>(entity =>
        {
            entity.HasIndex(i => i.Nombre).IsUnique();
            entity.Property(i => i.StockActual).HasPrecision(10, 3);
            entity.Property(i => i.StockMinimo).HasPrecision(10, 3);
        });

        modelBuilder.Entity<RecetaProducto>(entity =>
        {
            entity.HasKey(r => new { r.ProductoId, r.IngredienteId });
            entity.Property(r => r.CantidadRequerida).HasPrecision(10, 3);

            entity.HasOne(r => r.Producto)
                .WithMany(p => p.Receta)
                .HasForeignKey(r => r.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Ingrediente)
                .WithMany(i => i.Recetas)
                .HasForeignKey(r => r.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecetaOpcionModificador>(entity =>
        {
            entity.HasKey(r => new { r.OpcionModificadorId, r.IngredienteId });
            entity.Property(r => r.CantidadRequerida).HasPrecision(10, 3);

            entity.HasOne(r => r.OpcionModificador)
                .WithMany(o => o.Recetas)
                .HasForeignKey(r => r.OpcionModificadorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Ingrediente)
                .WithMany(i => i.RecetasOpciones)
                .HasForeignKey(r => r.IngredienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LogAuditoria>(entity =>
        {
            entity.HasIndex(l => l.FechaHora);

            entity.HasOne(l => l.Usuario)
                .WithMany()
                .HasForeignKey(l => l.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
