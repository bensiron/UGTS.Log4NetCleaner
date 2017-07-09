using System;
using System.Threading.Tasks;
using UGTS.Log4Net.Extensions.Interfaces;

namespace UGTS.Log4Net.Extensions
{
    public class TaskRunner : ITaskRunner
    {
        public Task Run(Action action)
        {
            return Task.Run(action);
        }
    }
}
