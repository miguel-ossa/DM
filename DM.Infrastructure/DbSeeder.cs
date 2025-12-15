using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DM.Domain.Entities;
using DM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DM.Infrastructure.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        Console.WriteLine("Reseteando datos de prueba...");

        // 1) Limpiar BD (excepto __EFMigrationsHistory)
        await ClearDatabaseAsync(db);

        Console.WriteLine("Sembrando datos de prueba...");

        // =========================================================
        // 2) USERS
        // =========================================================
        var users = new[]
        {
            new User { UserName = "alice", DisplayName = "Alice", PhoneNumber = "555-55-55", CreatedAt = DateTime.UtcNow, IsActive = true },
            new User { UserName = "bob",   DisplayName = "Bob",   Email = "bob@gmail.com", CreatedAt = DateTime.UtcNow, IsActive = true },
            new User { UserName = "carol", DisplayName = "Carol", PhoneNumber = "555-53-52", CreatedAt = DateTime.UtcNow, IsActive = true },
            new User { UserName = "dave",  DisplayName = "Dave",  PhoneNumber = "555-54-51", CreatedAt = DateTime.UtcNow, IsActive = true },
            new User { UserName = "erin",  DisplayName = "Erin",  Email = "erin@yahoo.com", CreatedAt = DateTime.UtcNow, IsActive = true },
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        var alice = users.Single(u => u.UserName == "alice");
        var bob = users.Single(u => u.UserName == "bob");
        var carol = users.Single(u => u.UserName == "carol");
        var dave = users.Single(u => u.UserName == "dave");
        var erin = users.Single(u => u.UserName == "erin");

        // =========================================================
        // 3) IDENTITY KEYS (1–1 por usuario)
        // =========================================================
        var identityKeys = users.Select(u => new IdentityKey
        {
            UserId = u.UserId,
            PublicKey = $"IDENTITY-{u.UserName.ToUpperInvariant()}",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        db.IdentityKeys.AddRange(identityKeys);
        await db.SaveChangesAsync();

        // =========================================================
        // 4) DEVICES (2 por usuario)
        // =========================================================
        var devices = new List<Device>
        {
            new() { UserId = alice.UserId, Platform = "iOS",     PushToken = "alice-ios",     IsTrusted = true, LastSeenAt = DateTime.UtcNow },
            new() { UserId = alice.UserId, Platform = "Android", PushToken = "alice-android", IsTrusted = false, LastSeenAt = DateTime.UtcNow.AddMinutes(-30) },

            new() { UserId = bob.UserId,   Platform = "Android", PushToken = "bob-android",   IsTrusted = true, LastSeenAt = DateTime.UtcNow },
            new() { UserId = bob.UserId,   Platform = "Web",     PushToken = "bob-web",       IsTrusted = true, LastSeenAt = DateTime.UtcNow.AddHours(-1) },

            new() { UserId = carol.UserId, Platform = "iOS",     PushToken = "carol-ios",     IsTrusted = true, LastSeenAt = DateTime.UtcNow },
            new() { UserId = carol.UserId, Platform = "Android", PushToken = "carol-android", IsTrusted = false, LastSeenAt = DateTime.UtcNow.AddHours(-2) },

            new() { UserId = dave.UserId,  Platform = "Android", PushToken = "dave-android",  IsTrusted = true, LastSeenAt = DateTime.UtcNow },
            new() { UserId = erin.UserId,  Platform = "iOS",     PushToken = "erin-ios",      IsTrusted = true, LastSeenAt = DateTime.UtcNow },
        };

        db.Devices.AddRange(devices);
        await db.SaveChangesAsync();

        var alicePhone = devices.First(d => d.UserId == alice.UserId && d.Platform == "iOS");
        var aliceAndroid = devices.First(d => d.UserId == alice.UserId && d.Platform == "Android");
        var bobAndroid = devices.First(d => d.UserId == bob.UserId && d.Platform == "Android");
        var bobWeb = devices.First(d => d.UserId == bob.UserId && d.Platform == "Web");
        var carolPhone = devices.First(d => d.UserId == carol.UserId && d.Platform == "iOS");
        var daveAndroid = devices.First(d => d.UserId == dave.UserId && d.Platform == "Android");
        var erinPhone = devices.First(d => d.UserId == erin.UserId && d.Platform == "iOS");

        // =========================================================
        // 5) DEVICE KEYS (1–1 por device)
        // =========================================================
        var deviceKeys = devices.Select(d => new DeviceKey
        {
            DeviceId = d.DeviceId,
            PublicKey = $"DEVICE-{d.DeviceId}",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        db.DeviceKeys.AddRange(deviceKeys);
        await db.SaveChangesAsync();

        // =========================================================
        // 6) USER SESSIONS
        // =========================================================
        var now = DateTime.UtcNow;

        var sessions = new[]
        {
            new UserSession
            {
                SessionId        = Guid.NewGuid(),
                UserId           = alice.UserId,
                DeviceId         = alicePhone.DeviceId,
                AuthTokenHash    = "alice-auth-1",
                RefreshTokenHash = "alice-refresh-1",
                CreatedAt        = now.AddMinutes(-60),
                ExpiresAt        = now.AddDays(7),
                RevokedAt        = null
            },
            new UserSession
            {
                SessionId        = Guid.NewGuid(),
                UserId           = bob.UserId,
                DeviceId         = bobAndroid.DeviceId,
                AuthTokenHash    = "bob-auth-1",
                RefreshTokenHash = "bob-refresh-1",
                CreatedAt        = now.AddMinutes(-30),
                ExpiresAt        = now.AddDays(7),
                RevokedAt        = null
            },
            new UserSession
            {
                SessionId        = Guid.NewGuid(),
                UserId           = carol.UserId,
                DeviceId         = carolPhone.DeviceId,
                AuthTokenHash    = "carol-auth-1",
                RefreshTokenHash = "carol-refresh-1",
                CreatedAt        = now.AddMinutes(-15),
                ExpiresAt        = now.AddDays(7),
                RevokedAt        = null
            }
        };

        db.UserSessions.AddRange(sessions);
        await db.SaveChangesAsync();

        // =========================================================
        // 7) CHATS (directos y grupos)
        // =========================================================
        var directAliceBob = new Chat
        {
            CreatedByUserId = alice.UserId,
            CreatedAt = now.AddHours(-5),
            Title = "Alice & Bob",
            IsEncrypted = true,
            Type = ChatType.Direct
        };

        var directAliceCarol = new Chat
        {
            CreatedByUserId = alice.UserId,
            CreatedAt = now.AddHours(-4),
            Title = "Alice & Carol",
            IsEncrypted = true,
            Type = ChatType.Direct
        };

        var directBobCarol = new Chat
        {
            CreatedByUserId = bob.UserId,
            CreatedAt = now.AddHours(-3),
            Title = "Bob & Carol",
            IsEncrypted = true,
            Type = ChatType.Direct
        };

        var groupCoreTeam = new Chat
        {
            CreatedByUserId = alice.UserId,
            CreatedAt = now.AddHours(-2),
            Title = "Core Team",
            IsEncrypted = true,
            Type = ChatType.Group
        };

        var groupObservers = new Chat
        {
            CreatedByUserId = dave.UserId,
            CreatedAt = now.AddHours(-1),
            Title = "Observers",
            IsEncrypted = false,
            Type = ChatType.Group
        };

        db.Chats.AddRange(directAliceBob, directAliceCarol, directBobCarol, groupCoreTeam, groupObservers);
        await db.SaveChangesAsync();

        // =========================================================
        // 8) CHAT MEMBERS
        // =========================================================
        var chatMembers = new List<ChatMember>
        {
            // Alice–Bob
            new() { ChatId = directAliceBob.ChatId, UserId = alice.UserId, Role = ChatMemberRole.Owner,  JoinedAt = now.AddHours(-5) },
            new() { ChatId = directAliceBob.ChatId, UserId = bob.UserId,   Role = ChatMemberRole.Member, JoinedAt = now.AddHours(-5) },

            // Alice–Carol
            new() { ChatId = directAliceCarol.ChatId, UserId = alice.UserId, Role = ChatMemberRole.Owner,  JoinedAt = now.AddHours(-4) },
            new() { ChatId = directAliceCarol.ChatId, UserId = carol.UserId, Role = ChatMemberRole.Member, JoinedAt = now.AddHours(-4) },

            // Bob–Carol
            new() { ChatId = directBobCarol.ChatId, UserId = bob.UserId,   Role = ChatMemberRole.Owner,  JoinedAt = now.AddHours(-3) },
            new() { ChatId = directBobCarol.ChatId, UserId = carol.UserId, Role = ChatMemberRole.Member, JoinedAt = now.AddHours(-3) },

            // Grupo Core Team: Alice owner, Bob admin, Carol & Dave members
            new() { ChatId = groupCoreTeam.ChatId, UserId = alice.UserId, Role = ChatMemberRole.Owner,  JoinedAt = now.AddHours(-2) },
            new() { ChatId = groupCoreTeam.ChatId, UserId = bob.UserId,   Role = ChatMemberRole.Admin,  JoinedAt = now.AddHours(-2) },
            new() { ChatId = groupCoreTeam.ChatId, UserId = carol.UserId, Role = ChatMemberRole.Member, JoinedAt = now.AddHours(-2) },
            new() { ChatId = groupCoreTeam.ChatId, UserId = dave.UserId,  Role = ChatMemberRole.Member, JoinedAt = now.AddHours(-2) },

            // Grupo Observers: Dave owner, Erin member
            new() { ChatId = groupObservers.ChatId, UserId = dave.UserId, Role = ChatMemberRole.Owner,  JoinedAt = now.AddHours(-1) },
            new() { ChatId = groupObservers.ChatId, UserId = erin.UserId, Role = ChatMemberRole.Member, JoinedAt = now.AddHours(-1) },
        };

        db.ChatMembers.AddRange(chatMembers);
        await db.SaveChangesAsync();

        // =========================================================
        // 9) MESSAGES + DELIVERIES (varios chats)
        // =========================================================
        var messages = new List<Message>();

        void AddMessage(Chat chat, User sender, string clientId, string text, int payloadSeqMinutes, List<(User user, int deliveredOffsetSec, int? readOffsetSec, long serverSeq)> deliveries)
        {
            var sentAt = now.AddMinutes(payloadSeqMinutes);
            var msg = new Message
            {
                MessageId = Guid.NewGuid(),
                ChatId = chat.ChatId,
                SenderUserId = sender.UserId,
                ClientMessageId = clientId,
                SentAt = sentAt,
                PayloadType = MessagePayloadType.Text,
                CipherText = $"{text} (cifrado)",
                CipherMetadata = "{\"alg\":\"X\"}"
            };
            messages.Add(msg);

            foreach (var (user, deliveredOffset, readOffset, seq) in deliveries)
            {
                db.MessageDeliveries.Add(new MessageDelivery
                {
                    MessageId = msg.MessageId,
                    UserId = user.UserId,
                    DeliveredAt = sentAt.AddSeconds(deliveredOffset),
                    ReadAt = readOffset.HasValue ? sentAt.AddSeconds(readOffset.Value) : null,
                    ServerSequence = seq
                });
            }
        }

        // Chat Alice–Bob
        AddMessage(
            directAliceBob,
            alice,
            "alice-1",
            "Hola Bob",
            -30,
            new()
            {
                (alice, 0, 0, 1),
                (bob,   2, 5, 2)
            });

        AddMessage(
            directAliceBob,
            bob,
            "bob-1",
            "Hola Alice",
            -29,
            new()
            {
                (bob,   0, 0, 3),
                (alice, 3, null, 4)
            });

        // Chat grupo Core Team
        AddMessage(
            groupCoreTeam,
            alice,
            "alice-core-1",
            "Bienvenidos al core team",
            -20,
            new()
            {
                (alice, 0, 0,  5),
                (bob,   1, 3,  6),
                (carol, 2, 10, 7),
                (dave,  3, null, 8)
            });

        AddMessage(
            groupCoreTeam,
            bob,
            "bob-core-1",
            "Tenemos que definir el protocolo",
            -19,
            new()
            {
                (alice, 1, 4,  9),
                (bob,   0, 0, 10),
                (carol, 2, null, 11),
                (dave,  5, null, 12)
            });

        // Chat Observers
        AddMessage(
            groupObservers,
            dave,
            "dave-obs-1",
            "Canal de observadores",
            -10,
            new()
            {
                (dave, 0, 0, 13),
                (erin, 1, 4, 14)
            });

        db.Messages.AddRange(messages);
        await db.SaveChangesAsync();
        await db.SaveChangesAsync(); // entrega y mensajes

        // =========================================================
        // 10) SESSION KEYS (con y sin ChatId)
        // =========================================================
        var sessionKeys = new List<SessionKey>
        {
            // Session key por chat directo Alice–Bob (device móvil de cada uno)
            new()
            {
                SessionKeyId   = Guid.NewGuid(),
                ChatId         = directAliceBob.ChatId,
                LocalDeviceId  = alicePhone.DeviceId,
                RemoteDeviceId = bobAndroid.DeviceId,
                KeyMaterial    = "SK-ALICE-BOB-CHAT",
                CreatedAt      = now.AddMinutes(-40),
                LastUsedAt     = now.AddMinutes(-25)
            },
            new()
            {
                SessionKeyId   = Guid.NewGuid(),
                ChatId         = directAliceBob.ChatId,
                LocalDeviceId  = bobAndroid.DeviceId,
                RemoteDeviceId = alicePhone.DeviceId,
                KeyMaterial    = "SK-BOB-ALICE-CHAT",
                CreatedAt      = now.AddMinutes(-39),
                LastUsedAt     = now.AddMinutes(-26)
            },

            // Session key “global” entre dispositivos (sin ChatId)
            new()
            {
                SessionKeyId   = Guid.NewGuid(),
                ChatId         = null,
                LocalDeviceId  = aliceAndroid.DeviceId,
                RemoteDeviceId = bobWeb.DeviceId,
                KeyMaterial    = "SK-ALICE-BOB-OUTOFCHAT",
                CreatedAt      = now.AddMinutes(-15),
                LastUsedAt     = null
            },
            new()
            {
                SessionKeyId   = Guid.NewGuid(),
                ChatId         = null,
                LocalDeviceId  = carolPhone.DeviceId,
                RemoteDeviceId = daveAndroid.DeviceId,
                KeyMaterial    = "SK-CAROL-DAVE",
                CreatedAt      = now.AddMinutes(-12),
                LastUsedAt     = null
            }
        };

        db.SessionKeys.AddRange(sessionKeys);
        await db.SaveChangesAsync();

        Console.WriteLine("Seed completado correctamente.");
    }

    // ---------------------------------------------------------
    // Limpieza de tablas respetando FKs (MySQL)
    // ---------------------------------------------------------
    private static async Task ClearDatabaseAsync(AppDbContext db)
    {
        // Ejecutamos todo en un solo batch para que se use la misma conexión
        var sql = @"
        SET FOREIGN_KEY_CHECKS = 0;

        DELETE FROM message_deliveries;
        DELETE FROM session_keys;
        DELETE FROM device_keys;
        DELETE FROM identity_keys;
        DELETE FROM user_sessions;
        DELETE FROM chat_members;
        DELETE FROM messages;
        DELETE FROM chats;
        DELETE FROM devices;
        DELETE FROM users;

        SET FOREIGN_KEY_CHECKS = 1;
    ";

        await db.Database.ExecuteSqlRawAsync(sql);
    }

}
