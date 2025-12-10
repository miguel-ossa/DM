using System;
using System.Linq;
using System.Threading.Tasks;
using DM.Domain.Entities;
using DM.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DM.Infrastructure.Tests;

public class ChatsTests
{
    private User CreateSampleUser(string userName)
        => new()
        {
            UserName = userName,
            DisplayName = $"Display {userName}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

    private Chat CreateDirectChat(int createdByUserId)
        => new()
        {
            Type = ChatType.Direct,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Title = null,
            IsEncrypted = true
        };

    private Chat CreateGroupChat(int createdByUserId, string title)
        => new()
        {
            Type = ChatType.Group,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Title = title,
            IsEncrypted = true
        };

    private ChatMember CreateChatMember(int chatId, int userId, ChatMemberRole role)
        => new()
        {
            ChatId = chatId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            MutedUntil = null
        };

    // 1) Modelo: ChatMember tiene PK compuesta y FKs a Chat y User
    [Fact]
    public void Model_ChatMember_Has_Composite_PK_And_FKs()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_ChatMember_Has_Composite_PK_And_FKs));

        var entity = db.Model.FindEntityType(typeof(ChatMember))!;
        var pk = entity.FindPrimaryKey()!;
        var fkToChat = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Chat));
        var fkToUser = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        // PK compuesta: ChatId + UserId
        Assert.Equal(2, pk.Properties.Count);
        Assert.Contains(pk.Properties, p => p.Name == "ChatId");
        Assert.Contains(pk.Properties, p => p.Name == "UserId");

        // FK hacia Chat
        Assert.Equal("ChatId", Assert.Single(fkToChat.Properties).Name);

        // FK hacia User
        Assert.Equal("UserId", Assert.Single(fkToUser.Properties).Name);
    }

    // 2) SQLite: se pueden crear Chat y ChatMember con User existentes
    [Fact]
    public async Task Sqlite_Allows_Adding_Members_To_Existing_Chat_And_Users()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            // Usuarios
            var alice = CreateSampleUser("alice");
            var bob = CreateSampleUser("bob");
            db.Users.AddRange(alice, bob);
            await db.SaveChangesAsync();

            // Chat directo creado por Alice
            var chat = CreateDirectChat(alice.UserId);
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            // Miembros: Alice (Owner), Bob (Member)
            var m1 = CreateChatMember(chat.ChatId, alice.UserId, ChatMemberRole.Owner);
            var m2 = CreateChatMember(chat.ChatId, bob.UserId, ChatMemberRole.Member);

            db.ChatMembers.AddRange(m1, m2);
            await db.SaveChangesAsync();

            var loadedChat = await db.Chats
                .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
                .SingleAsync(c => c.ChatId == chat.ChatId);

            Assert.Equal(2, loadedChat.Members.Count);
            Assert.Contains(loadedChat.Members, cm => cm.User!.UserName == "alice" && cm.Role == ChatMemberRole.Owner);
            Assert.Contains(loadedChat.Members, cm => cm.User!.UserName == "bob" && cm.Role == ChatMemberRole.Member);
        }
    }

    // 3) SQLite: no permite ChatMember sin Chat
    [Fact]
    public async Task Sqlite_Fails_When_ChatMember_Chat_Does_Not_Exist()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_no_chat");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // ChatId inexistente
            var member = CreateChatMember(chatId: 999, userId: user.UserId, role: ChatMemberRole.Member);
            db.ChatMembers.Add(member);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 4) SQLite: no permite ChatMember sin User
    [Fact]
    public async Task Sqlite_Fails_When_ChatMember_User_Does_Not_Exist()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var creator = CreateSampleUser("creator");
            db.Users.Add(creator);
            await db.SaveChangesAsync();

            var chat = CreateGroupChat(creator.UserId, "Mi grupo");
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            // UserId inexistente
            var member = CreateChatMember(chat.ChatId, userId: 999, role: ChatMemberRole.Member);
            db.ChatMembers.Add(member);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 5) Lógica: un chat directo tiene exactamente 2 miembros (test de dominio)
    [Fact]
    public async Task Direct_Chat_Should_Have_Exactly_Two_Members()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Direct_Chat_Should_Have_Exactly_Two_Members));

        var alice = CreateSampleUser("alice_direct");
        var bob = CreateSampleUser("bob_direct");

        db.Users.AddRange(alice, bob);
        await db.SaveChangesAsync();

        var chat = CreateDirectChat(alice.UserId);
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        var m1 = CreateChatMember(chat.ChatId, alice.UserId, ChatMemberRole.Owner);
        var m2 = CreateChatMember(chat.ChatId, bob.UserId, ChatMemberRole.Member);

        db.ChatMembers.AddRange(m1, m2);
        await db.SaveChangesAsync();

        var memberCount = await db.ChatMembers.CountAsync(cm => cm.ChatId == chat.ChatId);

        Assert.Equal(2, memberCount);
    }

    [Fact]
    public async Task Sqlite_Loads_Chat_With_Members_And_Messages()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var alice = CreateSampleUser("alice_chat");
            var bob = CreateSampleUser("bob_chat");

            db.Users.AddRange(alice, bob);
            await db.SaveChangesAsync();

            // Chat de grupo creado por Alice
            var chat = CreateGroupChat(alice.UserId, "Grupo de prueba");
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            // Miembros
            var m1 = CreateChatMember(chat.ChatId, alice.UserId, ChatMemberRole.Owner);
            var m2 = CreateChatMember(chat.ChatId, bob.UserId, ChatMemberRole.Member);
            db.ChatMembers.AddRange(m1, m2);

            // Mensajes
            var msg1 = new Message
            {
                ChatId = chat.ChatId,
                SenderUserId = alice.UserId,
                ClientMessageId = Guid.NewGuid().ToString(),
                SentAt = DateTime.UtcNow,
                PayloadType = MessagePayloadType.Text,
                CipherText = "cipher1",
                CipherMetadata = "meta1"
            };

            var msg2 = new Message
            {
                ChatId = chat.ChatId,
                SenderUserId = bob.UserId,
                ClientMessageId = Guid.NewGuid().ToString(),
                SentAt = DateTime.UtcNow,
                PayloadType = MessagePayloadType.Text,
                CipherText = "cipher2",
                CipherMetadata = "meta2"
            };

            db.Messages.AddRange(msg1, msg2);

            await db.SaveChangesAsync();

            // Cargar todo el gráfico: Chat + Members + Messages
            var loadedChat = await db.Chats
                .Include(c => c.Members)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.Messages)
                .SingleAsync(c => c.ChatId == chat.ChatId);

            Assert.Equal(2, loadedChat.Members.Count);
            Assert.Equal(2, loadedChat.Messages.Count);

            Assert.Contains(loadedChat.Members, cm => cm.User!.UserName == "alice_chat");
            Assert.Contains(loadedChat.Members, cm => cm.User!.UserName == "bob_chat");

            Assert.Contains(loadedChat.Messages, m => m.CipherText == "cipher1");
            Assert.Contains(loadedChat.Messages, m => m.CipherText == "cipher2");
        }
    }

}
