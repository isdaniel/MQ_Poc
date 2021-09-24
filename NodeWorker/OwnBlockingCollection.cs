using System.Collections.Generic;
using System.Threading;

namespace NodeWorker
{
    internal class OwnBlockingCollection<T>
    {
        private volatile bool _finished;
        private Queue<T> _taskQueue = new Queue<T>();
        private readonly int _maxSize;
        private readonly AutoResetEvent _dequeueNotify = new AutoResetEvent(false);
        private readonly AutoResetEvent _enqueueNotify = new AutoResetEvent(false);
        internal OwnBlockingCollection(int maxSize)
        {
            this._maxSize = maxSize;
        }
        internal bool Enqueue(T task){
            while (true)
            {  
                lock(_taskQueue){
                    if(_taskQueue.Count < _maxSize){
                        _taskQueue.Enqueue(task);
                        _dequeueNotify.Set();
                        return true;
                    }
                }
                
                if(_finished)
                    return false;
                    
                _enqueueNotify.WaitOne();
            }
        }

        public bool IsEmpty {
            get{
                lock(_taskQueue)
                    return _taskQueue.Count == 0;
            }  
        }

        internal T Dequeue(){
            while(true){
                lock(_taskQueue){
                    if(_taskQueue.TryDequeue(out T task)){
                        _enqueueNotify.Set();
                        return task;
                    }
                }
                if(_finished)
                    break;
                _dequeueNotify.WaitOne();
            }
            return default(T);
        }
        internal void CloseQueue(){
            _finished = true;
            _enqueueNotify.Set();
            _dequeueNotify.Set();
        }
    }
}
