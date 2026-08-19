using System;

namespace Ring.Videos.Writers
{
    /// <summary>
    /// Thin seam over the members of System.Console that ConsoleWriter uses, so its
    /// cursor-positioning logic can be exercised with a fake in unit tests instead of
    /// requiring a real terminal.
    ///
    /// MoveCursorUp/MoveCursorDown/CarriageReturn/ClearToEndOfLine are RELATIVE movements
    /// (ANSI cursor-up/down, \r, and erase-to-end-of-line under the real implementation) -
    /// deliberately not implemented via CursorTop/GetCursorPosition/SetCursorPosition.
    /// Querying cursor position on Windows requires a round trip through ConPTY when running
    /// under Windows Terminal (a Device Status Report query sent to the terminal, then a
    /// blocking read of its response) - unlike a plain write, that's neither instant nor
    /// guaranteed to reflect what's actually been rendered yet, and was the root cause of a
    /// display corruption bug (rows drifting apart from what ConsoleWriter believed their
    /// position was). Pure relative movement needs no such round trip: ConsoleWriter tracks
    /// its own notion of "how tall is the region I last drew" and moves purely relative to
    /// wherever the cursor already is, which is reliable everywhere VT sequences work.
    /// CursorTop/GetCursorPosition/SetCursorPosition are kept for compatibility but should
    /// not be used for anything position-critical.
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

        /// <summary>Moves the cursor up by the given number of rows without changing its column. No-op for rows &lt;= 0.</summary>
        void MoveCursorUp(int rows);
        /// <summary>Moves the cursor down by the given number of rows without changing its column. No-op for rows &lt;= 0. Only safe within rows that already exist on screen - unlike WriteLine, this does not create new scrollback content.</summary>
        void MoveCursorDown(int rows);
        /// <summary>Moves the cursor to column 0 of its current row.</summary>
        void CarriageReturn();
        /// <summary>Erases from the current cursor column to the end of the current row, without moving the cursor.</summary>
        void ClearToEndOfLine();
    }
}
