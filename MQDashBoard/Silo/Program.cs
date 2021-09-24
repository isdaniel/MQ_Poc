using Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silo
{
    class Program
    {
        private const string ClusterId = "dev";
        private const string ServiceId = "OrleansSample";

		private const string Invariant = "System.Data.SqlClient";
        private const string ConnectionString = "Server=localhost;Database=orleans;User Id=sa;Password=test.123;";
        private static AutoResetEvent _getway = new AutoResetEvent(false);
        static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (s, a) => {
                a.Cancel = true;
                Task.Run(StopSilo);
            };
            Console.WriteLine($"DBConnection : {Environment.GetEnvironmentVariable("DBConnection") ?? ConnectionString}");
            await RunMainAsync(11220, 35000, 8899);
        }

        private static void StopSilo()
        {
            _getway.Set();
        }

        private static async Task RunMainAsync(int siloPort, int gatewayPort, int dashboardPort)
        {
            try
            {
                var host = await InitialiseSilo(siloPort, gatewayPort, dashboardPort);
                Console.WriteLine("Press enter to exit...");
                
                _getway.WaitOne();

                await host.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task<ISiloHost> InitialiseSilo(int siloPort, int gatewayPort, int dashboardPort)
        {
            var builder = new SiloHostBuilder()
                // Grain State
                .AddAdoNetGrainStorage("OrleansStorage", options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = Environment.GetEnvironmentVariable("DBConnection") ?? ConnectionString;
                    options.UseJsonFormat = true;
                })
                // Membership
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = Environment.GetEnvironmentVariable("DBConnection") ?? ConnectionString;
                })
                .UseDashboard(options =>
                {
                    options.Username = "admin";
                    options.Password = "test.123";
                    options.Port = dashboardPort;

                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = ClusterId;
                    options.ServiceId = ServiceId;
                })
                .ConfigureEndpoints(siloPort: siloPort, gatewayPort: gatewayPort)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(MQMessagePrinter).Assembly).WithReferences())
                .ConfigureLogging(log => log.SetMinimumLevel(LogLevel.Warning).AddConsole());

            var host = builder.Build();
            await host.StartAsync();

            return host;
        }
    }
}
