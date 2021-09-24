using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NodeWorker
{

    public class ThreadPool : IWorkerPool
    {
        private readonly int _limitThreadCount;
        private OwnBlockingCollection<MessageTask> _taskQueue;
        public readonly List<Thread> _threadPool = new List<Thread>();
        private readonly ManualResetEvent _queueSignal = new ManualResetEvent(false);
        private volatile bool _finished;
        public ThreadPool(int limitThreadCount,int prefetchTaskCount)
        {
            _taskQueue = new OwnBlockingCollection<MessageTask>(prefetchTaskCount);
            this._limitThreadCount = limitThreadCount;
            InitPool();
        }

        public Task<bool> AddTaskAsync(MessageTask task)
        {
            var result = _taskQueue.Enqueue(task);
            _queueSignal.Set();

            return Task.FromResult(result);
        }

        private void InitPool()
        {
            for (int i = 0; i < _limitThreadCount; i++)
            {
                Thread worker = new Thread(Process);
                worker.Start();
                _threadPool.Add(worker);
            }
        }

        private void Process()
        {
            while (true)
            {
                while(!_taskQueue.IsEmpty){
                    var task = _taskQueue.Dequeue();
                    if(task != null)
                        task.Execute();
                }

                if (_finished)
                {
                    break;
                }

                _queueSignal.WaitOne();
                this._queueSignal.Reset();
            }
        }

        public async Task WaitFinishedAsync()
        {
            _finished = true;
            this._queueSignal.Set();

            _taskQueue.CloseQueue();

            await Task.Run(()=>{
                do
                {
                    var thread = _threadPool[0];
                    thread.Join();
                    _threadPool.Remove(thread);
                } while (_threadPool.Count > 0);
            });
        }
    }
}
