using Glimpse.API;
using Glimpse.Forms;
using SDL3;

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

            const string title = "Glimpse";
            
            string logLocation = Path.Combine(IConfigManager.BaseDir, "LastSession.log");
            string message = $"Oops! Glimpse crashed.\nLog file at: {logLocation}\n\nPlease send log file + the following error to the developers:\n{e}";

            SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Error, title, message, IntPtr.Zero);
        }
#endif
    }
}