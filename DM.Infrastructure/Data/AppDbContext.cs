using DM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DM.Infrastructure.Data;

public class AppDbContext : DbContext
{
      public DbSet<User> Users => Set<User>();
      public DbSet<Device> Devices => Set<Device>();
      public DbSet<UserSession> UserSessions => Set<UserSession>();
      public DbSet<Chat> Chats => Set<Chat>();
      public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
      public DbSet<Message> Messages => Set<Message>();
      public DbSet<MessageDelivery> MessageDeliveries => Set<MessageDelivery>();
      public DbSet<IdentityKey> IdentityKeys => Set<IdentityKey>();
      public DbSet<DeviceKey> DeviceKeys => Set<DeviceKey>();
      public DbSet<SessionKey> SessionKeys => Set<SessionKey>();

      public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
      {
      }

      protected override void OnModelCreating(ModelBuilder model)
      {
            // USER
            model.Entity<User>(entity =>
            {
                  entity.ToTable("users");
                  entity.HasKey(u => u.UserId);

                  entity.HasIndex(u => u.UserName).IsUnique();

                  entity.Property(u => u.UserName).HasMaxLength(50);
                  entity.Property(u => u.DisplayName).HasMaxLength(100);
            });

            // DEVICE
            model.Entity<Device>(entity =>
            {
                  entity.ToTable("devices");
                  entity.HasKey(d => d.DeviceId);

                  entity.HasOne(d => d.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(d => d.UserId);
            });

            // USERSESSION
            model.Entity<UserSession>(entity =>
            {
                  entity.ToTable("user_sessions");
                  entity.HasKey(s => s.SessionId);

                  entity.HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId);

                  entity.HasOne(s => s.Device)
                .WithMany(d => d.Sessions)
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);
            });

            // CHAT
            model.Entity<Chat>(entity =>
            {
                  entity.ToTable("chats");
                  entity.HasKey(c => c.ChatId);

                  entity.Property(c => c.Type).HasConversion<int>();

                  entity.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            // CHATMEMBER – PK compuesta
            model.Entity<ChatMember>(entity =>
            {
                  entity.ToTable("chat_members");
                  entity.HasKey(cm => new { cm.ChatId, cm.UserId });

                  entity.Property(cm => cm.Role).HasConversion<int>();

                  entity.HasOne(cm => cm.Chat)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ChatId);

                  entity.HasOne(cm => cm.User)
                .WithMany()
                .HasForeignKey(cm => cm.UserId);
            });

            // MESSAGE
            model.Entity<Message>(entity =>
            {
                  entity.ToTable("messages");
                  entity.HasKey(m => m.MessageId);

                  entity.Property(m => m.PayloadType).HasConversion<int>();

                  entity.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId);

                  entity.HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            // MESSAGEDELIVERY – PK compuesta POR USUARIO
            model.Entity<MessageDelivery>(entity =>
            {
                  entity.ToTable("message_deliveries");
                  entity.HasKey(md => new { md.MessageId, md.UserId });

                  entity.HasOne(md => md.Message)
                .WithMany(m => m.Deliveries)
                .HasForeignKey(md => md.MessageId);

                  entity.HasOne(md => md.User)
                .WithMany()
                .HasForeignKey(md => md.UserId);
            });

            // IDENTITY KEY (1–1 con User)
            model.Entity<IdentityKey>(entity =>
            {
                  entity.ToTable("identity_keys");
                  entity.HasKey(k => k.UserId);

                  entity.HasOne(k => k.User)
                .WithOne(u => u.IdentityKey)
                .HasForeignKey<IdentityKey>(k => k.UserId);
            });

            // DEVICE KEY (1–1 con Device)
            model.Entity<DeviceKey>(entity =>
            {
                  entity.ToTable("device_keys");
                  entity.HasKey(k => k.DeviceId);

                  entity.HasOne(k => k.Device)
                .WithOne(d => d.DeviceKey)
                .HasForeignKey<DeviceKey>(k => k.DeviceId);
            });

            // SESSION KEY
            model.Entity<SessionKey>(entity =>
            {
                  entity.ToTable("session_keys");
                  entity.HasKey(sk => sk.SessionKeyId);

                  entity.HasOne(sk => sk.Chat)
                .WithMany()
                .HasForeignKey(sk => sk.ChatId);

                  entity.HasOne(sk => sk.LocalDevice)
                .WithMany()
                .HasForeignKey(sk => sk.LocalDeviceId)
                .OnDelete(DeleteBehavior.Restrict);

                  entity.HasOne(sk => sk.RemoteDevice)
                .WithMany()
                .HasForeignKey(sk => sk.RemoteDeviceId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            base.OnModelCreating(model);
      }

}
