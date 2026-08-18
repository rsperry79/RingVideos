using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RingVideos.Writers
{
   /// <summary>
   /// Production IConsole implementation - delegates straight through to System.Console.
   /// Relative cursor movement is emitted as raw ANSI/VT escape sequences via Console.Write,
   /// rather than via Console.SetCursorPosition/CursorTop, deliberately avoiding any cursor
   /// position query (see IConsole's remarks for why).
   /// </summary>
   public class SystemConsole : IConsole
   {
      public SystemConsole()
      {
         TryEnableVirtualTerminalProcessing();
      }

      public int BufferHeight => Console.BufferHeight;
      public int BufferWidth => Console.BufferWidth;
      public int WindowWidth => Console.WindowWidth;
      public int CursorTop => Console.CursorTop;
      public ConsoleColor ForegroundColor
      {
         get => Console.ForegroundColor;
         set => Console.ForegroundColor = value;
      }

      public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);
      public (int Left, int Top) GetCursorPosition() => Console.GetCursorPosition();
      public void Write(string value) => Console.Write(value);
      public void WriteLine(string value = "") => Console.WriteLine(value);
      public void ResetColor() => Console.ResetColor();
      public void SetBufferSize(int width, int height) => Console.SetBufferSize(width, height);

      public void MoveCursorUp(int rows)
      {
         if (rows > 0) Console.Write($"\x1b[{rows}A");
      }
      public void MoveCursorDown(int rows)
      {
         if (rows > 0) Console.Write($"\x1b[{rows}B");
      }
      public void CarriageReturn() => Console.Write("\r");
      public void ClearToEndOfLine() => Console.Write("\x1b[0K");

      /// <summary>
      /// Windows Terminal/ConPTY interprets raw ANSI escape sequences natively. Legacy
      /// conhost.exe does not, unless ENABLE_VIRTUAL_TERMINAL_PROCESSING is set on the
      /// output handle - without it, the sequences above would render as literal garbage
      /// text instead of moving the cursor. Best-effort only: failures (redirected output,
      /// non-Windows, unsupported host) are swallowed, since there's nothing else to do.
      /// </summary>
      [SupportedOSPlatform("windows")]
      private static void TryEnableVirtualTerminalProcessing()
      {
         if (!OperatingSystem.IsWindows()) return;
         try
         {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
            if (!GetConsoleMode(handle, out uint mode)) return;
            SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
         }
         catch
         {
            // Nothing else we can do here.
         }
      }

      private const int STD_OUTPUT_HANDLE = -11;
      private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern IntPtr GetStdHandle(int nStdHandle);

      [DllImport("kernel32.dll")]
      private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

      [DllImport("kernel32.dll")]
      private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
   }
}
