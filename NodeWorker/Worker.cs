using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NodeWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMqWorkerBase _worker;

        public Worker(ILogger<Worker> logger, RabbitMqWorkerBase worker)
        {
            this._worker = worker;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            _worker.CreateWorkUnit();
            token.WaitHandle.WaitOne();
            _logger.LogInformation("ExecuteAsync Finish!");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Start Stop...Wait for queue to comsume stop.");
            await _worker.GracefulShutDown();
            _logger.LogInformation("Stop Service.");
            await base.StopAsync(cancellationToken);
        }
    }
}
