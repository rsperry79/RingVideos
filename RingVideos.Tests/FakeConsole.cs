using RingVideos.Writers;
using System;
using System.Collections.Generic;

namespace RingVideos.Tests;

/// <summary>
/// In-memory IConsole double. Tracks cursor position and, critically, mimics the one
/// real-console behavior ConsoleWriter's scroll-compensation logic depends on: once
/// WriteLine() is called while the cursor sits on the last row, the cursor does not
/// advance past that row (a real terminal scrolls its content instead).
/// Also records every write, keyed by the row it landed on, so tests can assert what
/// ended up on which line - the thing an integration test against a real terminal
/// can't easily do.
/// </summary>
public class FakeConsole : IConsole
{
    public int BufferHeight { get; set; } = 30;
    public int BufferWidth { get; set; } = 120;
    public int WindowWidth { get; set; } = 120;
    public int CursorTop { get; private set; } = 0;
    public int CursorLeft { get; private set; } = 0;
    public ConsoleColor ForegroundColor { get; set; } = ConsoleColor.Gray;

    /// <summary>Last text written to each row, keyed by absolute row index.</summary>
    public Dictionary<int, string> RowContents { get; } = new();

    public void SetCursorPosition(int left, int top)
    {
        CursorLeft = left;
        CursorTop = top;
    }

    public (int Left, int Top) GetCursorPosition() => (CursorLeft, CursorTop);

    public void Write(string value)
    {
        value ??= "";
        var existing = RowContents.TryGetValue(CursorTop, out var row) ? row : "";
        var chars = existing.PadRight(CursorLeft + value.Length).ToCharArray();
        for (int i = 0; i < value.Length; i++)
        {
            chars[CursorLeft + i] = value[i];
        }
        RowContents[CursorTop] = new string(chars);
        CursorLeft += value.Length;
    }

    public void WriteLine(string value = "")
    {
        if (!string.IsNullOrEmpty(value))
        {
            Write(value);
        }

        if (CursorTop >= BufferHeight - 1)
        {
            // Real console: already at the last row, so this scrolls instead of advancing.
            return;
        }

        CursorTop++;
        CursorLeft = 0;
    }

    public void ResetColor()
    {
        ForegroundColor = ConsoleColor.Gray;
    }

    public void SetBufferSize(int width, int height)
    {
        BufferWidth = width;
        BufferHeight = height;
    }
}
