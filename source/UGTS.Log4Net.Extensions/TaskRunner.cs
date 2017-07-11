using System;
using System.Threading.Tasks;
using UGTS.Log4Net.Extensions.Interfaces;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions
{
    public class TaskRunner : ITaskRunner
    {
        public Task Run(Action action, bool wait)
        {
            var task = Task.Run(action);
            if (wait) task.Wait();
            return task;
        }
    }
}
