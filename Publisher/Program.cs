using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;

namespace Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            string exchangeName = "my-direct";
            string routeKey = "*";
            string queueName =  Environment.GetEnvironmentVariable("QUEUENAME");
            string hostName =  Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME");
            string userName =  Environment.GetEnvironmentVariable("USERNAME")??"guest";
            string password =  Environment.GetEnvironmentVariable("PASSWORD")??"guest";
            string port =  Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";

            ConnectionFactory factory = new ConnectionFactory
            {
                Uri = new Uri($"amqp://{userName}:{password}@{hostName}:{port}")
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queueName,true,false,false,null);
                channel.ExchangeDeclare(exchangeName,ExchangeType.Direct.ToString(),true,false,null);
                channel.QueueBind(queueName,exchangeName,routeKey);
                var prop = channel.CreateBasicProperties();
                prop.Headers = new Dictionary<string, object>();
                prop.CorrelationId = $"{Guid.NewGuid().ToString()}---publisher:{Environment.MachineName}";
                
                string group = string.Empty;
                int i = 0;
                while (true)
                {
                    if(i%2==0)
                        group = "groupA";
                    else
                        group = "groupB";
                        
                    prop.Headers["group"] = group;
                    
                    string msg = $"Send Time[{DateTime.Now:yyyy/MM/dd hh:mm:sss:fff}] this message belong with {group}";
                    var sendBytes = Encoding.UTF8.GetBytes(msg);
                    System.Console.WriteLine(msg);
                    //發布訊息到RabbitMQ Server
                    channel.BasicPublish(exchangeName, routeKey, prop, sendBytes);
                    Thread.Sleep(1000);
                    i++;
                }
            }
        }
    }
}
