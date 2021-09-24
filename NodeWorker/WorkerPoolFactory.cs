using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeWorker
{
    public class WorkerPoolFactory : IPoolFactory {
        public Dictionary<string,IWorkerPool> GetPools(PoolSetting[] setting,PoolType poolType){
            if(poolType == PoolType.Process){

                if(setting.Any(x=>string.IsNullOrEmpty(x.FileName))){
                    throw new ArgumentException("PoolType.Process need to declare FilePath!");
                }

                return setting.ToDictionary(x=>x.Group, y => (IWorkerPool)new ProcessPool(y));
            }
            
            return setting.ToDictionary(x=>x.Group,y=> (IWorkerPool)new ThreadPool(y.WorkUnitCount,1));
        }
    }

    public interface IPoolFactory
    {
        Dictionary<string, IWorkerPool> GetPools(PoolSetting[] setting, PoolType poolType);
    }
}
