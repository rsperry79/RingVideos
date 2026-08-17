using System;
using System.Threading;

namespace RingVideos
{
   /// <summary>
   /// Process-wide Ctrl+C signal. Registered directly against Console.CancelKeyPress in Program.Main
   /// (rather than relying solely on IHostApplicationLifetime) so it fires immediately even while the
   /// app is deep inside an awaited download loop or blocked on Console.ReadLine.
   /// </summary>
   public static class ShutdownSignal
   {
      public static readonly CancellationTokenSource Cts = new();

      public static void Register()
      {
         Console.CancelKeyPress += (s, e) =>
         {
            e.Cancel = true; // prevent the runtime from killing the process outright

            if (Cts.IsCancellationRequested)
            {
               // Second Ctrl+C - the user wants out now
               Console.WriteLine();
               Console.WriteLine("Force quitting...");
               Environment.Exit(130);
               return;
            }

            Cts.Cancel();

            if (RingVideoApplication.IsRunActive)
            {
               Console.WriteLine();
               Console.WriteLine("Ctrl+C received - finishing in-flight downloads and saving progress... (press Ctrl+C again to force quit)");
            }
            else
            {
               // Idle at the REPL prompt - nothing in-flight to protect, exit immediately
               Environment.Exit(0);
            }
         };
      }
   }
}
