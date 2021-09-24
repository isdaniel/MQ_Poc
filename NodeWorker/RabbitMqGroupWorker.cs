using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace NodeWorker
{
    public class RabbitMqGroupWorker : RabbitMqWorkerBase
    {
        private readonly Dictionary<string,IWorkerPool> _poolMap;
        public RabbitMqGroupWorker(
            RabbitMqSetting setting,
            ILogger<RabbitMqGroupWorker> logger,
            IPoolFactory poolFactory) : base(setting,logger)
        {
            _poolMap = poolFactory.GetPools(setting.PoolSettings,setting.PoolType);
        }

        protected override async Task<bool> ExecuteAsync(BasicDeliverEventArgs args)
        {
            var group = GetGroup(args);
            if (string.IsNullOrEmpty(group) || !_poolMap.TryGetValue(group, out var workerPool))
            {
                //send wrong group data in this queue......
                Logger.LogInformation($"data send wrong queue group type is {group ?? "Empty"}......");
                return false;
            }
            else
            {
                var message = Encoding.UTF8.GetString(args.Body.Span);
                //System.Console.WriteLine(message);
                return  await workerPool.AddTaskAsync(new MessageTask(message, group,GetCorrelationId(args),Logger));
            }
        }
        private string GetGroup(BasicDeliverEventArgs e)
        {
            if(e.BasicProperties.Headers.TryGetValue("group",out var g))
                return Encoding.UTF8.GetString((byte[])g);
        
            return string.Empty;
        }

        private string GetCorrelationId(BasicDeliverEventArgs args){
            return args.BasicProperties.IsCorrelationIdPresent() ? $"{args.BasicProperties.CorrelationId}---WorkerNode:{Environment.MachineName}" : "CorrelationId-Not-Exists";
        }
        
        private async Task CloseAllPoolAsync()
        {
            List<Task> closePoolTasks = new List<Task>();
            
            foreach (var item in _poolMap)
            {
                closePoolTasks.Add(Task.Run(async () =>
                {
                    await item.Value.WaitFinishedAsync();
                }));
            }
            
            await Task.WhenAll(closePoolTasks.ToArray());
        }

        protected override async Task GracefulReleaseAsync()
        {
            await CloseAllPoolAsync();
        }
    }
}
