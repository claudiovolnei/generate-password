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
            entity.Property(item => item.UserAccountId)
                .IsRequired();
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

            entity.HasOne(item => item.UserAccount)
                .WithMany()
                .HasForeignKey(item => item.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(item => item.UserAccountId);
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
            entity.Property(item => item.RequireMobileAuthentication)
                .HasDefaultValue(true)
                .IsRequired();
            entity.Property(item => item.CreatedAtUtc)
                .IsRequired();
            entity.HasIndex(item => item.Username)
                .IsUnique();
        });
    }
}
