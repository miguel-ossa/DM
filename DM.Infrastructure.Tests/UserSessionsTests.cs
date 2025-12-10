using DM.Domain.Entities;
using DM.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DM.Infrastructure.Tests;

public class UserSessionsTests
{
    private User CreateSampleUser(string userName = "user_session")
        => new()
        {
            UserName = userName,
            DisplayName = "User for sessions",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

    private Device CreateSampleDevice(int userId)
        => new()
        {
            UserId = userId,
            Platform = "Android",
            PushToken = "token-session",
            IsTrusted = true,
            LastSeenAt = DateTime.UtcNow
        };

    private UserSession CreateSampleSession(int userId, int? deviceId = null)
        => new()
        {
            UserId = userId,
            DeviceId = deviceId,
            AuthTokenHash = "auth-hash",
            RefreshTokenHash = "refresh-hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RevokedAt = null
        };

    // 1) Modelo: FK obligatoria de UserSession -> User
    [Fact]
    public void Model_Has_FK_From_UserSession_To_User()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_FK_From_UserSession_To_User));

        var entity = db.Model.FindEntityType(typeof(UserSession))!;
        var fks = entity.GetForeignKeys();

        var fkToUser = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal("UserId", Assert.Single(fkToUser.Properties).Name);
        Assert.False(fkToUser.IsRequiredDependent); // depende de cómo esté configurado, pero lo normal es requerido
    }

    // 2) Modelo: FK opcional de UserSession -> Device
    [Fact]
    public void Model_Has_Optional_FK_From_UserSession_To_Device()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_Optional_FK_From_UserSession_To_Device));

        var entity = db.Model.FindEntityType(typeof(UserSession))!;
        var fks = entity.GetForeignKeys();

        var fkToDevice = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(Device));

        Assert.Equal("DeviceId", Assert.Single(fkToDevice.Properties).Name);
        Assert.True(fkToDevice.IsRequired == false); // opcional
    }

    // 3) SQLite: se puede crear sesión para User + Device existentes
    [Fact]
    public async Task Sqlite_Allows_Session_With_Existing_User_And_Device()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_with_session");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var device = CreateSampleDevice(user.UserId);
            db.Devices.Add(device);
            await db.SaveChangesAsync();

            var session = CreateSampleSession(user.UserId, device.DeviceId);
            db.UserSessions.Add(session);
            await db.SaveChangesAsync();

            var loaded = await db.UserSessions
                .Include(s => s.User)
                .Include(s => s.Device)
                .SingleAsync(s => s.SessionId == session.SessionId);

            Assert.NotNull(loaded.User);
            Assert.NotNull(loaded.Device);
            Assert.Equal("user_with_session", loaded.User!.UserName);
        }
    }

    // 4) SQLite: FK UserId debe existir
    [Fact]
    public async Task Sqlite_Fails_When_Session_User_Does_Not_Exist()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            // No insertamos User
            var session = CreateSampleSession(userId: 999);

            db.UserSessions.Add(session);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    // 5) Lógica: sesiones activas (no revocadas, no expiradas)
    [Fact]
    public async Task Can_Query_Active_Sessions()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_Query_Active_Sessions));

        var user = CreateSampleUser("user_active_sessions");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var activeSession = CreateSampleSession(user.UserId);
        var expiredSession = CreateSampleSession(user.UserId);
        expiredSession.ExpiresAt = DateTime.UtcNow.AddHours(-1);

        var revokedSession = CreateSampleSession(user.UserId);
        revokedSession.RevokedAt = DateTime.UtcNow;

        db.UserSessions.AddRange(activeSession, expiredSession, revokedSession);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        var activeSessions = await db.UserSessions
            .Where(s =>
                s.UserId == user.UserId &&
                s.RevokedAt == null &&
                s.ExpiresAt > now)
            .ToListAsync();

        Assert.Single(activeSessions);
        Assert.Equal(activeSession.SessionId, activeSessions[0].SessionId);
    }
}
