using System;

namespace RingVideos.Writers
{
   /// <summary>
   /// Production IConsole implementation - delegates straight through to System.Console.
   /// </summary>
   public class SystemConsole : IConsole
   {
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
   }
}
