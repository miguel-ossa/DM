using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DM.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            "server=localhost;port=3306;database=dm;user=dm_user;password=dm_password;";

        var serverVersion = ServerVersion.AutoDetect(connectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, serverVersion)
            .Options;

        return new AppDbContext(options);
    }
}
