using System;
using System.Linq;
using System.Threading.Tasks;
using DM.Domain.Entities;
using DM.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DM.Infrastructure.Tests;

public class MessagesTests
{
    private User CreateSampleUser(string userName)
        => new()
        {
            UserName = userName,
            DisplayName = $"Display {userName}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

    private Chat CreateSampleChat(int createdByUserId, ChatType type = ChatType.Direct, string? title = null)
        => new()
        {
            Type = type,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Title = title,
            IsEncrypted = true
        };

    private Message CreateSampleMessage(int chatId, int senderUserId, string text)
        => new()
        {
            MessageId = Guid.NewGuid(),
            ChatId = chatId,
            SenderUserId = senderUserId,
            ClientMessageId = Guid.NewGuid().ToString(),
            SentAt = DateTime.UtcNow,
            PayloadType = MessagePayloadType.Text,   // Ajusta si tu enum tiene otro nombre/valor
            CipherText = text,
            CipherMetadata = "meta"
        };

    private MessageDelivery CreateSampleDelivery(Guid messageId, int userId, long serverSeq,
                                                 bool delivered = true, bool read = false)
        => new()
        {
            MessageId = messageId,
            UserId = userId,
            DeliveredAt = delivered ? DateTime.UtcNow : null,
            ReadAt = read ? DateTime.UtcNow : null,
            ServerSequence = serverSeq
        };

    // 1) Modelo: MessageDelivery tiene PK compuesta y FKs a Message y User
    [Fact]
    public void Model_MessageDelivery_Has_Composite_PK_And_FKs()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_MessageDelivery_Has_Composite_PK_And_FKs));

        var entity = db.Model.FindEntityType(typeof(MessageDelivery))!;
        var pk = entity.FindPrimaryKey()!;
        var fkToMessage = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Message));
        var fkToUser = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        // PK compuesta: MessageId + UserId
        Assert.Equal(2, pk.Properties.Count);
        Assert.Contains(pk.Properties, p => p.Name == "MessageId");
        Assert.Contains(pk.Properties, p => p.Name == "UserId");

        // FK hacia Message
        Assert.Equal("MessageId", Assert.Single(fkToMessage.Properties).Name);

        // FK hacia User
        Assert.Equal("UserId", Assert.Single(fkToUser.Properties).Name);
    }

    // 2) SQLite: se puede crear mensaje y entregas para usuarios válidos
    [Fact]
    public async Task Sqlite_Creates_Message_With_Deliveries()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            // Usuarios
            var alice = CreateSampleUser("alice_msg");
            var bob   = CreateSampleUser("bob_msg");
            db.Users.AddRange(alice, bob);
            await db.SaveChangesAsync();

            // Chat
            var chat = CreateSampleChat(alice.UserId);
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            // Mensaje de Alice
            var msg = CreateSampleMessage(chat.ChatId, alice.UserId, "Hola Bob");
            db.Messages.Add(msg);
            await db.SaveChangesAsync();

            // Deliveries: para Alice (sender) y Bob (receptor)
            var d1 = CreateSampleDelivery(msg.MessageId, alice.UserId, serverSeq: 1, delivered: true, read: true);
            var d2 = CreateSampleDelivery(msg.MessageId, bob.UserId, serverSeq: 2, delivered: true, read: false);

            db.MessageDeliveries.AddRange(d1, d2);
            await db.SaveChangesAsync();

            var loaded = await db.Messages
                .Include(m => m.Deliveries)
                .ThenInclude(d => d.User)
                .SingleAsync(m => m.MessageId == msg.MessageId);

            Assert.Equal(2, loaded.Deliveries.Count);
            Assert.Contains(loaded.Deliveries, d => d.User!.UserName == "alice_msg" && d.ReadAt != null);
            Assert.Contains(loaded.Deliveries, d => d.User!.UserName == "bob_msg" && d.ReadAt == null);
        }
    }

    // 3) SQLite: no permite MessageDelivery sin Message
    [Fact]
    public async Task Sqlite_Fails_When_Delivery_Message_Does_Not_Exist()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_no_message");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // MessageId inexistente
            var delivery = CreateSampleDelivery(Guid.NewGuid(), user.UserId, serverSeq: 1);

            db.MessageDeliveries.Add(delivery);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 4) SQLite: no permite MessageDelivery sin User
    [Fact]
    public async Task Sqlite_Fails_When_Delivery_User_Does_Not_Exist()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var alice = CreateSampleUser("alice_only");
            db.Users.Add(alice);
            await db.SaveChangesAsync();

            // Chat y mensaje correctos
            var chat = CreateSampleChat(alice.UserId);
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            var msg = CreateSampleMessage(chat.ChatId, alice.UserId, "Mensaje sin user destinatario");
            db.Messages.Add(msg);
            await db.SaveChangesAsync();

            // Delivery para UserId inexistente
            var delivery = CreateSampleDelivery(msg.MessageId, userId: 999, serverSeq: 1);

            db.MessageDeliveries.Add(delivery);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 5) Lógica: mensajes ordenados por ServerSequence para un usuario
    [Fact]
    public async Task Can_Order_Deliveries_By_ServerSequence_For_User()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_Order_Deliveries_By_ServerSequence_For_User));

        var alice = CreateSampleUser("alice_seq");
        var bob   = CreateSampleUser("bob_seq");
        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        var chat = CreateSampleChat(alice.UserId);
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        var msg1 = CreateSampleMessage(chat.ChatId, alice.UserId, "msg1");
        var msg2 = CreateSampleMessage(chat.ChatId, alice.UserId, "msg2");
        var msg3 = CreateSampleMessage(chat.ChatId, alice.UserId, "msg3");

        db.Messages.AddRange(msg1, msg2, msg3);
        await db.SaveChangesAsync();

        // Entregas solo para Bob, con ServerSequence desordenado
        var d1 = CreateSampleDelivery(msg1.MessageId, bob.UserId, serverSeq: 20);
        var d2 = CreateSampleDelivery(msg2.MessageId, bob.UserId, serverSeq: 10);
        var d3 = CreateSampleDelivery(msg3.MessageId, bob.UserId, serverSeq: 30);

        db.MessageDeliveries.AddRange(d1, d2, d3);
        await db.SaveChangesAsync();

        var ordered = await db.MessageDeliveries
            .Where(d => d.UserId == bob.UserId)
            .OrderBy(d => d.ServerSequence)
            .Select(d => d.MessageId)
            .ToListAsync();

        // Esperamos el orden por secuencia: msg2 (10), msg1 (20), msg3 (30)
        Assert.Equal(3, ordered.Count);
        Assert.Equal(msg2.MessageId, ordered[0]);
        Assert.Equal(msg1.MessageId, ordered[1]);
        Assert.Equal(msg3.MessageId, ordered[2]);
    }
}
