using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace RingVideos.Writers
{
   public class ConsoleWriter
   {
      private ILogger<ConsoleWriter> log;
      private IConsole console;
      private object lockObj = new object();
      private ThreadSafeList<LineWriter> lineWriters;
      private int footerStatusLinePosition = -1;
      private int footerSeparatorLinePosition = -1;

      public ConsoleWriter(ILogger<ConsoleWriter> log) : this(log, new SystemConsole())
      {
      }

      public ConsoleWriter(ILogger<ConsoleWriter> log, IConsole console)
      {
         this.log = log;
         this.console = console;
         lineWriters = new ThreadSafeList<LineWriter>();
         InitializeFooter();
      }

      private void InitializeFooter()
      {
         try
         {
            Monitor.Enter(lockObj);
            footerStatusLinePosition = console.BufferHeight - 1;
            footerSeparatorLinePosition = console.BufferHeight - 2;

            // Write separator line
            console.SetCursorPosition(0, footerSeparatorLinePosition);
            console.WriteLine();

            // Write empty status line
            console.SetCursorPosition(0, footerStatusLinePosition);
            console.WriteLine();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      /// <summary>
      /// Grows the console screen buffer (if needed) so a run with many downloads never fills it up.
      /// Every per-download status line lives at a fixed absolute row (see GetLineWriter/Update below);
      /// once total output exceeds the buffer height, the console scrolls and silently invalidates every
      /// previously recorded row position, which shows up as corrupted/overlapping text. Call this once
      /// the expected number of lines for the run is known, before any of those lines are written.
      /// </summary>
      public void EnsureBufferHeight(int expectedLineCount)
      {
         try
         {
            Monitor.Enter(lockObj);

            var needed = Math.Min(expectedLineCount + 20, short.MaxValue - 1);
            if (console.BufferHeight >= needed)
               return;

            console.SetBufferSize(console.BufferWidth, needed);

            // Footer position depends on buffer height - recompute and redraw without triggering
            // a scroll (avoid WriteLine at the last row, which would itself push the buffer up by one).
            footerStatusLinePosition = console.BufferHeight - 1;
            footerSeparatorLinePosition = console.BufferHeight - 2;

            console.SetCursorPosition(0, footerSeparatorLinePosition);
            console.Write(new string(' ', console.BufferWidth - 1));
            console.SetCursorPosition(0, footerStatusLinePosition);
            console.Write(new string(' ', console.BufferWidth - 1));
         }
         catch (Exception ex) when (ex is IOException || ex is ArgumentOutOfRangeException || ex is System.Security.SecurityException)
         {
            // Output redirected/piped, or the host terminal doesn't support resizing - nothing we can do,
            // fall back to whatever buffer height already exists.
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
      private void WriteMessage(string message, MessageType msgType = MessageType.Info)
      {
         try
         {
            Monitor.Enter(lockObj);
            var maxLine = GetMaxLineWriterLine();
            switch (msgType)
            {
               case MessageType.Highlight:
                  console.ForegroundColor = ConsoleColor.Cyan;
                  break;
               case MessageType.Warning:
                  console.ForegroundColor = ConsoleColor.Yellow;
                  break;
               case MessageType.Error:
                  console.ForegroundColor = ConsoleColor.Red;
                  break;
               case MessageType.Info:
               default:
                  console.ResetColor();
                  break;
            }
            if (maxLine > 0)
            {
               console.SetCursorPosition(0, maxLine);
            }
            console.WriteLine(message);
            console.ResetColor();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
      public int GetMaxLineWriterLine()
      {
         if (lineWriters.Count > 0)
         {
            return lineWriters.Max(l => l.LinePosition);
         }
         else
         {
            return -1;
         }

      }
      public void ClearLineWriters()
      {
         lineWriters.Clear();
      }
      public void Warning(string message)
      {
         WriteMessage(message, MessageType.Warning);
         log.LogWarning(message);
      }
      public void Highlight(string message)
      {
         WriteMessage(message, MessageType.Highlight);
         log.LogInformation(message);
      }
      public void Info(string message)
      {
         WriteMessage(message);
         log.LogInformation(message);
      }
      public void Error(string message)
      {
         WriteMessage(message, MessageType.Error);
         log.LogError(message);
      }
      public LineWriter GetLineWriter()
      {

         LineWriter lw;
         try
         {
            Monitor.Enter(lockObj);
            if (console.BufferHeight - 3 == console.CursorTop)
            {
               // A WriteLine() below would push the cursor onto the footer's separator row
               // (i.e. a scroll is about to happen). Every previously recorded absolute row -
               // including the footer's own two rows - shifts up by one when that happens, so
               // keep footerStatusLinePosition/footerSeparatorLinePosition in sync with the
               // lineWriters we're compensating below. Missing this was the cause of the footer
               // "status bar" drifting off its row and reappearing as a fresh line on every
               // update once a run had enough rows to reach the bottom of the buffer.
               lineWriters.ForEach(l => l.LinePosition--);
               footerSeparatorLinePosition--;
               footerStatusLinePosition--;
            }

            console.WriteLine();
            (_, int linePosition) = console.GetCursorPosition();
            // Don't use the footer buffer (last 2 lines)
            if (linePosition >= console.BufferHeight - 2)
            {
               linePosition = console.BufferHeight - 3;
            }
            lw = new LineWriter(linePosition);
            lineWriters.Add(lw);
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
         return lw;
      }
      public void Write(LineWriter lw, string message)
      {
         try
         {
            if (lw.LinePosition < 0) lw.LinePosition = 0;
            Monitor.Enter(lockObj);
            console.SetCursorPosition(0, lw.LinePosition);
            console.Write(message);
            lw.InitialMessage = message;
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
      internal void Update(LineWriter lw, string message, MessageType msgType)
      {
         try
         {
            Monitor.Enter(lockObj);
            if(lw.LinePosition < 0) lw.LinePosition = 0;
            console.SetCursorPosition(0, lw.LinePosition);
            if (message.Length > lw.InitialStatusLength)
            {
               lw.InitialStatusLength = message.Length;
            }
            console.Write($"{lw.InitialMessage}  ");
            switch (msgType)
            {
               case MessageType.Highlight:
                  console.ForegroundColor = ConsoleColor.Cyan;
                  break;
               case MessageType.Warning:
                  console.ForegroundColor = ConsoleColor.Yellow;
                  break;
               case MessageType.Error:
                  console.ForegroundColor = ConsoleColor.Red;
                  break;
               case MessageType.Final:
                  console.ForegroundColor = ConsoleColor.Green;
                  break;
               case MessageType.Initial:
                  console.ForegroundColor = ConsoleColor.Blue;
                  break;
               case MessageType.Info:
               default:
                  console.ResetColor();
                  break;
            }

            console.Write(message.PadRight(lw.InitialStatusLength));
            console.ResetColor();
            log.LogInformation($"{lw.InitialMessage}  {message}");
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
      public void Update(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Initial);
      }
      public void UpdateFinal(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Final);
         //this.lineWriters.Remove(lw);
      }
      public void UpdateError(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Error);
      }
      public void UpdateWarning(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Warning);
      }

      public void UpdateFooterStatus(string message)
      {
         try
         {
            Monitor.Enter(lockObj);
            if (footerStatusLinePosition >= 0)
            {
               // Update separator line (blank)
               console.SetCursorPosition(0, footerSeparatorLinePosition);
               console.Write("".PadRight(console.WindowWidth - 1));

               // Update status line
               console.SetCursorPosition(0, footerStatusLinePosition);
               console.ForegroundColor = ConsoleColor.Cyan;
               console.Write(message.PadRight(console.WindowWidth - 1));
               console.ResetColor();
               log.LogInformation($"Status: {message}");
            }
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

   }
}
