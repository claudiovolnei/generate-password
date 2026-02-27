using Microsoft.EntityFrameworkCore;
using PasswordManager.Api.Models;

namespace PasswordManager.Api.Infrastructure;

public class PasswordManagerDbContext(DbContextOptions<PasswordManagerDbContext> options) : DbContext(options)
{
    public DbSet<PasswordEntry> PasswordEntries => Set<PasswordEntry>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

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

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("UserAccounts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Username)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.Password)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(item => item.CreatedAtUtc)
                .IsRequired();
            entity.HasIndex(item => item.Username)
                .IsUnique();

            entity.HasData(
                new UserAccount
                {
                    Id = new Guid("5D9F4954-0A14-4739-B0E0-6F6470D8C415"),
                    Username = "admin",
                    Password = "Admin@123",
                    CreatedAtUtc = new DateTime(2026, 2, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new UserAccount
                {
                    Id = new Guid("2A0F7394-DBE4-4A65-9556-50D53FA4F141"),
                    Username = "gestor",
                    Password = "Gestor@123",
                    CreatedAtUtc = new DateTime(2026, 2, 27, 0, 0, 0, DateTimeKind.Utc)
                });
        });
    }
}
