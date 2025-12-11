using DM.Domain.Entities;
using DM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DM.Infrastructure.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Evitar duplicar seeds si ya existen usuarios
        if (await db.Users.AnyAsync())
        {
            Console.WriteLine("Ya existen datos en la BD. Seed ignorado.");
            return;
        }

        Console.WriteLine("Sembrando datos de prueba...");

        // 1) USERS
        var alice = new User
        {
            UserName = "alice",
            DisplayName = "Alice",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var bob = new User
        {
            UserName = "bob",
            DisplayName = "Bob",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var carol = new User
        {
            UserName = "carol",
            DisplayName = "Carol",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();

        // 2) DEVICES
        var aliceDevice = new Device
        {
            UserId = alice.UserId,
            Platform = "iOS",
            PushToken = "alice-token",
            IsTrusted = true,
            LastSeenAt = DateTime.UtcNow
        };

        var bobDevice = new Device
        {
            UserId = bob.UserId,
            Platform = "Android",
            PushToken = "bob-token",
            IsTrusted = true,
            LastSeenAt = DateTime.UtcNow
        };

        var carolDevice = new Device
        {
            UserId = carol.UserId,
            Platform = "Android",
            PushToken = "carol-token",
            IsTrusted = true,
            LastSeenAt = DateTime.UtcNow
        };

        db.Devices.AddRange(aliceDevice, bobDevice, carolDevice);
        await db.SaveChangesAsync();

        // 3) CHATS
        // Chat directo Alice <-> Bob
        var directChat = new Chat
        {
            CreatedByUserId = alice.UserId,
            CreatedAt = DateTime.UtcNow,
            Title = "Alice & Bob",
            IsEncrypted = true,
            Type = ChatType.Direct   // ajusta si tu enum tiene otro nombre
        };

        // Chat de grupo Alice + Bob + Carol
        var groupChat = new Chat
        {
            CreatedByUserId = alice.UserId,
            CreatedAt = DateTime.UtcNow,
            Title = "Grupo demo",
            IsEncrypted = true,
            Type = ChatType.Group    // idem
        };

        db.Chats.AddRange(directChat, groupChat);
        await db.SaveChangesAsync();

        // 4) CHAT MEMBERS
        var directMembers = new[]
        {
            new ChatMember
            {
                ChatId = directChat.ChatId,
                UserId = alice.UserId,
                Role = ChatMemberRole.Owner,  // ajusta enum
                JoinedAt = DateTime.UtcNow
            },
            new ChatMember
            {
                ChatId = directChat.ChatId,
                UserId = bob.UserId,
                Role = ChatMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        };

        var groupMembers = new[]
        {
            new ChatMember
            {
                ChatId = groupChat.ChatId,
                UserId = alice.UserId,
                Role = ChatMemberRole.Owner,
                JoinedAt = DateTime.UtcNow
            },
            new ChatMember
            {
                ChatId = groupChat.ChatId,
                UserId = bob.UserId,
                Role = ChatMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            },
            new ChatMember
            {
                ChatId = groupChat.ChatId,
                UserId = carol.UserId,
                Role = ChatMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            }
        };

        db.ChatMembers.AddRange(directMembers);
        db.ChatMembers.AddRange(groupMembers);
        await db.SaveChangesAsync();

        // 5) MESSAGES (en el chat directo)
        var m1 = new Message
        {
            MessageId = Guid.NewGuid(),
            ChatId = directChat.ChatId,
            SenderUserId = alice.UserId,
            ClientMessageId = "alice-1",
            SentAt = DateTime.UtcNow.AddMinutes(-10),
            PayloadType = MessagePayloadType.Text,
            CipherText = "Hola Bob (cifrado)",
            CipherMetadata = "{\"alg\":\"X\"}"
        };

        var m2 = new Message
        {
            MessageId = Guid.NewGuid(),
            ChatId = directChat.ChatId,
            SenderUserId = bob.UserId,
            ClientMessageId = "bob-1",
            SentAt = DateTime.UtcNow.AddMinutes(-9),
            PayloadType = MessagePayloadType.Text,
            CipherText = "Hola Alice (cifrado)",
            CipherMetadata = "{\"alg\":\"X\"}"
        };

        db.Messages.AddRange(m1, m2);
        await db.SaveChangesAsync();

        // 6) MESSAGE DELIVERIES (para que haya “entregado” y “leído”)
        var d1ForAlice = new MessageDelivery
        {
            MessageId = m1.MessageId,
            UserId = alice.UserId,
            DeliveredAt = m1.SentAt,
            ReadAt = m1.SentAt,
            ServerSequence = 1
        };

        var d1ForBob = new MessageDelivery
        {
            MessageId = m1.MessageId,
            UserId = bob.UserId,
            DeliveredAt = m1.SentAt.AddSeconds(2),
            ReadAt = m1.SentAt.AddSeconds(5),
            ServerSequence = 2
        };

        var d2ForBob = new MessageDelivery
        {
            MessageId = m2.MessageId,
            UserId = bob.UserId,
            DeliveredAt = m2.SentAt,
            ReadAt = m2.SentAt,
            ServerSequence = 3
        };

        var d2ForAlice = new MessageDelivery
        {
            MessageId = m2.MessageId,
            UserId = alice.UserId,
            DeliveredAt = m2.SentAt.AddSeconds(2),
            ReadAt = null, // aún no leído
            ServerSequence = 4
        };

        db.MessageDeliveries.AddRange(d1ForAlice, d1ForBob, d2ForBob, d2ForAlice);
        await db.SaveChangesAsync();

        Console.WriteLine("Seed completado correctamente.");
    }
}
