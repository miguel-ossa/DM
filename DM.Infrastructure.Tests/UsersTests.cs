using DM.Domain.Entities;
using DM.Infrastructure.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DM.Infrastructure.Tests;

public class UsersTests
{
    private User CreateSampleUser(string userName = "user1")
        => new()
        {
            UserName = userName,
            DisplayName = "Usuario de prueba",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

    [Fact]
    public async Task Can_Add_User()
    {
        // Arrange
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_Add_User));
        var user = CreateSampleUser("user_add");

        // Act
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Assert
        var saved = await db.Users.SingleOrDefaultAsync(u => u.UserName == "user_add");
        Assert.NotNull(saved);
        Assert.True(saved!.UserId > 0); // PK generada
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Can_Query_User_By_UserName()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_Query_User_By_UserName));

        var user1 = CreateSampleUser("alice");
        var user2 = CreateSampleUser("bob");

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var result = await db.Users.SingleOrDefaultAsync(u => u.UserName == "bob");

        Assert.NotNull(result);
        Assert.Equal("bob", result!.UserName);
    }

    [Fact]
    public async Task Can_Update_User_DisplayName()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_Update_User_DisplayName));

        var user = CreateSampleUser("user_update");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act: modificar
        user.DisplayName = "Nombre actualizado";
        await db.SaveChangesAsync();

        // Assert
        var reloaded = await db.Users.SingleAsync(u => u.UserName == "user_update");
        Assert.Equal("Nombre actualizado", reloaded.DisplayName);
    }

    [Fact]
    public async Task Can_SoftDelete_User_By_Setting_IsActive_False()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_SoftDelete_User_By_Setting_IsActive_False));

        var user = CreateSampleUser("user_delete");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act: “borrado lógico”
        user.IsActive = false;
        await db.SaveChangesAsync();

        // Assert
        var reloaded = await db.Users.SingleAsync(u => u.UserName == "user_delete");
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task Can_HardDelete_User()
    {
        using var db = DbContextHelper.CreateInMemoryDbContext(nameof(Can_HardDelete_User));

        var user = CreateSampleUser("user_hard_delete");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act: borrado físico
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        // Assert
        var exists = await db.Users.AnyAsync(u => u.UserName == "user_hard_delete");
        Assert.False(exists);
    }
}
