using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NodeWorker
{
    /// <summary>
    /// worker base to handle infrastructure and connection matter, export an Execute method let subclass implement their logic
    /// </summary>
    public abstract class RabbitMqWorkerBase 
    {
        public RabbitMqSetting Setting { get; }
        protected AsyncEventHandler<BasicDeliverEventArgs> ReceiveEvent;
        private IConnection _conn;
        private IModel _channle;
        private AsyncEventingBasicConsumer _consumer;
        protected ILogger<RabbitMqWorkerBase> Logger { get; }
        public RabbitMqWorkerBase(
            RabbitMqSetting setting,
            ILogger<RabbitMqWorkerBase> logger)
        {
            this.Logger = logger;
            this.Setting = setting;

            var _connFactory = new ConnectionFactory
            {
                Uri = setting.GetUri(),
                DispatchConsumersAsync = true // async mode
            };

            _conn = _connFactory.CreateConnection();
            
        }

        /// <summary>
        /// 在 subclass 可以返回結果，來代表是否做完此訊息
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected abstract Task<bool> ExecuteAsync(BasicDeliverEventArgs args);

        public void CreateWorkUnit()
        {
            _channle = _conn.CreateModel();
            _consumer = new AsyncEventingBasicConsumer(_channle);
            _channle.BasicQos(0, Setting.PrefetchTaskCount, true);
            _channle.BasicConsume(Setting.QueueName, false, _consumer);
            ReceiveEvent = async (object sender, BasicDeliverEventArgs e) =>
            {
                try
                {
                    var ackReuslt = await ExecuteAsync(e);
                    if(ackReuslt)
                        _channle.BasicAck(e.DeliveryTag, false);
                    else
                        _channle.BasicNack(e.DeliveryTag, false, true);
                }
                catch (Exception ex)
                {
                    _channle.BasicNack(e.DeliveryTag, false, true);
                    Logger.LogError(ex,ex.ToString());
                }
                await Task.Yield();
            };
            _consumer.Received += ReceiveEvent;
        }

        protected virtual async Task GracefulReleaseAsync()
        {
            await Task.CompletedTask;
        }

        public async Task GracefulShutDown()
        {
            _consumer.Received -= ReceiveEvent;
            ReceiveEvent = null;
            //wait for all unit tasks be done.
            Logger.LogInformation("Wait for Pool Close!!!!");

            await GracefulReleaseAsync();
            
            if (_channle.IsOpen)
                _channle.Close();
            
            if (_conn.IsOpen)
                _conn.Close();
            
            Logger.LogInformation("RabbitMQ Conn Closed!!!!");
        }
    }
}
