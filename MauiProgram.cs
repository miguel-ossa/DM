using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DM.Infrastructure.Data;

namespace DM;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // 🔹 Connection string MySQL (ajusta usuario/clave/bd a lo tuyo)
        var connectionString = "server=localhost;port=3306;database=dm;user=dm_user;password=dm_password;";

        // 🔹 Detecta versión de MySQL automáticamente
        var serverVersion = ServerVersion.AutoDetect(connectionString);

        // 🔹 Registro de AppDbContext usando MySQL (Pomelo)
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        return builder.Build();
    }
}
