using System.Data.Common;
using DM.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DM.Infrastructure.Tests.Helpers;

public static class DbContextHelper
{
    // Para tests lógicos (sin FK real)
    public static AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }

    // Para tests relacionales con FK reales (SQLite en memoria)
    public static (AppDbContext Context, DbConnection Connection) CreateSqliteInMemoryDbContext()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);

        // Crea el esquema según tu modelo EF
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
