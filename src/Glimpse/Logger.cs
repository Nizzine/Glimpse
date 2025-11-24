using System.Runtime.CompilerServices;

namespace Glimpse;

public static class Logger
{
    private static StreamWriter _writer;
    
    //[Conditional("DEBUG")]
    public static void Log(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = "")
    {
#if !DEBUG
        if (_writer == null)
        {
            Directory.CreateDirectory(IConfig.BaseDir);
            
            string fileLocation = Path.Combine(IConfig.BaseDir, "LastSession.log");

            Console.WriteLine($"Initializing log file {fileLocation}");
            _writer = new StreamWriter(fileLocation)
            {
                AutoFlush = true
            };
        }
#endif
        
        string localFile = Path.GetFileName(file);

        string logText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [LOG] ({localFile}:{lineNumber}) {message}";
#if !DEBUG
        _writer.WriteLine(logText);
#else
        Console.WriteLine(logText);
#endif
    }
}