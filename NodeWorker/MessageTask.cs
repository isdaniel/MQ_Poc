using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NodeWorker
{
    public class MessageTask
    {
        private readonly ILogger _logger;

        protected  string Group { get;  }
        protected  string Message { get;  }
        protected string CorrelationId{ get;}
        public MessageTask(string message,string group,string correlationId,ILogger logger)
        {
            Message = message;
            Group = group;
            CorrelationId = correlationId;
            this._logger = logger;
        }
        public void Execute(){
            //do your logic here
            Thread.Sleep(1000);
            _logger.LogInformation($"ThreadID[{Thread.CurrentThread.ManagedThreadId}] Group[{Group}] Got Message from MQ：{Message}");
        }

        internal string ToJsonMessage() {
            Thread.Sleep(1000);
            return JsonConvert.SerializeObject(new { this.Group, this.Message, this.CorrelationId });
        }
    }
}
