using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Web.Models
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatRoomUser> ChatRoomUsers { get; set; }
        public DbSet<ChatRoomRequest> ChatRoomRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ChatRoomUser için composite key ve ilişkiler
            builder.Entity<ChatRoomUser>(entity =>
            {
                // Composite key tanımlama
                entity.HasKey(cu => new { cu.ChatRoomId, cu.UserId });

                // ChatRoom ilişkisi
                entity.HasOne(cu => cu.ChatRoom)
                    .WithMany(c => c.Users)
                    .HasForeignKey(cu => cu.ChatRoomId)
                    .OnDelete(DeleteBehavior.NoAction); // Cascade yerine NoAction kullanıyoruz

                // User ilişkisi
                entity.HasOne(cu => cu.User)
                    .WithMany()
                    .HasForeignKey(cu => cu.UserId)
                    .OnDelete(DeleteBehavior.NoAction); // Cascade yerine NoAction kullanıyoruz
            });

            // ChatMessage ilişkileri
            builder.Entity<ChatMessage>(entity =>
            {
                // Sender ilişkisi
                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ChatRoom ilişkisi
                entity.HasOne(m => m.ChatRoom)
                    .WithMany(r => r.Messages)
                    .HasForeignKey(m => m.ChatRoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Content alanı nullable olabilir
                entity.Property(m => m.Content)
                    .IsRequired(false);

                // ImagePath alanı nullable olabilir
                entity.Property(m => m.ImagePath)
                    .IsRequired(false);
            });

            // Index uzunluğunu sınırlamak için
            builder.Entity<ChatRoomUser>()
                .HasIndex(e => new { e.ChatRoomId, e.UserId })
                .HasDatabaseName("IX_ChatRoomUsers_Composite")
                .IsUnique();

            // Sabit GUID'ler
            const string ADMIN_ROLE_ID = "8D04DCE2-969A-435D-BBA4-DF3F325983DC";
            const string USER_ROLE_ID = "2C5E174E-3B0E-446F-86AF-483D56FD7210";
            const string ADMIN_ID = "69BD714F-9576-45BA-B5B7-F00649BE00DE";

            // Admin rolü
            builder.Entity<AppRole>().HasData(
                new AppRole
                {
                    Id = ADMIN_ROLE_ID,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );

            // User rolü
            builder.Entity<AppRole>().HasData(
                new AppRole
                {
                    Id = USER_ROLE_ID,
                    Name = "User",
                    NormalizedName = "USER",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                }
            );

            // Admin kullanıcısı
            builder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = ADMIN_ID,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "baran@admin.com",
                    NormalizedEmail = "BARAN@ADMIN.COM",
                    EmailConfirmed = true,
                    Name = "Admin",
                    Surname = "User",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    PasswordHash = new PasswordHasher<AppUser>().HashPassword(null, "Baran123!")
                }
            );

            // Admin rolü ataması
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    UserId = ADMIN_ID,
                    RoleId = ADMIN_ROLE_ID
                }
            );

            // Örnek bir sohbet odası
            builder.Entity<ChatRoom>().HasData(
                new ChatRoom
                {
                    Id = 1,
                    Name = "Genel Sohbet",
                    Description = "Herkesin katılabileceği genel sohbet odası",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedById = ADMIN_ID,
                    ImagePath = "/images/general-chat.png"
                }
            );

            // ChatRoomRequest konfigürasyonu
            builder.Entity<ChatRoomRequest>()
                .HasOne(r => r.ChatRoom)
                .WithMany()
                .HasForeignKey(r => r.ChatRoomId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ChatRoomRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            base.OnModelCreating(builder);
        }
    }
}
