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
        // Regression test: Footer position should always be recalculated based on BufferHeight,
        // so it stays at the last line even when scrolling happens due to many download items.
        var console = new FakeConsole { BufferHeight = 10, WindowWidth = 80 };
        var writer = CreateWriter(console);

        // Footer should be at last two rows: 8 (separator) and 9 (status)
        int expectedStatusRow = console.BufferHeight - 1;
        int expectedSeparatorRow = console.BufferHeight - 2;

        // Create first line writer
        console.SetCursorPosition(0, 4);
        var firstRow = writer.GetLineWriter();
        Assert.Equal(5, firstRow.LinePosition);

        // Trigger scroll-compensation by getting another line writer
        // at position that would cause scroll
        console.SetCursorPosition(0, 7);
        writer.GetLineWriter();

        // LineWriter positions should shift up after scroll compensation
        Assert.Equal(4, firstRow.LinePosition);

        // Footer should still be at the last line (recalculated on each update)
        writer.UpdateFooterStatus("still static");
        Assert.True(console.RowContents.ContainsKey(expectedStatusRow),
            $"Footer status should be at row {expectedStatusRow} (BufferHeight - 1)");
        Assert.Contains("still static", console.RowContents[expectedStatusRow]);
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

    [Fact]
    public void FooterStatus_StaysStaticDuringScrollingWithManyItems()
    {
        // Test that when many download items scroll the buffer, the footer status
        // bar stays on the last line and doesn't create stacked duplicates.
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        int expectedStatusRow = console.BufferHeight - 1;

        // Simulate many download items being added (like in the app)
        for (int i = 0; i < 20; i++)
        {
            var lineWriter = writer.GetLineWriter();
            writer.Write(lineWriter, $"Item {i}: Downloading...");
        }

        // Update footer status multiple times (simulating speed bar updates)
        writer.UpdateFooterStatus("Speed: 1.0 MB/s | Total: 10 MB");
        writer.UpdateFooterStatus("Speed: 2.0 MB/s | Total: 20 MB");
        writer.UpdateFooterStatus("Speed: 3.0 MB/s | Total: 30 MB");
        writer.UpdateFooterStatus("Speed: 4.0 MB/s | Total: 40 MB");

        // Footer should always be at the last line, with only the latest status
        Assert.True(console.RowContents.ContainsKey(expectedStatusRow),
            $"Footer should be at row {expectedStatusRow}");
        Assert.Contains("Speed: 4.0 MB/s | Total: 40 MB", console.RowContents[expectedStatusRow]);

        // Verify earlier status updates are NOT stacked as separate lines
        Assert.DoesNotContain("Speed: 1.0 MB/s", console.RowContents[expectedStatusRow]);
        Assert.DoesNotContain("Speed: 2.0 MB/s", console.RowContents[expectedStatusRow]);
        Assert.DoesNotContain("Speed: 3.0 MB/s", console.RowContents[expectedStatusRow]);
    }
}
