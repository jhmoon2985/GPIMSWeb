using Microsoft.EntityFrameworkCore;
using GPIMSWeb.Models;

namespace GPIMSWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // 디자인 타임용 기본 생성자 추가
        public ApplicationDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 디자인 타임용 연결 문자열
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GPIMSWebDB;Trusted_Connection=true;MultipleActiveResultSets=true");
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<CanLinData> CanLinData { get; set; }
        public DbSet<AuxData> AuxData { get; set; }
        public DbSet<ChamberChillerData> ChamberChillerData { get; set; }
        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<UserHistory> UserHistories { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Equipment configuration
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Version).HasMaxLength(50);
                entity.HasMany(e => e.Channels).WithOne().HasForeignKey(c => c.EquipmentId);
                entity.HasMany(e => e.CanLinData).WithOne().HasForeignKey(c => c.EquipmentId);
                entity.HasMany(e => e.AuxData).WithOne().HasForeignKey(a => a.EquipmentId);
                entity.HasMany(e => e.Alarms).WithOne().HasForeignKey(a => a.EquipmentId);
            });

            // Channel configuration
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.EquipmentId, e.ChannelNumber }).IsUnique();
                entity.Property(e => e.ScheduleName).HasMaxLength(100);
            });

            // System Settings configuration
            modelBuilder.Entity<SystemSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Seed default data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Default admin user
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Name = "System Administrator",
                Department = "IT",
                Level = UserLevel.Admin,
                CreatedAt = DateTime.Now,
                IsActive = true
            });

            // Default system settings
            modelBuilder.Entity<SystemSettings>().HasData(
                new SystemSettings 
                { 
                    Id = 1, 
                    Key = "EquipmentCount", 
                    Value = "4", 
                    Description = "Number of equipment",
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = "System"
                },
                new SystemSettings 
                { 
                    Id = 2, 
                    Key = "ChannelsPerEquipment", 
                    Value = "8", 
                    Description = "Channels per equipment",
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = "System"
                },
                new SystemSettings 
                { 
                    Id = 3, 
                    Key = "DefaultLanguage", 
                    Value = "en", 
                    Description = "Default system language",
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = "System"
                },
                new SystemSettings 
                { 
                    Id = 4, 
                    Key = "DateFormat", 
                    Value = "yyyy-MM-dd HH:mm:ss", 
                    Description = "Date format",
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = "System"
                }
            );
        }
    }
}