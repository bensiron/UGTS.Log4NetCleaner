using System;
using System.Threading.Tasks;
using UGTS.Log4Net.Extensions.Interfaces;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions
{
    public class TaskRunner : ITaskRunner
    {
        public Task Run(Action action, WaitType wait)
        {
            return wait == WaitType.Never ? Task.Run(action) : RunTaskOnSameThread(action);
        }

        private static Task RunTaskOnSameThread(Action action)
        {
            var taskSource = new TaskCompletionSource<object>();
            try
            {
                action();
                taskSource.SetResult(null);
            }
            catch (Exception e)
            {
                taskSource.SetException(e);
            }
            return taskSource.Task;
        }
    }
}
