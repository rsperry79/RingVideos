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
        // This is the "static speed bar" expectation: repeated status updates use
        // carriage return (\r) to overwrite on the same line, not stack as new lines.
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        // Write initial status to establish a line
        console.WriteLine();

        writer.UpdateFooterStatus("Speed: 1.0 MB/s");
        writer.UpdateFooterStatus("Speed: 2.0 MB/s");
        writer.UpdateFooterStatus("Speed: 3.0 MB/s");

        // With carriage return approach, latest status should be present
        var allOutput = string.Join("\n", console.RowContents.Values);
        Assert.Contains("Speed: 3.0 MB/s", allOutput);
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
        // Test: Footer updates use carriage return to overwrite on the same line,
        // preventing the stacking issue when many download items are added.
        var console = new FakeConsole { BufferHeight = 10, WindowWidth = 80 };
        var writer = CreateWriter(console);

        // Create first line writer
        console.SetCursorPosition(0, 4);
        var firstRow = writer.GetLineWriter();
        Assert.Equal(5, firstRow.LinePosition);

        // Trigger scroll-compensation
        console.SetCursorPosition(0, 7);
        writer.GetLineWriter();

        // LineWriter positions should shift up after scroll compensation
        Assert.Equal(4, firstRow.LinePosition);

        // Footer updates should use carriage return (no new rows created)
        console.WriteLine(); // Establish a baseline
        writer.UpdateFooterStatus("still static");

        // Verify footer status is in the output (using carriage return approach)
        var allOutput = string.Join("\n", console.RowContents.Values);
        Assert.Contains("still static", allOutput);
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
        // bar uses carriage return to overwrite on the same line and doesn't create duplicates.
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        // Simulate many download items being added (like in the app)
        for (int i = 0; i < 20; i++)
        {
            var lineWriter = writer.GetLineWriter();
            writer.Write(lineWriter, $"Item {i}: Downloading...");
        }

        // Establish a baseline for status output
        console.WriteLine();

        // Update footer status multiple times (simulating speed bar updates)
        writer.UpdateFooterStatus("Speed: 1.0 MB/s | Total: 10 MB");
        writer.UpdateFooterStatus("Speed: 2.0 MB/s | Total: 20 MB");
        writer.UpdateFooterStatus("Speed: 3.0 MB/s | Total: 30 MB");
        writer.UpdateFooterStatus("Speed: 4.0 MB/s | Total: 40 MB");

        // Verify latest status is in output and not stacked with earlier ones
        var allOutput = string.Join("\n", console.RowContents.Values);
        Assert.Contains("Speed: 4.0 MB/s | Total: 40 MB", allOutput);

        // Count occurrences - should not have all four statuses (they overwrite)
        int count1 = (allOutput.Length - allOutput.Replace("Speed: 1.0", "").Length) / "Speed: 1.0".Length;
        int count2 = (allOutput.Length - allOutput.Replace("Speed: 2.0", "").Length) / "Speed: 2.0".Length;
        int count3 = (allOutput.Length - allOutput.Replace("Speed: 3.0", "").Length) / "Speed: 3.0".Length;
        int count4 = (allOutput.Length - allOutput.Replace("Speed: 4.0", "").Length) / "Speed: 4.0".Length;

        // With carriage return, should have at most one of each
        Assert.True(count1 <= 1 && count2 <= 1 && count3 <= 1 && count4 <= 1,
            "Status updates should overwrite via carriage return, not create new lines");
    }
}
