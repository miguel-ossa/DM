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

    private Chat CreateSampleChat(int createdByUserId, string title = "Sample chat")
        => new()
        {
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            Title = title,
            IsEncrypted = false
            // Type se quedará con el valor por defecto del enum
        };

    private ChatMember CreateSampleChatMember(int chatId, int userId)
        => new()
        {
            ChatId = chatId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
            // Role y MutedUntil pueden quedarse con valores por defecto
        };

    // 1) Modelo: ChatMember tiene FK hacia Chat
    [Fact]
    public void Model_Has_FK_From_ChatMember_To_Chat()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_FK_From_ChatMember_To_Chat));

        var entity = db.Model.FindEntityType(typeof(ChatMember))!;
        var fks = entity.GetForeignKeys();

        var fkToChat = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(Chat));
        Assert.Equal("ChatId", Assert.Single(fkToChat.Properties).Name);
    }

    // 2) Modelo: ChatMember tiene FK hacia User
    [Fact]
    public void Model_Has_FK_From_ChatMember_To_User()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_FK_From_ChatMember_To_User));

        var entity = db.Model.FindEntityType(typeof(ChatMember))!;
        var fks = entity.GetForeignKeys();

        var fkToUser = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal("UserId", Assert.Single(fkToUser.Properties).Name);
    }

    // 3) SQLite: Crear un chat de grupo con varios miembros válidos
    [Fact]
    public async Task Sqlite_Allows_GroupChat_With_Valid_Members()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            // Arrange: 3 usuarios
            var owner = CreateSampleUser("owner");
            var member1 = CreateSampleUser("member1");
            var member2 = CreateSampleUser("member2");

            db.Users.AddRange(owner, member1, member2);
            await db.SaveChangesAsync();

            // Chat creado por "owner"
            var chat = CreateSampleChat(owner.UserId, "Group chat");
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            // Miembros
            var cmOwner = CreateSampleChatMember(chat.ChatId, owner.UserId);
            var cm1 = CreateSampleChatMember(chat.ChatId, member1.UserId);
            var cm2 = CreateSampleChatMember(chat.ChatId, member2.UserId);

            db.ChatMembers.AddRange(cmOwner, cm1, cm2);
            await db.SaveChangesAsync();

            // Assert: se recupera el chat con sus miembros
            var loaded = await db.Chats
                .Include(c => c.Members)
                .SingleAsync(c => c.ChatId == chat.ChatId);

            Assert.Equal(3, loaded.Members.Count);
            Assert.Contains(loaded.Members, m => m.UserId == owner.UserId);
            Assert.Contains(loaded.Members, m => m.UserId == member1.UserId);
            Assert.Contains(loaded.Members, m => m.UserId == member2.UserId);
        }
    }

    // 4) SQLite: no permite ChatMember con Chat inexistente
    [Fact]
    public async Task Sqlite_Fails_When_ChatMember_Has_NonExisting_Chat()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_for_invalid_chat");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var cm = CreateSampleChatMember(chatId: 999, userId: user.UserId);

            db.ChatMembers.Add(cm);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 5) SQLite: no permite ChatMember con User inexistente
    [Fact]
    public async Task Sqlite_Fails_When_ChatMember_Has_NonExisting_User()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var owner = CreateSampleUser("owner_for_invalid_user");
            db.Users.Add(owner);
            await db.SaveChangesAsync();

            var chat = CreateSampleChat(owner.UserId, "Chat invalid user");
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            var cm = CreateSampleChatMember(chat.ChatId, userId: 999);

            db.ChatMembers.Add(cm);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 6) Lógica de dominio: chat "directo" con exactamente 2 miembros
    // (no se puede reforzar vía FK, pero sí comprobar el patrón de uso)
    [Fact]
    public async Task DirectChat_Has_Exactly_Two_Members()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(DirectChat_Has_Exactly_Two_Members));

        var user1 = CreateSampleUser("direct1");
        var user2 = CreateSampleUser("direct2");

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var chat = CreateSampleChat(user1.UserId, "Direct chat (logical)");
        db.Chats.Add(chat);
        await db.SaveChangesAsync();

        var cm1 = CreateSampleChatMember(chat.ChatId, user1.UserId);
        var cm2 = CreateSampleChatMember(chat.ChatId, user2.UserId);

        db.ChatMembers.AddRange(cm1, cm2);
        await db.SaveChangesAsync();

        var loaded = await db.Chats
            .Include(c => c.Members)
            .SingleAsync(c => c.ChatId == chat.ChatId);

        Assert.Equal(2, loaded.Members.Count);
        Assert.All(loaded.Members, m => Assert.True(m.UserId == user1.UserId || m.UserId == user2.UserId));
    }
}
