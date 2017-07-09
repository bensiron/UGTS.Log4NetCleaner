using System;
using System.Threading.Tasks;

namespace UGTS.Log4Net.Extensions.Interfaces
{
    public interface ITaskRunner
    {
        Task Run(Action action);
    }
}