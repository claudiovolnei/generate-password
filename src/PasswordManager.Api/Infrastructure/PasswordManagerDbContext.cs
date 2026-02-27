using Microsoft.EntityFrameworkCore;
using PasswordManager.Api.Models;

namespace PasswordManager.Api.Infrastructure;

public class PasswordManagerDbContext(DbContextOptions<PasswordManagerDbContext> options) : DbContext(options)
{
    public DbSet<PasswordEntry> PasswordEntries => Set<PasswordEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordEntry>(entity =>
        {
            entity.ToTable("PasswordEntries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.Username)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.Secret)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(item => item.CreatedAtUtc)
                .IsRequired();
        });
    }
}
