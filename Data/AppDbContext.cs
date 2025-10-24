using Microsoft.EntityFrameworkCore;
using HortalisCSharp.Models;

namespace HortalisCSharp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Horta> Hortas => Set<Horta>();

        public DbSet<Produto> Produtos => Set<Produto>();
        public DbSet<HortaProduto> HortaProdutos => Set<HortaProduto>();

        // Novo DbSet para indicações
        public DbSet<Indicacao> Indicacoes => Set<Indicacao>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Horta>(entity =>
            {
                entity.Property(h => h.Nome).IsRequired().HasMaxLength(160);
                entity.Property(h => h.Descricao).HasMaxLength(2000);
                entity.Property(h => h.Foto).HasMaxLength(500);
                entity.Property(h => h.Telefone).HasMaxLength(40);

                entity.HasIndex(h => h.Nome);
                entity.HasIndex(h => new { h.Latitude, h.Longitude });

                entity.HasOne(h => h.Usuario)
                      .WithMany()
                      .HasForeignKey(h => h.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Produto>(p =>
            {
                p.Property(x => x.Nome).IsRequired().HasMaxLength(120);
                p.HasIndex(x => x.Nome).IsUnique();
            });

            modelBuilder.Entity<HortaProduto>(hp =>
            {
                hp.HasKey(x => new { x.HortaId, x.ProdutoId });

                hp.HasOne(x => x.Horta)
                  .WithMany(h => h.HortaProdutos)
                  .HasForeignKey(x => x.HortaId)
                  .OnDelete(DeleteBehavior.Cascade);

                hp.HasOne(x => x.Produto)
                  .WithMany(p => p.HortaProdutos)
                  .HasForeignKey(x => x.ProdutoId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuração simples para Indicacao
            modelBuilder.Entity<Indicacao>(i =>
            {
                i.Property(x => x.AreaNome).HasMaxLength(120);
                i.HasIndex(x => x.AreaNome);
                i.HasIndex(x => new { x.Latitude, x.Longitude });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}