using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace GestioLan.API.Models;

public partial class GestioLanContext : DbContext
{
    public GestioLanContext()
    {
    }

    public GestioLanContext(DbContextOptions<GestioLanContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            Console.WriteLine("Warning: Using hardcoded connection string. Consider moving it to configuration.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("armscii8_general_ci")
            .HasCharSet("armscii8");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.IdCategory).HasName("PRIMARY");

            entity.ToTable("category");

            // Conrolla che id_category sia una potenza di 2 (1, 2, 4, 8, 16, 32)
            entity.ToTable(t => t.HasCheckConstraint("CK_Category_Bitmask", "(id_category > 0) AND (id_category & (id_category - 1) = 0)"));

            entity.HasIndex(e => e.NameCategory, "nome_categoria_UNIQUE").IsUnique();

            entity.Property(e => e.IdCategory)
                .ValueGeneratedNever()
                .HasColumnName("id_category");
            entity.Property(e => e.NameCategory)
                .HasMaxLength(100)
                .HasColumnName("name_category");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.IdItem).HasName("PRIMARY");

            entity.ToTable("items");

            entity.Property(e => e.IdItem)
                .HasColumnName("id_item");
            entity.Property(e => e.ItemName)
                .HasMaxLength(64)
                .HasColumnName("item_name");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IdImage)
                .HasColumnName("id_image");
            entity.Property(e => e.ImageName)
                .HasMaxLength(64)
                .HasColumnName("image_name");
            entity.Property(e => e.IdCategory)
                .HasColumnName("id_category");
            entity.Property(e => e.Quantity)
                .HasColumnName("quantity");
            entity.Property(e => e.TypeQuantity)
                .HasMaxLength(45)
                .HasColumnName("type_quantity");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Username);
            entity.ToTable("user");

            entity.Property(e => e.CreateTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("create_time");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(64)
                .HasColumnName("password");
            entity.Property(e => e.Username)
                .HasMaxLength(32)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.ToTable("images");
            entity.HasKey(e => e.IdImage);
            entity.Property(e => e.IdImage)
                .HasColumnName("id_image");
            entity.Property(e => e.FileName)
                .HasColumnName("file_name")
                .IsRequired();
            entity.Property(e => e.ItemsCount)
                .HasColumnName("items_count")
                .HasDefaultValue(0);
            entity.Property(e => e.LastModified)
                .HasColumnName("last_modified")
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Prima di salvare, cerca tutti i record Image modificati
        var modifiedImages = ChangeTracker.Entries<Image>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedImages)
        {
            entry.Entity.LastModified = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
