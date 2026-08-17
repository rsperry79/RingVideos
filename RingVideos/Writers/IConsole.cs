using System;

namespace RingVideos.Writers
{
   /// <summary>
   /// Thin seam over the members of System.Console that ConsoleWriter uses, so its
   /// cursor-positioning/scroll-compensation logic can be exercised with a fake in
   /// unit tests instead of requiring a real terminal.
   /// </summary>
   public interface IConsole
   {
      int BufferHeight { get; }
      int BufferWidth { get; }
      int WindowWidth { get; }
      int CursorTop { get; }
      ConsoleColor ForegroundColor { get; set; }

      void SetCursorPosition(int left, int top);
      (int Left, int Top) GetCursorPosition();
      void Write(string value);
      void WriteLine(string value = "");
      void ResetColor();
      void SetBufferSize(int width, int height);
   }
}
