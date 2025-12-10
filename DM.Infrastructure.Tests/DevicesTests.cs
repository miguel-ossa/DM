using System;
using System.Threading.Tasks;
using DM.Domain.Entities;
using DM.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DM.Infrastructure.Tests;

public class DevicesTests
{
    private User CreateSampleUser(string userName = "user1")
        => new()
        {
            UserName = userName,
            DisplayName = "Usuario de prueba",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

    private Device CreateSampleDevice(int userId)
        => new()
        {
            UserId = userId,
            Platform = "Android",
            PushToken = "token123",
            IsTrusted = true,
            LastSeenAt = DateTime.UtcNow
        };

    [Fact]
    public void Model_Has_FK_From_Device_To_User()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Model_Has_FK_From_Device_To_User));

        var deviceEntity = db.Model.FindEntityType(typeof(Device))!;
        var fks = deviceEntity.GetForeignKeys();

        var fkToUser = Assert.Single(fks, fk => fk.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal("UserId", Assert.Single(fkToUser.Properties).Name);
    }

    [Fact]
    public async Task Sqlite_Creates_Device_With_Existing_User()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var user = CreateSampleUser("user_devices");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var device = CreateSampleDevice(user.UserId);
            db.Devices.Add(device);
            await db.SaveChangesAsync();

            var loadedDevice = await db.Devices
                .Include(d => d.User)
                .SingleAsync(d => d.DeviceId == device.DeviceId);

            Assert.NotNull(loadedDevice.User);
            Assert.Equal("user_devices", loadedDevice.User!.UserName);
        }
    }

    [Fact]
    public async Task Sqlite_Fails_When_Device_Has_NonExisting_UserId()
    {
        var (db, conn) = DbContextHelper.CreateSqliteInMemoryDbContext();
        await using (db)
        await using (conn)
        {
            var device = CreateSampleDevice(userId: 999); // FK inválida

            db.Devices.Add(device);

            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await db.SaveChangesAsync();
            });
        }
    }
}
