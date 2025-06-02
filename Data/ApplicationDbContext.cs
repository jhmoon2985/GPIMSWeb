using Microsoft.EntityFrameworkCore;
using GPIMSWeb.Models;

namespace GPIMSWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // 기존 코드는 동일...
        public ApplicationDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
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

            // 기존 User, Equipment, Channel 설정은 동일...
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

            modelBuilder.Entity<Channel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.EquipmentId, e.ChannelNumber }).IsUnique();
                entity.Property(e => e.ScheduleName).HasMaxLength(100);
            });

            modelBuilder.Entity<SystemSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Key).IsUnique();
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Seed default data - 수정된 부분
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

            // Default Equipment 데이터 추가 - 새로 추가된 부분
            modelBuilder.Entity<Equipment>().HasData(
                new Equipment
                {
                    Id = 1,
                    Name = "GPIMS-001",
                    IsOnline = true,
                    LastUpdateTime = DateTime.Now,
                    Version = "v2.1.0",
                    Status = EquipmentStatus.Idle
                },
                new Equipment
                {
                    Id = 2,
                    Name = "GPIMS-002",
                    IsOnline = true,
                    LastUpdateTime = DateTime.Now,
                    Version = "v2.1.0",
                    Status = EquipmentStatus.Idle
                },
                new Equipment
                {
                    Id = 3,
                    Name = "GPIMS-003",
                    IsOnline = false,
                    LastUpdateTime = DateTime.Now.AddMinutes(-30),
                    Version = "v2.0.5",
                    Status = EquipmentStatus.Error
                },
                new Equipment
                {
                    Id = 4,
                    Name = "GPIMS-004",
                    IsOnline = true,
                    LastUpdateTime = DateTime.Now,
                    Version = "v2.1.0",
                    Status = EquipmentStatus.Running
                }
            );

            // 각 장비별 샘플 채널 데이터 추가
            var channels = new List<Channel>();
            for (int equipmentId = 1; equipmentId <= 4; equipmentId++)
            {
                for (int channelNum = 1; channelNum <= 8; channelNum++)
                {
                    channels.Add(new Channel
                    {
                        Id = (equipmentId - 1) * 8 + channelNum,
                        EquipmentId = equipmentId,
                        ChannelNumber = channelNum,
                        Status = channelNum <= 4 ? ChannelStatus.Active : ChannelStatus.Idle,
                        Mode = channelNum % 2 == 0 ? ChannelMode.Charge : ChannelMode.Discharge,
                        Voltage = 3.7 + (channelNum * 0.1),
                        Current = channelNum <= 4 ? 1.5 + (channelNum * 0.2) : 0,
                        Capacity = 50.0 + (channelNum * 5),
                        Power = channelNum <= 4 ? (3.7 + (channelNum * 0.1)) * (1.5 + (channelNum * 0.2)) : 0,
                        Energy = channelNum <= 4 ? 25.5 + (channelNum * 3) : 0,
                        ScheduleName = channelNum <= 4 ? $"Schedule_{channelNum}" : "",
                        LastUpdateTime = DateTime.Now
                    });
                }
            }
            modelBuilder.Entity<Channel>().HasData(channels);
        }
    }
}