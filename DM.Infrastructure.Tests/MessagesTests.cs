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

    private Chat CreateSampleChat(int createdByUserId, string title = "Chat for messages")
        => new()
        {
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Title = title,
            IsEncrypted = true
        };

    private Message CreateSampleMessage(int chatId, int senderUserId, string clientMessageId = "client-1")
        => new()
        {
            ChatId = chatId,
            SenderUserId = senderUserId,
            ClientMessageId = clientMessageId,
            SentAt = DateTime.UtcNow,
            PayloadType = MessagePayloadType.Text, // ajusta si tu enum tiene otro nombre/valor por defecto
            CipherText = "cipher-text",
            CipherMetadata = "cipher-meta"
        };

    private MessageDelivery CreateSampleDelivery(Guid messageId, int userId, long serverSequence = 1)
        => new()
        {
            MessageId = messageId,
            UserId = userId,
            ServerSequence = serverSequence,
            DeliveredAt = null,
            ReadAt = null
        };

    // 1) Modelo: Message tiene FK hacia Chat
    [Fact]
    public void Model_Has_FK_From_Message_To_Chat()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_FK_From_Message_To_Chat));

        var entity = db.Model.FindEntityType(typeof(Message))!;
        var fks = entity.GetForeignKeys();

        var fkToChat = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(Chat));
        Assert.Equal("ChatId", Assert.Single(fkToChat.Properties).Name);
    }

    // 2) Modelo: Message tiene FK hacia User (SenderUser)
    [Fact]
    public void Model_Has_FK_From_Message_To_SenderUser()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_FK_From_Message_To_SenderUser));

        var entity = db.Model.FindEntityType(typeof(Message))!;
        var fks = entity.GetForeignKeys();

        var fkToUser = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal("SenderUserId", Assert.Single(fkToUser.Properties).Name);
    }

    // 3) Modelo: MessageDelivery tiene PK compuesta (MessageId, UserId) y FKs correctas
    [Fact]
    public void Model_MessageDelivery_Has_Composite_PK_And_FKs()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_MessageDelivery_Has_Composite_PK_And_FKs));

        var entity = db.Model.FindEntityType(typeof(MessageDelivery))!;
        var pk = entity.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(2, pk!.Properties.Count);
        Assert.Contains(pk.Properties, p => p.Name == "MessageId");
        Assert.Contains(pk.Properties, p => p.Name == "UserId");

        var fks = entity.GetForeignKeys().ToList();

        // FK -> Message
        var fkToMessage = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(Message));
        Assert.Equal("MessageId", Assert.Single(fkToMessage.Properties).Name);

        // FK -> User
        var fkToUser = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal("UserId", Assert.Single(fkToUser.Properties).Name);
    }

    // 4) SQLite: se puede crear un mensaje para un chat y usuario existentes
    [Fact]
    public async Task Sqlite_Allows_Message_With_Existing_Chat_And_Sender()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var sender = CreateSampleUser("sender");
            db.Users.Add(sender);
            await db.SaveChangesAsync();

            var chat = CreateSampleChat(sender.UserId, "Chat messages");
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            var msg = CreateSampleMessage(chat.ChatId, sender.UserId, "client-42");
            db.Messages.Add(msg);
            await db.SaveChangesAsync();

            var loaded = await db.Messages
                .Include(m => m.Chat)
                .Include(m => m.SenderUser)
                .SingleAsync(m => m.MessageId == msg.MessageId);

            Assert.NotNull(loaded.Chat);
            Assert.NotNull(loaded.SenderUser);
            Assert.Equal("Chat messages", loaded.Chat!.Title);
            Assert.Equal("sender", loaded.SenderUser!.UserName);
        }
    }

    // 5) SQLite: no permite Message con Chat inexistente
    [Fact]
    public async Task Sqlite_Fails_When_Message_Has_NonExisting_Chat()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var sender = CreateSampleUser("sender_invalid_chat");
            db.Users.Add(sender);
            await db.SaveChangesAsync();

            var msg = CreateSampleMessage(chatId: 999, senderUserId: sender.UserId);

            db.Messages.Add(msg);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 6) SQLite: no permite Message con SenderUser inexistente
    [Fact]
    public async Task Sqlite_Fails_When_Message_Has_NonExisting_Sender()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var owner = CreateSampleUser("owner_for_chat");
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var chat = CreateSampleChat(owner.UserId, "Chat invalid sender");
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            var msg = CreateSampleMessage(chat.ChatId, senderUserId: 999);

            db.Messages.Add(msg);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 7) SQLite: MessageDelivery requiere Message y User válidos
    [Fact]
    public async Task Sqlite_Allows_MessageDelivery_With_Existing_Message_And_User()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var sender = CreateSampleUser("sender_md");
            var recipient = CreateSampleUser("recipient_md");
            db.Users.AddRange(sender, recipient);
            await db.SaveChangesAsync();

            var chat = CreateSampleChat(sender.UserId, "Chat delivery");
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            var msg = CreateSampleMessage(chat.ChatId, sender.UserId);
            db.Messages.Add(msg);
            await db.SaveChangesAsync();

            var delivery = CreateSampleDelivery(msg.MessageId, recipient.UserId);
            db.MessageDeliveries.Add(delivery);
            await db.SaveChangesAsync();

            var loaded = await db.MessageDeliveries
                .Include(md => md.Message)
                .Include(md => md.User)
                .SingleAsync(md => md.MessageId == msg.MessageId && md.UserId == recipient.UserId);

            Assert.NotNull(loaded.Message);
            Assert.NotNull(loaded.User);
        }
    }

    // 8) Lógica: marcar mensaje como entregado y leído
    [Fact]
    public async Task Can_Mark_Delivery_As_Delivered_And_Read()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_Mark_Delivery_As_Delivered_And_Read));

        var sender = CreateSampleUser("sender_logic");
        var recipient = CreateSampleUser("recipient_logic");
        db.Users.AddRange(sender, recipient);
        await db.SaveChangesAsync();

        var chat = CreateSampleChat(sender.UserId, "Chat logic");
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        var msg = CreateSampleMessage(chat.ChatId, sender.UserId);
        db.Messages.Add(msg);
        await db.SaveChangesAsync();

        var delivery = CreateSampleDelivery(msg.MessageId, recipient.UserId);
        db.MessageDeliveries.Add(delivery);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        // marcar como entregado
        delivery.DeliveredAt = now;
        await db.SaveChangesAsync();

        // marcar como leído
        delivery.ReadAt = now.AddMinutes(1);
        await db.SaveChangesAsync();

        var loaded = await db.MessageDeliveries
            .SingleAsync(md => md.MessageId == msg.MessageId && md.UserId == recipient.UserId);

        Assert.NotNull(loaded.DeliveredAt);
        Assert.NotNull(loaded.ReadAt);
        Assert.True(loaded.ReadAt > loaded.DeliveredAt);
    }

    // 9) Lógica: contar mensajes no leídos para un usuario
    [Fact]
    public async Task Can_Count_Unread_Messages_For_User()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_Count_Unread_Messages_For_User));

        var sender = CreateSampleUser("sender_unread");
        var recipient = CreateSampleUser("recipient_unread");
        db.Users.AddRange(sender, recipient);
        await db.SaveChangesAsync();

        var chat = CreateSampleChat(sender.UserId, "Chat unread");
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        var msg1 = CreateSampleMessage(chat.ChatId, sender.UserId, "m1");
        var msg2 = CreateSampleMessage(chat.ChatId, sender.UserId, "m2");
        var msg3 = CreateSampleMessage(chat.ChatId, sender.UserId, "m3");
        db.Messages.AddRange(msg1, msg2, msg3);
        await db.SaveChangesAsync();

        var d1 = CreateSampleDelivery(msg1.MessageId, recipient.UserId);
        var d2 = CreateSampleDelivery(msg2.MessageId, recipient.UserId);
        var d3 = CreateSampleDelivery(msg3.MessageId, recipient.UserId);

        d1.ReadAt = DateTime.UtcNow;

        db.MessageDeliveries.AddRange(d1, d2, d3);
        await db.SaveChangesAsync();

        var unreadCount = await db.MessageDeliveries
            .Where(md => md.UserId == recipient.UserId && md.ReadAt == null)
            .CountAsync();

        Assert.Equal(2, unreadCount);
    }

    [Fact]
    public void Model_Has_Unique_Index_On_ChatId_And_ClientMessageId()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_Unique_Index_On_ChatId_And_ClientMessageId));

        var entity = db.Model.FindEntityType(typeof(Message))!;

        var indexes = entity.GetIndexes();

        var index = Assert.Single(indexes, i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { "ChatId", "ClientMessageId" })
        );

        Assert.True(index.IsUnique);
    }

}
