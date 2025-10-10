using Microsoft.EntityFrameworkCore;
using HortalisCSharp.Models;

namespace HortalisCSharp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Horta> Hortas => Set<Horta>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Horta>(entity =>
            {
                entity.Property(h => h.Nome).IsRequired().HasMaxLength(160);
                entity.Property(h => h.Descricao).HasMaxLength(2000);
                entity.Property(h => h.Produtos).HasMaxLength(1000);
                entity.Property(h => h.Foto).HasMaxLength(500);
                entity.Property(h => h.Telefone).HasMaxLength(40);

                entity.HasIndex(h => h.Nome);
                entity.HasIndex(h => new { h.Latitude, h.Longitude });

                entity.HasOne(h => h.Usuario)
                      .WithMany()
                      .HasForeignKey(h => h.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}