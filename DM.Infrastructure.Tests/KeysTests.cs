using System;
using System.Linq;
using System.Threading.Tasks;
using DM.Domain.Entities;
using DM.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DM.Infrastructure.Tests;

public class KeysTests
{
    // Helpers básicos
    private User CreateSampleUser(string userName)
        => new()
        {
            UserName = userName,
            DisplayName = $"Display {userName}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

    private Device CreateSampleDevice(int userId, string platform = "Android")
        => new()
        {
            UserId = userId,
            Platform = platform,
            PushToken = $"{platform.ToLower()}-token",
            IsTrusted = true,
            LastSeenAt = DateTime.UtcNow
        };

    // ===========================================================
    // IDENTITY KEY
    // ===========================================================

    [Fact]
    public void Model_IdentityKey_Has_PK_And_OneToOne_With_User()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_IdentityKey_Has_PK_And_OneToOne_With_User));

        var entity = db.Model.FindEntityType(typeof(IdentityKey))!;

        // PK = UserId
        var pk = entity.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk!.Properties);
        Assert.Equal("UserId", pk.Properties[0].Name);

        // Una única FK hacia User
        var fk = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(typeof(User), fk.PrincipalEntityType.ClrType);
        Assert.Equal("UserId", Assert.Single(fk.Properties).Name);
    }

    [Fact]
    public async Task Sqlite_Allows_IdentityKey_For_Existing_User()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_identity");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var identityKey = new IdentityKey
            {
                UserId = user.UserId,
                PublicKey = "IDENTITY_PUBLIC_KEY",
                CreatedAt = DateTime.UtcNow
            };

            db.IdentityKeys.Add(identityKey);
            await db.SaveChangesAsync();

            var loaded = await db.Users
                .Include(u => u.IdentityKey)
                .SingleAsync(u => u.UserId == user.UserId);

            Assert.NotNull(loaded.IdentityKey);
            Assert.Equal("IDENTITY_PUBLIC_KEY", loaded.IdentityKey!.PublicKey);
        }
    }

    [Fact]
    public async Task Sqlite_Fails_When_IdentityKey_Uses_NonExisting_User()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            // No creamos User
            var identityKey = new IdentityKey
            {
                UserId = 999,
                PublicKey = "KEY_NO_USER",
                CreatedAt = DateTime.UtcNow
            };

            db.IdentityKeys.Add(identityKey);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Sqlite_Fails_When_Adding_Two_IdentityKeys_For_Same_User()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_two_identity_keys");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var key1 = new IdentityKey
            {
                UserId = user.UserId,
                PublicKey = "KEY1",
                CreatedAt = DateTime.UtcNow
            };

            var key2 = new IdentityKey
            {
                UserId = user.UserId,
                PublicKey = "KEY2",
                CreatedAt = DateTime.UtcNow
            };

            db.IdentityKeys.Add(key1);
            await db.SaveChangesAsync();

            // ❗ Aquí es donde falla, no en SaveChanges
            Assert.Throws<InvalidOperationException>(() =>
            {
                db.IdentityKeys.Add(key2);
            });
        }
    }

    // ===========================================================
    // DEVICE KEY
    // ===========================================================

    [Fact]
    public void Model_DeviceKey_Has_PK_And_OneToOne_With_Device()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_DeviceKey_Has_PK_And_OneToOne_With_Device));

        var entity = db.Model.FindEntityType(typeof(DeviceKey))!;

        var pk = entity.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk!.Properties);
        Assert.Equal("DeviceId", pk.Properties[0].Name);

        var fk = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(typeof(Device), fk.PrincipalEntityType.ClrType);
        Assert.Equal("DeviceId", Assert.Single(fk.Properties).Name);
    }

    [Fact]
    public async Task Sqlite_Allows_DeviceKey_For_Existing_Device()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_devicekey");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var device = CreateSampleDevice(user.UserId, "iOS");
            db.Devices.Add(device);
            await db.SaveChangesAsync();

            var deviceKey = new DeviceKey
            {
                DeviceId = device.DeviceId,
                PublicKey = "DEVICE_PUBLIC_KEY",
                CreatedAt = DateTime.UtcNow
            };

            db.DeviceKeys.Add(deviceKey);
            await db.SaveChangesAsync();

            var loaded = await db.Devices
                .Include(d => d.DeviceKey)
                .SingleAsync(d => d.DeviceId == device.DeviceId);

            Assert.NotNull(loaded.DeviceKey);
            Assert.Equal("DEVICE_PUBLIC_KEY", loaded.DeviceKey!.PublicKey);
        }
    }

    [Fact]
    public async Task Sqlite_Fails_When_DeviceKey_Uses_NonExisting_Device()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var deviceKey = new DeviceKey
            {
                DeviceId = 999,
                PublicKey = "KEY_NO_DEVICE",
                CreatedAt = DateTime.UtcNow
            };

            db.DeviceKeys.Add(deviceKey);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Sqlite_Fails_When_Adding_Two_DeviceKeys_For_Same_Device()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_two_devicekeys");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var device = CreateSampleDevice(user.UserId);
            db.Devices.Add(device);
            await db.SaveChangesAsync();

            var key1 = new DeviceKey
            {
                DeviceId = device.DeviceId,
                PublicKey = "KEY1",
                CreatedAt = DateTime.UtcNow
            };

            var key2 = new DeviceKey
            {
                DeviceId = device.DeviceId,
                PublicKey = "KEY2",
                CreatedAt = DateTime.UtcNow
            };

            db.DeviceKeys.Add(key1);
            await db.SaveChangesAsync();

            // ❗ El error salta al intentar añadir la segunda instancia con misma PK
            Assert.Throws<InvalidOperationException>(() =>
            {
                db.DeviceKeys.Add(key2);
            });
        }
    }

    // ===========================================================
    // SESSION KEY
    // ===========================================================

    [Fact]
    public void Model_SessionKey_Has_PK_And_FKs()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_SessionKey_Has_PK_And_FKs));

        var entity = db.Model.FindEntityType(typeof(SessionKey))!;

        var pk = entity.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk!.Properties);
        Assert.Equal("SessionKeyId", pk.Properties[0].Name);

        var props = entity.GetProperties().ToDictionary(p => p.Name);

        // ChatId opcional
        Assert.True(props["ChatId"].IsNullable);

        var fks = entity.GetForeignKeys().ToList();
        Assert.Equal(3, fks.Count);

        var fkToChat = fks.Single(fk => fk.PrincipalEntityType.ClrType == typeof(Chat));
        Assert.Equal("ChatId", Assert.Single(fkToChat.Properties).Name);

        var fkToLocal = fks.Single(fk => fk.DependentToPrincipal!.Name == "LocalDevice");
        Assert.Equal(typeof(Device), fkToLocal.PrincipalEntityType.ClrType);
        Assert.Equal("LocalDeviceId", Assert.Single(fkToLocal.Properties).Name);
        Assert.Equal(DeleteBehavior.Restrict, fkToLocal.DeleteBehavior);

        var fkToRemote = fks.Single(fk => fk.DependentToPrincipal!.Name == "RemoteDevice");
        Assert.Equal(typeof(Device), fkToRemote.PrincipalEntityType.ClrType);
        Assert.Equal("RemoteDeviceId", Assert.Single(fkToRemote.Properties).Name);
        Assert.Equal(DeleteBehavior.Restrict, fkToRemote.DeleteBehavior);
    }

    [Fact]
    public async Task Sqlite_Allows_SessionKey_With_Chat_And_Devices()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_sessionkey");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var localDevice = CreateSampleDevice(user.UserId, "Android");
            var remoteDevice = CreateSampleDevice(user.UserId, "iOS");
            db.Devices.AddRange(localDevice, remoteDevice);
            await db.SaveChangesAsync();

            var chat = new Chat
            {
                CreatedByUserId = user.UserId,
                CreatedAt = DateTime.UtcNow,
                Title = "Chat SessionKey",
                IsEncrypted = true,
                Type = ChatType.Direct
            };
            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            var sk = new SessionKey
            {
                SessionKeyId = Guid.NewGuid(),
                ChatId = chat.ChatId,
                LocalDeviceId = localDevice.DeviceId,
                RemoteDeviceId = remoteDevice.DeviceId,
                KeyMaterial = "KEY_MATERIAL",
                CreatedAt = DateTime.UtcNow
            };

            db.SessionKeys.Add(sk);
            await db.SaveChangesAsync();

            var loaded = await db.SessionKeys
                .Include(s => s.Chat)
                .Include(s => s.LocalDevice)
                .Include(s => s.RemoteDevice)
                .SingleAsync(s => s.SessionKeyId == sk.SessionKeyId);

            Assert.NotNull(loaded.Chat);
            Assert.NotNull(loaded.LocalDevice);
            Assert.NotNull(loaded.RemoteDevice);
        }
    }

    [Fact]
    public async Task Sqlite_Allows_SessionKey_Without_Chat()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_sessionkey_nochat");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var localDevice = CreateSampleDevice(user.UserId, "Android");
            var remoteDevice = CreateSampleDevice(user.UserId, "Android");
            db.Devices.AddRange(localDevice, remoteDevice);
            await db.SaveChangesAsync();

            var sk = new SessionKey
            {
                SessionKeyId = Guid.NewGuid(),
                ChatId = null,
                LocalDeviceId = localDevice.DeviceId,
                RemoteDeviceId = remoteDevice.DeviceId,
                KeyMaterial = "KEY_NO_CHAT",
                CreatedAt = DateTime.UtcNow
            };

            db.SessionKeys.Add(sk);
            await db.SaveChangesAsync();

            var loaded = await db.SessionKeys
                .SingleAsync(s => s.SessionKeyId == sk.SessionKeyId);

            Assert.Null(loaded.ChatId);
        }
    }

    [Fact]
    public async Task Sqlite_Fails_When_LocalDevice_Does_Not_Exist()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_invalid_local");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var remoteDevice = CreateSampleDevice(user.UserId, "Android");
            db.Devices.Add(remoteDevice);
            await db.SaveChangesAsync();

            var sk = new SessionKey
            {
                SessionKeyId = Guid.NewGuid(),
                ChatId = null,
                LocalDeviceId = 999,
                RemoteDeviceId = remoteDevice.DeviceId,
                KeyMaterial = "KEY_INVALID_LOCAL",
                CreatedAt = DateTime.UtcNow
            };

            db.SessionKeys.Add(sk);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Sqlite_Fails_When_Deleting_Device_Referenced_By_SessionKey()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_delete_device");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var localDevice = CreateSampleDevice(user.UserId, "Android");
            var remoteDevice = CreateSampleDevice(user.UserId, "iOS");
            db.Devices.AddRange(localDevice, remoteDevice);
            await db.SaveChangesAsync();

            var sk = new SessionKey
            {
                SessionKeyId = Guid.NewGuid(),
                ChatId = null,
                LocalDeviceId = localDevice.DeviceId,
                RemoteDeviceId = remoteDevice.DeviceId,
                KeyMaterial = "KEY_DELETE_DEVICE",
                CreatedAt = DateTime.UtcNow
            };

            db.SessionKeys.Add(sk);
            await db.SaveChangesAsync();

            // ❗ El error se produce al intentar Remove, por relación requerida
            Assert.Throws<InvalidOperationException>(() =>
            {
                db.Devices.Remove(localDevice);
            });
        }
    }
}
