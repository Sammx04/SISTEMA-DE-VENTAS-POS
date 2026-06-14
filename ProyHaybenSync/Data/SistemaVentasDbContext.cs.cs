using Microsoft.EntityFrameworkCore;
using ProySistemaVentas.Models;


namespace ProySistemaVentas.Data
{
    public class SistemaVentasDbContext : DbContext
    {
        public SistemaVentasDbContext(DbContextOptions<SistemaVentasDbContext> options)
            : base(options)
        {
        }

        public DbSet<Rol> Roles { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Caja> Cajas { get; set; }

        public DbSet<Venta> Ventas { get; set; }

        public DbSet<DetalleVenta> DetallesVenta { get; set; }
    }
}
