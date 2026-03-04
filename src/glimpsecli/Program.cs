using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Glimpse.API;
using Glimpse.Audio;

public static class GlimpseCli
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }
        
        List<string> files = new List<string>();
        float? volume = null;
        double? speed = null;
        bool shuffle = false;
        int startingTrack = 0;

        int argIndex = 0;
        while (ReadArg(args, ref argIndex, out string arg))
        {
            if (arg.StartsWith('-'))
            {
                switch (arg)
                {
                    case "--help" or "-h":
                        PrintHelp();
                        return;
                    
                    case "--volume" or "-v":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && float.TryParse(arg, out float vol))
                        {
                            volume = vol;
                            continue;
                        }
                        
                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine("ERROR: Volume was not parsable.");
                        return;
                    }
                
                    case "--speed" or "-s":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && double.TryParse(arg, out double spd))
                        {
                            speed = spd;
                            continue;
                        }
                        
                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine("ERROR: Speed was not parsable.");
                        return;
                    }

                    case "--track" or "-t":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && int.TryParse(arg, out int trackNumber))
                        {
                            startingTrack = trackNumber - 1;
                            continue;
                        }

                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine("ERROR: Track number was not parsable.");
                        return;
                    }
                    
                    case "--shuffle":
                        shuffle = true;
                        break;
                    
                    default:
                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine($"ERROR: Invalid argument \"{arg}\".");
                        return;
                }
            }
            else
            {
                string fileName = arg.Trim('"');

                if (File.Exists(fileName))
                {
                    files.Add(fileName);
                }
                else if (Directory.Exists(fileName))
                {
                    foreach (string file in Directory.EnumerateFiles(fileName, "*.*", SearchOption.AllDirectories).Where(s => Path.GetExtension(s) is ".mp3" or ".ogg" or ".wav" or ".flac"))
                        files.Add(file);
                }
                else
                {
                    PrintHelp();
                    Console.WriteLine();
                    Console.WriteLine($"ERROR: Argument {argIndex}: An invalid file was provided.");
                    return;
                }
            }
        }
        
        if (files.Count == 0)
        {
            PrintHelp();
            Console.WriteLine();
            Console.WriteLine("ERROR: No file was provided.");
            return;
        }

        if (shuffle)
        {
            Random random = new Random();
            List<string> shuffled = new List<string>(files.Count);

            while (shuffled.Count < files.Count)
            {
                int randomId = random.Next(files.Count);
                string file = files[randomId];
                if (shuffled.Contains(file))
                    continue;
                shuffled.Add(file);
            }

            files = shuffled;
        }

        PlayerSettings settings = new PlayerSettings()
        {
            SampleRate = 48000,
            Volume = volume ?? 1.0f,
            SpeedAdjust = speed ?? 1.0f
        };
        AudioPlayer player = new AudioPlayer(null, settings);
        
        foreach (string path in files)
            player.QueueTrack(path, QueueSlot.AtEnd);

        player.ChangeTrack(startingTrack);

        PrintConsoleText(player.CurrentTrack, 0, (int) player.TrackLength.TotalSeconds, player.TrackState,
            player.CurrentTrackIndex, files.Count);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            ResetConsole();
        };
        
        Console.CursorVisible = false;

        while (player.TrackState != TrackState.Stopped)
        {
            int elapsed = (int) player.ElapsedTime.TotalSeconds;
            int total = (int) player.TrackLength.TotalSeconds;

            (int left, int top) = Console.GetCursorPosition();
            Console.SetCursorPosition(left, top - 8);
            PrintConsoleText(player.CurrentTrack, elapsed, total, player.TrackState, player.CurrentTrackIndex, files.Count);

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.P:
                    {
                        if (player.TrackState == TrackState.Playing)
                            player.Pause();
                        else
                            player.Play();

                        break;
                    }
                    
                    case ConsoleKey.Q:
                        player.Stop();
                        break;

                    case ConsoleKey.OemPeriod:
                    {
                        player.Next();
                        break;
                    }

                    case ConsoleKey.OemComma:
                    {
                        player.Previous();
                        break;
                    }
                }
            }
            
            Thread.Sleep(125);
        }
        
        
        ResetConsole();
        player.Dispose();
    }

    private static void PrintConsoleText(TrackInfo info, int elapsed, int total, TrackState state, int track, int totalTracks)
    {
        int padAmount = Console.BufferWidth;
        
        Console.WriteLine($"Track:  {track + 1} / {totalTracks}".PadRight(padAmount));
        Console.WriteLine($"Title:  {info.Title}".PadRight(padAmount));
        Console.WriteLine($"Artist: {info.Artist}".PadRight(padAmount));
        Console.WriteLine($"Album:  {info.Album}".PadRight(padAmount));
        
        Console.WriteLine();
        
        Console.WriteLine(state.ToString().PadRight(60));
        Console.Write($"{elapsed / 60}:{elapsed % 60:00} [");

        int progress = (int) (((double) elapsed / total) * 51) - 1;
    
        for (int i = 0; i < 50; i++)
        {
            if (i <= progress)
                Console.Write('=');
            else
                Console.Write('-');
        }
    
        Console.WriteLine($"] {total / 60}:{total % 60:00}".PadRight(padAmount));
    }

    private static void ResetConsole()
    {
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    private static bool ReadArg(string[] args, ref int index, out string arg)
    {
        arg = null;
        
        if (index >= args.Length)
            return false;

        arg = args[index++];
        return true;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
                          glimpsecli

                          Usage: glimpsecli [options] <files/directories>

                          Options:
                              --track <n>, -t <n>
                                  Start at track n.
                              --volume <v>, -v <v>
                                  Change the playback volume, where a value of 1.0 is 100% volume.
                              --speed <s>, -s <s>
                                  Change the playback speed, where a value of 1.0 is 100% speed;
                              --shuffle
                                  Shuffle the playback.
                          """);
    }
}

