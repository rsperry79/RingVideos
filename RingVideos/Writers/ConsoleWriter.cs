using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;

namespace RingVideos.Writers
{
   /// <summary>
   /// Renders a small "pinned" region - up to N live download rows, a separator, and a
   /// footer status line - that always stays at the bottom of the terminal, with general
   /// log messages scrolling normally above it.
   ///
   /// Why this shape: an earlier version gave every download item its own permanent
   /// absolute buffer row and tried to keep the footer's absolute row in sync by detecting
   /// and compensating for buffer scrolls (via Console.BufferHeight arithmetic). That broke
   /// down on real terminals - particularly Windows Terminal/ConPTY - where BufferHeight
   /// does not reliably describe a stable coordinate space that tracks the visible viewport.
   /// Trying to reason about "the buffer scrolled N times" from BufferHeight alone silently
   /// desynced item/footer rows from what was actually on screen, which is what caused the
   /// footer status to appear to pile up as new lines instead of overwriting in place.
   ///
   /// Instead, every write here is relative to the cursor's ACTUAL, freshly-queried position
   /// (Console.CursorTop), never a cached/assumed absolute row. The class enforces one
   /// invariant: after any public method returns, the cursor is parked at column 0 of the
   /// region's last line (the footer). Updating a single row is a small in-place jump (up N
   /// rows, write, back down); inserting new scrolling log content is an erase-region /
   /// write-line / redraw-region sequence. Because the region and the log both move via the
   /// same natural relative cursor motion, they can never drift apart the way independently
   /// computed absolute rows could.
   ///
   /// The active region is deliberately small and fixed (default 10, matching the app's
   /// download concurrency semaphore) rather than one row per item - a handful of rows
   /// always fits inside any real terminal window, so no buffer growth is ever required.
   /// Items that finish are expected to call <see cref="ReleaseLineWriter"/>, which prints
   /// their final line into the scrolling log and frees the slot for reuse.
   /// </summary>
   public class ConsoleWriter
   {
      private readonly ILogger<ConsoleWriter> log;
      private readonly IConsole console;
      private readonly object lockObj = new object();
      private readonly int maxActiveSlots;
      private readonly LineWriter[] slots;
      private string footerText = "";

      public ConsoleWriter(ILogger<ConsoleWriter> log, int maxActiveSlots = 10) : this(log, new SystemConsole(), maxActiveSlots)
      {
      }

      public ConsoleWriter(ILogger<ConsoleWriter> log, IConsole console, int maxActiveSlots = 10)
      {
         this.log = log;
         this.console = console;
         this.maxActiveSlots = Math.Max(1, maxActiveSlots);
         this.slots = new LineWriter[this.maxActiveSlots];
         InitializeRegion();
      }

      private int RegionHeight => maxActiveSlots + 2; // active rows + separator + footer

      private void InitializeRegion()
      {
         try
         {
            Monitor.Enter(lockObj);
            RedrawRegion();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      /// <summary>
      /// Historically grew the console buffer to fit one absolute row per download. The
      /// pinned-region design no longer needs that - the live region is always a small,
      /// fixed number of rows - so this is now just a defensive minimum-size check kept for
      /// API compatibility with existing call sites.
      /// </summary>
      public void EnsureBufferHeight(int expectedLineCount)
      {
         try
         {
            Monitor.Enter(lockObj);
            var minNeeded = RegionHeight + 5;
            if (console.BufferHeight >= minNeeded)
               return;

            console.SetBufferSize(console.BufferWidth, minNeeded);
         }
         catch (Exception ex) when (ex is IOException || ex is ArgumentOutOfRangeException || ex is System.Security.SecurityException)
         {
            // Output redirected/piped, or the host terminal doesn't support resizing - nothing we can do.
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      /// <summary>Clears every row of the pinned region, leaving the cursor parked at the region's top-left.</summary>
      private void EraseRegionAndMoveToTop()
      {
         int bottom = console.CursorTop;
         int top = Math.Max(0, bottom - (RegionHeight - 1));
         console.SetCursorPosition(0, top);
         for (int i = 0; i < RegionHeight; i++)
         {
            console.Write(new string(' ', Math.Max(0, console.WindowWidth - 1)));
            if (i < RegionHeight - 1)
            {
               console.WriteLine();
            }
            else
            {
               console.SetCursorPosition(0, top);
            }
         }
      }

      /// <summary>Redraws every active slot, the separator, and the footer, assuming the cursor is at the region's top-left. Leaves the cursor parked at column 0 of the footer row.</summary>
      private void RedrawRegion()
      {
         for (int i = 0; i < maxActiveSlots; i++)
         {
            var lw = slots[i];
            console.Write((lw?.RenderedLine ?? string.Empty).PadRight(Math.Max(0, console.WindowWidth - 1)));
            console.WriteLine();
         }
         console.WriteLine(); // separator (blank)

         int footerRow = console.CursorTop;
         console.Write(footerText.PadRight(Math.Max(0, console.WindowWidth - 1)));
         console.SetCursorPosition(0, footerRow);
      }

      /// <summary>Jumps up from the parked footer row to the given active slot, writes, then restores the parked position.</summary>
      private void WriteSlot(int slotIndex, string text)
      {
         int bottom = console.CursorTop;
         int rowsUpFromBottom = (RegionHeight - 1) - slotIndex;
         int targetRow = Math.Max(0, bottom - rowsUpFromBottom);
         console.SetCursorPosition(0, targetRow);
         console.Write(text.PadRight(Math.Max(0, console.WindowWidth - 1)));
         console.SetCursorPosition(0, bottom);
      }

      /// <summary>Erases the region, writes one new line into the scrolling log above it, then redraws the region.</summary>
      private void WriteLogLine(string message, MessageType msgType)
      {
         try
         {
            Monitor.Enter(lockObj);
            EraseRegionAndMoveToTop();
            SetColorFor(msgType);
            console.WriteLine(message);
            console.ResetColor();
            RedrawRegion();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      private void SetColorFor(MessageType msgType)
      {
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
      }

      public void ClearLineWriters()
      {
         try
         {
            Monitor.Enter(lockObj);
            Array.Clear(slots, 0, slots.Length);
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      public int GetMaxLineWriterLine()
      {
         try
         {
            Monitor.Enter(lockObj);
            int max = -1;
            for (int i = 0; i < slots.Length; i++)
            {
               if (slots[i] != null) max = i;
            }
            return max;
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      public void Warning(string message)
      {
         WriteLogLine(message, MessageType.Warning);
         log.LogWarning(message);
      }
      public void Highlight(string message)
      {
         WriteLogLine(message, MessageType.Highlight);
         log.LogInformation(message);
      }
      public void Info(string message)
      {
         WriteLogLine(message, MessageType.Info);
         log.LogInformation(message);
      }
      public void Error(string message)
      {
         WriteLogLine(message, MessageType.Error);
         log.LogError(message);
      }

      public LineWriter GetLineWriter()
      {
         try
         {
            Monitor.Enter(lockObj);
            int slotIndex = Array.IndexOf(slots, null);
            if (slotIndex < 0)
            {
               // Defensive only: callers are expected to bound concurrency to maxActiveSlots
               // (RingVideoApplication does this via its download semaphore). Reuse slot 0
               // rather than throw if that contract is ever violated.
               slotIndex = 0;
            }

            var lw = new LineWriter(slotIndex);
            slots[slotIndex] = lw;
            WriteSlot(slotIndex, "");
            return lw;
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      public void Write(LineWriter lw, string message)
      {
         try
         {
            Monitor.Enter(lockObj);
            lw.InitialMessage = message;
            lw.RenderedLine = message;
            WriteSlot(lw.LinePosition, message);
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
            if (message.Length > lw.InitialStatusLength)
            {
               lw.InitialStatusLength = message.Length;
            }
            lw.LastMessage = message;
            lw.LastMessageType = msgType;

            int bottom = console.CursorTop;
            int rowsUpFromBottom = (RegionHeight - 1) - lw.LinePosition;
            int targetRow = Math.Max(0, bottom - rowsUpFromBottom);
            console.SetCursorPosition(0, targetRow);
            console.Write($"{lw.InitialMessage}  ");
            SetColorFor(msgType);
            var paddedStatus = message.PadRight(lw.InitialStatusLength);
            console.Write(paddedStatus);
            console.ResetColor();
            console.SetCursorPosition(0, bottom);

            lw.RenderedLine = $"{lw.InitialMessage}  {paddedStatus}";
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
      }
      public void UpdateError(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Error);
      }
      public void UpdateWarning(LineWriter lw, string message)
      {
         Update(lw, message, MessageType.Warning);
      }

      /// <summary>
      /// Marks an item's row as finished: prints its final rendered line into the scrolling
      /// log (so it remains visible in terminal history) and frees its active slot so a new
      /// download can reuse the row. Callers should invoke this once the item is truly done
      /// (success or permanently failed), typically right where they release whatever
      /// concurrency gate they used to bound how many rows can be active at once.
      /// </summary>
      public void ReleaseLineWriter(LineWriter lw)
      {
         try
         {
            Monitor.Enter(lockObj);

            string finalText = string.IsNullOrEmpty(lw.LastMessage)
               ? lw.InitialMessage
               : $"{lw.InitialMessage}  {lw.LastMessage}";
            var msgType = lw.LastMessageType ?? MessageType.Info;

            if (lw.LinePosition >= 0 && lw.LinePosition < slots.Length && ReferenceEquals(slots[lw.LinePosition], lw))
            {
               slots[lw.LinePosition] = null;
            }

            EraseRegionAndMoveToTop();
            SetColorFor(msgType);
            console.WriteLine(finalText);
            console.ResetColor();
            RedrawRegion();

            log.LogInformation(finalText);
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      public void UpdateFooterStatus(string message)
      {
         try
         {
            Monitor.Enter(lockObj);
            footerText = message ?? string.Empty;

            // Cursor is parked at column 0 of the footer row per the class invariant - just
            // overwrite it in place, no absolute-row lookup needed.
            int footerRow = console.CursorTop;
            console.SetCursorPosition(0, footerRow);
            console.ForegroundColor = ConsoleColor.Cyan;
            console.Write(footerText.PadRight(Math.Max(0, console.WindowWidth - 1)));
            console.ResetColor();
            console.SetCursorPosition(0, footerRow);

            log.LogInformation($"Status: {message}");
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
   }
}
