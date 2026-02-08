namespace LoadGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.AddServiceDefaults();

            builder.Services.AddSingleton<LoadGenerator.Services.MarketSimulationService>();
            builder.Services.AddHttpClient("aggregator", client =>
            {
                client.BaseAddress = new Uri("http://aggregator");
            });

            var host = builder.Build();

            host.Run();
        }
    }
}
