using System.IO.Pipes;
using Glimpse.API;
using Glimpse.Forms;
using SDL3;

namespace Glimpse;

public static class Program
{
    public static unsafe void Main(string[] args)
    {
        Console.WriteLine("Checking for an already existing instance.");
        NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", Glimpse.PipeServerName, PipeDirection.Out);
        try
        {
            pipeClient.Connect(100);
            Console.WriteLine("Found!");
            if (args.Length > 0)
            {
                BinaryWriter writer = new BinaryWriter(pipeClient);
                writer.Write((byte) CommunicationFlags.PlayFile);
                writer.Write(args[0]);
            }
            return;
        }
        catch (TimeoutException e) { }
        pipeClient.Dispose();
        
#if !DEBUG
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            Logger logger = new Logger();
            string msg = eventArgs.ExceptionObject?.ToString() ??
                         "An error occurred, but there was no message attached.";
            logger.Log(msg);
            SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Error, "Glimpse", msg, 0);
        };

        try
#endif
        {
            using Glimpse glimpse = new Glimpse();
            glimpse.Run(new GlimpsePlayer(), args);
        }
#if !DEBUG
        catch (Exception e)
        {
            Logger logger = new Logger();
            logger.Log(e.ToString());

            const string title = "Glimpse";
            
            string logLocation = Path.Combine(IConfigManager.BaseDir, "LastSession.log");
            string message = $"Oops! Glimpse crashed.\nLog file at: {logLocation}\n\nPlease send log file + the following error to the developers:\n{e}";

            SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Error, title, message, IntPtr.Zero);
        }
#endif
    }

    [Flags]
    public enum CommunicationFlags : byte
    {
        None = 0,
        
        PlayFile = 1 << 0
    }
}