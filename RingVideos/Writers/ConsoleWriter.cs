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
      private object lockObj = new object();
      private ThreadSafeList<LineWriter> lineWriters;
      private int footerStatusLinePosition = -1;
      private int footerSeparatorLinePosition = -1;

      public ConsoleWriter(ILogger<ConsoleWriter> log)
      {
         this.log = log;
         lineWriters = new ThreadSafeList<LineWriter>();
         InitializeFooter();
      }

      private void InitializeFooter()
      {
         try
         {
            Monitor.Enter(lockObj);
            footerStatusLinePosition = Console.BufferHeight - 1;
            footerSeparatorLinePosition = Console.BufferHeight - 2;

            // Write separator line
            Console.SetCursorPosition(0, footerSeparatorLinePosition);
            Console.WriteLine();

            // Write empty status line
            Console.SetCursorPosition(0, footerStatusLinePosition);
            Console.WriteLine();
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
            if (Console.BufferHeight >= needed)
               return;

            Console.SetBufferSize(Console.BufferWidth, needed);

            // Footer position depends on buffer height - recompute and redraw without triggering
            // a scroll (avoid WriteLine at the last row, which would itself push the buffer up by one).
            footerStatusLinePosition = Console.BufferHeight - 1;
            footerSeparatorLinePosition = Console.BufferHeight - 2;

            Console.SetCursorPosition(0, footerSeparatorLinePosition);
            Console.Write(new string(' ', Console.BufferWidth - 1));
            Console.SetCursorPosition(0, footerStatusLinePosition);
            Console.Write(new string(' ', Console.BufferWidth - 1));
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
                  Console.ForegroundColor = ConsoleColor.Cyan;
                  break;
               case MessageType.Warning:
                  Console.ForegroundColor = ConsoleColor.Yellow;
                  break;
               case MessageType.Error:
                  Console.ForegroundColor = ConsoleColor.Red;
                  break;
               case MessageType.Info:
               default:
                  Console.ResetColor();
                  break;
            }
            if (maxLine > 0)
            {
               Console.SetCursorPosition(0, maxLine);
            }
            Console.WriteLine(message);
            Console.ResetColor();
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
            if (Console.BufferHeight - 3 == Console.CursorTop)
            {
               if (Console.BufferHeight - 3 == Console.CursorTop)
               {
                  lineWriters.ForEach(l => l.LinePosition--);
               }
            }

            Console.WriteLine();
            (_, int linePosition) = Console.GetCursorPosition();
            // Don't use the footer buffer (last 2 lines)
            if (linePosition >= Console.BufferHeight - 2)
            {
               linePosition = Console.BufferHeight - 3;
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
            Console.SetCursorPosition(0, lw.LinePosition);
            Console.Write(message);
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
            //Console.SetCursorPosition(0, lw.LinePosition);
            //Console.Write(new string(' ', Console.WindowWidth));
            if(lw.LinePosition < 0) lw.LinePosition = 0;
            Console.SetCursorPosition(0, lw.LinePosition);
            if (message.Length > lw.InitialStatusLength)
            {
               lw.InitialStatusLength = message.Length;
            }
            Console.Write($"{lw.InitialMessage}  ");
            switch (msgType)
            {
               case MessageType.Highlight:
                  Console.ForegroundColor = ConsoleColor.Cyan;
                  break;
               case MessageType.Warning:
                  Console.ForegroundColor = ConsoleColor.Yellow;
                  break;
               case MessageType.Error:
                  Console.ForegroundColor = ConsoleColor.Red;
                  break;
               case MessageType.Final:
                  Console.ForegroundColor = ConsoleColor.Green;
                  break;
               case MessageType.Initial:
                  Console.ForegroundColor = ConsoleColor.Blue;
                  break;
               case MessageType.Info:
               default:
                  Console.ResetColor();
                  break;
            }

            Console.Write(message.PadRight(lw.InitialStatusLength));
            Console.ResetColor();
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
               Console.SetCursorPosition(0, footerSeparatorLinePosition);
               Console.Write("".PadRight(Console.WindowWidth - 1));

               // Update status line
               Console.SetCursorPosition(0, footerStatusLinePosition);
               Console.ForegroundColor = ConsoleColor.Cyan;
               Console.Write(message.PadRight(Console.WindowWidth - 1));
               Console.ResetColor();
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
