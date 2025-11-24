using System.Runtime.CompilerServices;

namespace Glimpse.API;

public interface ILogger
{
    public void Log(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = "");
}