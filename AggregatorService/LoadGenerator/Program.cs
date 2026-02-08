using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.HttpLogging;

namespace LoadGenerator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddSingleton<Services.MarketSimulationService>();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient("aggregator", client =>
        {
            client.BaseAddress = new Uri("http://localhost:58773");
        });

        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
            logging.RequestBodyLogLimit = 4096;
            logging.ResponseBodyLogLimit = 4096;
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();
        app.UseWebSockets();
        app.UseHttpLogging();
        app.MapControllers();

        app.Run();
    }
}
