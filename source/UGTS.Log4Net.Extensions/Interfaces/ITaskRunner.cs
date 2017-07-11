using System;
using System.Threading.Tasks;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface ITaskRunner
    {
        Task Run(Action action, bool wait);
    }
}