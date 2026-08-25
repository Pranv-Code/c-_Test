using Microsoft.EntityFrameworkCore;
using TestApp.Api.Models;

namespace TestApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.CommentText).HasColumnName("Comment").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
        });
    }
}
