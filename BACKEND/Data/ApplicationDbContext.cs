using FichajeApp.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace FichajeApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Empleado> Empleados => Set<Empleado>();
        public DbSet<Fichaje> Fichajes => Set<Fichaje>();
        public DbSet<Ausencia> Ausencias => Set<Ausencia>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Empleado
            modelBuilder.Entity<Empleado>(b =>
            {
                b.Property(e => e.Rol)
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();

                b.ToTable(tb => tb.HasCheckConstraint("CK_Empleados_Rol", "\"Rol\" IN ('Admin')"));

                b.ToTable("Empleados");
                b.HasKey(e => e.Id);

                b.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
                b.Property(e => e.Apellidos).HasMaxLength(150).IsRequired();
                b.Property(e => e.DNI).HasMaxLength(15).IsRequired();
                b.Property(e => e.Email).HasMaxLength(150).IsRequired();
                b.Property(e => e.Contrasena).HasMaxLength(256).IsRequired();
                b.Property(e => e.Telefono).HasMaxLength(20);
                b.Property(e => e.Rol).HasMaxLength(50).IsRequired();

                b.HasIndex(e => e.Email).IsUnique();
            });

            // Fichaje
            modelBuilder.Entity<Fichaje>(b =>
            {
                b.ToTable("Fichajes");
                b.HasKey(f => f.Id);

                b.Property(f => f.Id)
                    .HasColumnType("integer")
                    .UseIdentityByDefaultColumn()
                    .ValueGeneratedOnAdd();

                b.Property(f => f.FechaHora)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'")
                    .IsRequired();

                b.Property(f => f.Tipo).HasMaxLength(20).IsRequired();
                b.Property(f => f.Observacion).HasMaxLength(300);

                b.HasOne(f => f.Empleado)
                    .WithMany()
                    .HasForeignKey(f => f.EmpleadoId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(f => new { f.EmpleadoId, f.FechaHora });
            });

            // Ausencia
            modelBuilder.Entity<Ausencia>(b =>
            {
                b.ToTable("Ausencias");
                b.HasKey(a => a.Id);

                b.Property(a => a.Id)
                    .HasColumnType("integer")
                    .UseIdentityByDefaultColumn()
                    .ValueGeneratedOnAdd();

                // <-- Cambiado: almacenar SIN zona horaria para mantener horas locales exactas
                b.Property(a => a.FechaInicio)
                    .HasColumnType("timestamp without time zone")
                    .IsRequired();

                b.Property(a => a.FechaFin)
                    .HasColumnType("timestamp without time zone")
                    .IsRequired();

                b.Property(a => a.Motivo)
                    .HasMaxLength(500)
                    .IsRequired();

                b.Property(a => a.Aceptada)
                    .HasColumnType("boolean");

                b.HasOne(a => a.Empleado)
                    .WithMany()
                    .HasForeignKey(a => a.EmpleadoId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(a => new { a.EmpleadoId, a.FechaInicio });
            });
        }
    }
}
