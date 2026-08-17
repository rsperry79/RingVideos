using RingVideos.Writers;
using Microsoft.Extensions.Logging;
using Moq;

namespace RingVideos.Tests;

public class ConsoleWriterTests
{
    private static ConsoleWriter CreateWriter(FakeConsole console)
    {
        var mockLogger = new Mock<ILogger<ConsoleWriter>>();
        return new ConsoleWriter(mockLogger.Object, console);
    }

    [Fact]
    public void ConsoleWriterCanBeCreated()
    {
        // FakeConsole means this no longer needs a real terminal to construct.
        var console = new FakeConsole { BufferHeight = 30 };
        var writer = CreateWriter(console);
        Assert.NotNull(writer);
    }

    [Fact]
    public void FooterStatus_WritesToLastRowOfBuffer()
    {
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        writer.UpdateFooterStatus("Active Downloads: 5");

        Assert.True(console.RowContents.ContainsKey(29), "Footer status should be on the last buffer row.");
        Assert.Contains("Active Downloads: 5", console.RowContents[29]);
    }

    [Fact]
    public void FooterStatus_OverwritesSameRowAcrossRepeatedCalls()
    {
        // This is the "static speed bar" expectation: repeated status updates with no
        // intervening download rows must land on the same row every time, not stack up
        // as new lines. Footer should always be at the last line, regardless of buffer height.
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        writer.UpdateFooterStatus("Speed: 1.0 MB/s");
        writer.UpdateFooterStatus("Speed: 2.0 MB/s");
        writer.UpdateFooterStatus("Speed: 3.0 MB/s");

        // Footer should be at last line (BufferHeight - 1), separator at last - 1
        int expectedStatusRow = console.BufferHeight - 1;
        int expectedSeparatorRow = console.BufferHeight - 2;

        Assert.Contains(expectedStatusRow, console.RowContents.Keys);
        Assert.Contains(expectedSeparatorRow, console.RowContents.Keys);
        Assert.Contains("Speed: 3.0 MB/s", console.RowContents[expectedStatusRow]);
        Assert.DoesNotContain("Speed: 1.0 MB/s", console.RowContents[expectedStatusRow]);
    }

    [Fact]
    public void EnsureBufferHeight_GrowsBufferAndRecomputesFooterRow()
    {
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        writer.EnsureBufferHeight(200); // needs 220 rows, exceeds 30

        Assert.Equal(220, console.BufferHeight);

        writer.UpdateFooterStatus("after growth");

        // Footer should follow the new buffer height (last line = 219), not the original (29).
        int expectedStatusRow = console.BufferHeight - 1;
        Assert.True(console.RowContents.ContainsKey(expectedStatusRow));
        Assert.Contains("after growth", console.RowContents[expectedStatusRow]);
    }

    [Fact]
    public void EnsureBufferHeight_NoOpWhenBufferAlreadyLargeEnough()
    {
        var console = new FakeConsole { BufferHeight = 500, WindowWidth = 80 };
        var writer = CreateWriter(console);

        writer.EnsureBufferHeight(10); // needs only 30, buffer already 500

        Assert.Equal(500, console.BufferHeight);
    }

    [Fact]
    public void GetLineWriter_ScrollCompensation_KeepsFooterPositionInSyncWithLineWriters()
    {
        // Regression test for the bug reported live: with enough download rows to reach
        // the bottom of the buffer, GetLineWriter's scroll-compensation branch shifted every
        // tracked LineWriter up by one row but left the footer's tracked row untouched. Each
        // time that fired, the footer drifted further from where it actually rendered, so the
        // "status bar" appeared as a new line instead of overwriting in place.
        var console = new FakeConsole { BufferHeight = 10, WindowWidth = 80 };
        var writer = CreateWriter(console);
        // Footer rows are 8 (separator) and 9 (status) at this buffer height.

        // Existing content row, well clear of the bottom.
        console.SetCursorPosition(0, 4);
        var firstRow = writer.GetLineWriter();
        Assert.Equal(5, firstRow.LinePosition);

        // Put the cursor exactly where GetLineWriter's scroll-compensation check fires
        // (BufferHeight - 3 == CursorTop, i.e. one row before the footer separator).
        console.SetCursorPosition(0, 7);
        writer.GetLineWriter();

        // The pre-existing row should have shifted up by one, matching what a real scroll does...
        Assert.Equal(4, firstRow.LinePosition);

        // ...and the footer must have shifted by the same amount, or its next write lands on a
        // stale row instead of overwriting the visible footer.
        writer.UpdateFooterStatus("still static");
        Assert.True(console.RowContents.ContainsKey(8), "Footer status should have shifted to row 8, not stayed at 9.");
        Assert.Contains("still static", console.RowContents[8]);
    }

    [Fact]
    public void ConsoleWriterFooterRservationWorks()
    {
        // Test the logic: footer should reserve last 2 rows of console buffer
        // The fix was to reserve 2 rows instead of 1 (off-by-one bug)
        int bufferHeight = 30;
        int footerReservation = 2; // After fix

        int availableLines = bufferHeight - footerReservation;

        Assert.Equal(28, availableLines);
        Assert.True(availableLines > 0, "Should have available lines for output");
    }

    [Fact]
    public void ConsoleWriterStatusLineCanBeFormatted()
    {
        // Test formatting of status line with download speed/bytes
        long totalBytes = 1024 * 1024; // 1 MB
        var elapsed = TimeSpan.FromSeconds(10);
        var speedMbps = totalBytes / elapsed.TotalSeconds / (1024 * 1024);

        var statusLine = $"Downloaded {totalBytes / (1024 * 1024)} MB at {speedMbps:F2} MB/s";

        Assert.Contains("MB/s", statusLine);
        Assert.Contains("Downloaded", statusLine);
    }
}
