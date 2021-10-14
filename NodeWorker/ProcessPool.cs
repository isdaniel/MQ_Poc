using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NodeWorker
{
    public class ProcessPool : IWorkerPool
    {
        const string CLOSED_SIGNAL = "quit";
        private readonly PoolSetting _poolSetting;
        private BlockingCollection<MessageTask> _taskQueue;
        private List<Task> _workers = new List<Task>();
        private readonly int _processCount;
        private volatile bool _finish = false;
        private List<Process> _processList = new List<Process>();
        public ProcessPool(PoolSetting poolSetting)
        {
            this._processCount = poolSetting.WorkUnitCount;
            this._poolSetting = poolSetting;
            _taskQueue = new BlockingCollection<MessageTask>(poolSetting.WorkUnitCount);
            Init();
        }

        private void Init()
        {
            for (int i = 0; i < _processCount; i++)
            {
                var process = CreateProcess();
                this._workers.Add(Task.Run(()=>{
                    ProcessHandler(process);
                }));
                _processList.Add(process);
            }
        }

        private Process CreateProcess() {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    FileName = _poolSetting.FileName,
                    Arguments = _poolSetting.Arguments,
                    CreateNoWindow = true
                };
            process.Start();

            process.BeginErrorReadLine();
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                System.Console.WriteLine($"Procees Error Information:{e.Data}");
            };

            return process;
        }


        public Task<bool> AddTaskAsync(MessageTask task){
            bool result = false;
            if(!_finish){
                _taskQueue.Add(task);
                result = true;
            }
            return Task.FromResult(result);
        }

        private void ProcessHandler(Process process)
        {
            while (true){
                var task= _taskQueue.Take();
                task.Execute();
                if(_finish && _taskQueue.Count == 0)
                    break;
            }

            process.StandardInput.WriteLine(CLOSED_SIGNAL);
        }

        public async Task WaitFinishedAsync(){
            _finish = true;
        
            foreach (var process in _processList)
            {
                process.WaitForExit();
            }

            await Task.WhenAll(_workers.ToArray());
        }
    }

    public interface IWorkerPool
    {
        Task<bool> AddTaskAsync(MessageTask task);
        Task WaitFinishedAsync();
    }
}
