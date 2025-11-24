using Glimpse.API;
using Glimpse.Forms;
using Silk.NET.SDL;

namespace Glimpse;

public static class Program
{
    public static unsafe void Main(string[] args)
    {
#if !DEBUG
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
            
            Sdl sdl = Sdl.GetApi();

            const string title = "Glimpse";
            
            string logLocation = Path.Combine(IConfigManager.BaseDir, "LastSession.log");
            string message = $"Oops! Glimpse crashed.\nLog file at: {logLocation}\n\nPlease send log file + the following error to the developers:\n{e}";

            sdl.ShowSimpleMessageBox((uint) MessageBoxFlags.Error, title, message, null);
        }
#endif
    }
}