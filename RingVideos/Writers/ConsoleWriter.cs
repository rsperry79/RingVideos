using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;

namespace RingVideos.Writers
{
   /// <summary>
   /// Renders a small "pinned" region - a handful of live download rows, a separator, and a
   /// footer status line - that always stays at the bottom of the terminal, with general log
   /// messages scrolling normally above it.
   ///
   /// This has gone through two prior designs, both of which broke down on real terminals:
   ///
   /// 1. Every download item got its own permanent absolute buffer row, with the footer's
   ///    row cached from Console.BufferHeight and "compensated" when a scroll was detected.
   ///    Windows Terminal/ConPTY doesn't honor Console.SetBufferSize or reliably tie
   ///    BufferHeight to the visible viewport, so rows silently drifted from what was
   ///    actually on screen.
   ///
   /// 2. A fixed/dynamic-height region addressed via Console.SetCursorPosition, using
   ///    Console.CursorTop queries to find "where am I right now" before every move. This
   ///    fixed the drift, but introduced gaps between consecutive log lines: on Windows
   ///    Terminal, CursorTop/GetCursorPosition require a round trip through ConPTY (a Device
   ///    Status Report query sent to the terminal, then a blocking read of the response) -
   ///    interleaved with rapid writes, that round trip is neither instant nor guaranteed to
   ///    reflect what has actually rendered yet, so the computed position could be stale.
   ///
   /// The fix: never query cursor position at all. Every move here is a RELATIVE ANSI
   /// sequence (cursor up/down N rows, carriage return, erase-to-end-of-line) applied on top
   /// of ConsoleWriter's own tracked notion of "how tall is the region I last drew"
   /// (lastDrawnRegionHeight). Relative moves need no response from the terminal - they're
   /// fire-and-forget writes, just like printing text. The class enforces one invariant:
   /// after any public method returns, the cursor is at column 0 of the region's last line
   /// (the footer). Updating a single row is a small relative jump (up N, write, back down
   /// N) entirely within already-drawn rows; inserting new scrolling log content moves up to
   /// the region's top, overwrites it with the new line via a real newline (preserving
   /// scrollback), then redraws the region below it.
   ///
   /// The active region is deliberately sized to actual occupancy (not a constant): with no
   /// downloads active it's just separator+footer (2 rows), growing only as slots fill in up
   /// to maxActiveSlots (default 10, matching the app's download concurrency semaphore) and
   /// shrinking again as items are released. This keeps it small enough to always fit inside
   /// any real terminal window, and avoids a fixed height forcing a wall of blank rows to be
   /// redrawn under every log line before any downloads start.
   ///
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
      // Height (in rows) of the pinned region as it was last actually drawn on screen - the
      // sole source of truth for where things are, since we never query the terminal for
      // cursor position. Recomputed by RedrawRegionRows from current slot occupancy every
      // time it runs.
      private int lastDrawnRegionHeight;

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

      private int HighestOccupiedSlotIndex()
      {
         int highest = -1;
         for (int i = 0; i < slots.Length; i++)
         {
            if (slots[i] != null) highest = i;
         }
         return highest;
      }

      private void InitializeRegion()
      {
         try
         {
            Monitor.Enter(lockObj);
            RedrawRegionRows();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      /// <summary>
      /// Historically grew the console buffer to fit one absolute row per download. Neither
      /// that nor the buffer height matters to this design anymore - every move is relative -
      /// so this is now just a defensive minimum-size best-effort call, kept for API
      /// compatibility with existing call sites.
      /// </summary>
      public void EnsureBufferHeight(int expectedLineCount)
      {
         try
         {
            Monitor.Enter(lockObj);
            var minNeeded = maxActiveSlots + 2 + 5;
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

      /// <summary>
      /// Redraws active slots up to the highest occupied one, the separator, and the footer,
      /// assuming the cursor is already at the region's top-left (either nothing has been
      /// drawn yet, or a caller just moved there). Each row is cleared to end-of-line before
      /// being written so shrinking content never leaves stale trailing characters. Leaves
      /// the cursor parked at column 0 of the footer row.
      /// </summary>
      private void RedrawRegionRows()
      {
         int activeRows = HighestOccupiedSlotIndex() + 1;
         for (int i = 0; i < activeRows; i++)
         {
            var lw = slots[i];
            console.ClearToEndOfLine();
            console.Write(lw?.RenderedLine ?? string.Empty);
            console.WriteLine();
         }

         console.ClearToEndOfLine();
         console.WriteLine(); // separator (blank)

         console.ClearToEndOfLine();
         console.Write(footerText);
         console.CarriageReturn();

         lastDrawnRegionHeight = activeRows + 2;
      }

      /// <summary>Moves from the parked footer row up to the current region's top-left, ready for RedrawRegionRows.</summary>
      private void MoveToRegionTop()
      {
         int height = Math.Max(2, lastDrawnRegionHeight);
         console.MoveCursorUp(height - 1);
         console.CarriageReturn();
      }

      /// <summary>Grows/redraws the region in place - used when a newly assigned slot falls outside what's currently drawn.</summary>
      private void RedrawInPlace()
      {
         MoveToRegionTop();
         RedrawRegionRows();
      }

      /// <summary>Moves up to the region's top, overwrites it with one new line of scrolling log content (preserved via a real newline), then redraws the region below it.</summary>
      private void InsertLogLineAndRedraw(string message, MessageType msgType)
      {
         try
         {
            Monitor.Enter(lockObj);
            MoveToRegionTop();
            console.ClearToEndOfLine();
            SetColorFor(msgType);
            console.WriteLine(message);
            console.ResetColor();
            RedrawRegionRows();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      /// <summary>Jumps up from the parked footer row to the given active slot (which must already be within the currently drawn region), writes, then jumps back down. Both moves stay within already-drawn rows, so no new scrollback content is created.</summary>
      private void WriteSlot(int slotIndex, string text)
      {
         int rowsUp = (lastDrawnRegionHeight - 1) - slotIndex;
         console.MoveCursorUp(rowsUp);
         console.CarriageReturn();
         console.ClearToEndOfLine();
         console.Write(text);
         console.CarriageReturn();
         console.MoveCursorDown(rowsUp);
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
            return HighestOccupiedSlotIndex();
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }

      public void Warning(string message)
      {
         InsertLogLineAndRedraw(message, MessageType.Warning);
         log.LogWarning(message);
      }
      public void Highlight(string message)
      {
         InsertLogLineAndRedraw(message, MessageType.Highlight);
         log.LogInformation(message);
      }
      public void Info(string message)
      {
         InsertLogLineAndRedraw(message, MessageType.Info);
         log.LogInformation(message);
      }
      public void Error(string message)
      {
         InsertLogLineAndRedraw(message, MessageType.Error);
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

            int neededHeight = slotIndex + 1 + 2;
            if (neededHeight > lastDrawnRegionHeight)
            {
               RedrawInPlace();
            }
            else
            {
               WriteSlot(slotIndex, "");
            }
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
            lw.LastMessage = message;
            lw.LastMessageType = msgType;

            int rowsUp = (lastDrawnRegionHeight - 1) - lw.LinePosition;
            console.MoveCursorUp(rowsUp);
            console.CarriageReturn();
            console.ClearToEndOfLine();
            console.Write($"{lw.InitialMessage}  ");
            SetColorFor(msgType);
            console.Write(message);
            console.ResetColor();
            console.CarriageReturn();
            console.MoveCursorDown(rowsUp);

            lw.RenderedLine = $"{lw.InitialMessage}  {message}";
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

            MoveToRegionTop();
            console.ClearToEndOfLine();
            SetColorFor(msgType);
            console.WriteLine(finalText);
            console.ResetColor();
            RedrawRegionRows();

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
            // overwrite it in place, no movement needed.
            console.ClearToEndOfLine();
            console.ForegroundColor = ConsoleColor.Cyan;
            console.Write(footerText);
            console.ResetColor();
            console.CarriageReturn();

            log.LogInformation($"Status: {message}");
         }
         finally
         {
            Monitor.Exit(lockObj);
         }
      }
   }
}
