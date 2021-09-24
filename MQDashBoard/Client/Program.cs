using Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private const string ClusterId = "dev";
        private const string ServiceId = "OrleansSample";

		private const string Invariant = "System.Data.SqlClient";
        private const string ConnectionString = "Server=localhost;Database=orleans;User Id=sa;Password=test.123;";

        private const int InitializeAttemptsBeforeFailing = 5;
        private static int attempt = 0;

        static async Task Main(string[] args)
        {
            Console.Title = "Client";

            await RunMainAsync();
        }

        private static async Task RunMainAsync()
        {
            try
            {
                using var client = await InitialiseClient();
                Console.WriteLine($"Clinet Staring....");
                await DoClientWork(client);
                Console.WriteLine($"Clinet End");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error on process CorrelationId was {Thread.GetData(Thread.GetNamedDataSlot("CorrelationId"))} : {ex}");
                throw;
            }
        }

        private static async Task<IClusterClient> InitialiseClient()
        {
            var client = new ClientBuilder()
                    .UseAdoNetClustering(options =>
                    {
                        options.Invariant = Invariant;
                        options.ConnectionString = Environment.GetEnvironmentVariable("DBConnection") ?? ConnectionString;
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = ClusterId;
                        options.ServiceId = ServiceId;
                    })
                    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IMQMessagePrinter).Assembly))
                    .ConfigureLogging(log => log.SetMinimumLevel(LogLevel.Warning).AddConsole())
                    .Build();

            await client.Connect(RetryFilter);

            return client;
        }

        private static async Task<bool> RetryFilter(Exception exception)
        {
            if (exception.GetType() != typeof(SiloUnavailableException))
            {
                Console.WriteLine($"Cluster client failed to connect to cluster with unexpected error.  Exception: {exception}");
                return false;
            }
            attempt++;
            Console.WriteLine($"Cluster client attempt {attempt} of {InitializeAttemptsBeforeFailing} failed to connect to cluster.  Exception: {exception}");
            if (attempt > InitializeAttemptsBeforeFailing)
            {
                return false;
            }
            await Task.Delay(TimeSpan.FromSeconds(3));
            return true;
        }

        private static async Task DoClientWork(IClusterClient client)
        {
            string line = null;
            int i = 0;
            while ((line = Console.ReadLine()) != "quit")
            {
                if(TryParseJson(line,out MessageTask task)){
                    Thread.SetData(Thread.GetNamedDataSlot("CorrelationId"),task.CorrelationId);
                    var grain = client.GetGrain<IMQMessagePrinter>(line);
                    
                    if(task.Group.Equals("groupA",StringComparison.InvariantCultureIgnoreCase))
                        await grain.GroupA(line);
                    else if(task.Group.Equals("groupB",StringComparison.InvariantCultureIgnoreCase))
                        await grain.GroupB(line);
                }
                
                if(i == 10)
                    throw new Exception();
                i++;
            }

            await client.Close();
        }

        public static bool TryParseJson<T>(string @this, out T result)
        {
            bool success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            result = JsonConvert.DeserializeObject<T>(@this, settings);
            return success;
        }
    }

    public class MessageTask
    {

        public string Group { get; set; }
        public string Message { get; set; }

        public string CorrelationId { get; set; }
    }
}
