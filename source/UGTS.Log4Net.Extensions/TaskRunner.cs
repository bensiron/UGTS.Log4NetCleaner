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
            if (!wait) return Task.Run(action);

            var taskSource = new TaskCompletionSource<bool>();
            try
            {
                action();
                taskSource.SetResult(true);
            }
            catch (Exception e)
            {
                taskSource.SetException(e);
            }
            return taskSource.Task;
        }
    }
}
