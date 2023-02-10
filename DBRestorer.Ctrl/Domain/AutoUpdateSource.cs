using System;
using System.Threading.Tasks;

namespace DBRestorer.Ctrl.Domain;

public interface IAutoUpdateSource
{
    Task<bool> Update(Action<int> progressReport);
}

public static class AutoUpdateSource
{
    public static IAutoUpdateSource Source { get; set; }
}