using DM.Infrastructure.Data;
using DM.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Connection string fija para el seeder
var connectionString = "Server=localhost;Database=dm;User=dm_user;Password=dm_password;";

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

Console.WriteLine("Aplicando migraciones si es necesario...");
await db.Database.MigrateAsync();

Console.WriteLine("Ejecutando seed...");
await DbSeeder.SeedAsync(db);

Console.WriteLine("Seed finalizado.");
