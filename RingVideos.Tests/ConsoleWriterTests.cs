using RingVideos.Writers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;

namespace RingVideos.Tests;

public class ConsoleWriterTests
{
    private const int DefaultMaxActiveSlots = 10;
    // RegionHeight = maxActiveSlots + 2 (separator + footer). Footer sits on the last row
    // of that region, so starting from a fresh console (cursor at row 0), the footer lands
    // on row (maxActiveSlots + 1).
    private static int ExpectedFooterRowFromStart(int maxActiveSlots = DefaultMaxActiveSlots) => maxActiveSlots + 1;

    private static ConsoleWriter CreateWriter(FakeConsole console, int maxActiveSlots = DefaultMaxActiveSlots)
    {
        var mockLogger = new Mock<ILogger<ConsoleWriter>>();
        return new ConsoleWriter(mockLogger.Object, console, maxActiveSlots);
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
    public void FooterStatus_WritesToLastRowOfRegion()
    {
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        writer.UpdateFooterStatus("Active Downloads: 5");

        int expectedRow = ExpectedFooterRowFromStart();
        Assert.True(console.RowContents.ContainsKey(expectedRow), $"Footer status should be on row {expectedRow}.");
        Assert.Contains("Active Downloads: 5", console.RowContents[expectedRow]);
    }

    [Fact]
    public void FooterStatus_OverwritesSameRowAcrossRepeatedCalls()
    {
        // The "static speed bar" expectation: repeated status updates land on the same
        // row every time - the row is recomputed relative to the cursor's actual position,
        // never a cached/assumed absolute row - so nothing ever stacks as new lines.
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console);

        writer.UpdateFooterStatus("Speed: 1.0 MB/s");
        writer.UpdateFooterStatus("Speed: 2.0 MB/s");
        writer.UpdateFooterStatus("Speed: 3.0 MB/s");

        int expectedRow = ExpectedFooterRowFromStart();
        Assert.Contains("Speed: 3.0 MB/s", console.RowContents[expectedRow]);
        Assert.DoesNotContain("Speed: 1.0 MB/s", console.RowContents[expectedRow]);
        Assert.DoesNotContain("Speed: 2.0 MB/s", console.RowContents[expectedRow]);

        // No other row should ever have received footer text.
        foreach (var kvp in console.RowContents)
        {
            if (kvp.Key != expectedRow)
            {
                Assert.DoesNotContain("MB/s", kvp.Value);
            }
        }
    }

    [Fact]
    public void GetLineWriter_AssignsDistinctSlots()
    {
        var console = new FakeConsole { BufferHeight = 30, WindowWidth = 80 };
        var writer = CreateWriter(console, maxActiveSlots: 3);

        var first = writer.GetLineWriter();
        var second = writer.GetLineWriter();
        var third = writer.GetLineWriter();

        Assert.Equal(new[] { 0, 1, 2 }, new[] { first.LinePosition, second.LinePosition, third.LinePosition }.OrderBy(x => x));
    }

    [Fact]
    public void ReleaseLineWriter_FreesSlotForReuse()
    {
        var console = new FakeConsole { BufferHeight = 200, WindowWidth = 80 };
        var writer = CreateWriter(console, maxActiveSlots: 2);

        var first = writer.GetLineWriter();
        writer.Write(first, "001) item-one");
        writer.UpdateFinal(first, "Complete");

        writer.ReleaseLineWriter(first);

        var second = writer.GetLineWriter();

        // With only 2 slots and the first released, the new writer should be able to reuse
        // a freed slot rather than colliding with the still-untouched second slot.
        Assert.True(second.LinePosition == 0 || second.LinePosition == 1);
    }

    [Fact]
    public void ReleaseLineWriter_PrintsFinalLineIntoScrollingLog()
    {
        var console = new FakeConsole { BufferHeight = 200, WindowWidth = 80 };
        var writer = CreateWriter(console, maxActiveSlots: 2);

        var lw = writer.GetLineWriter();
        writer.Write(lw, "001) my-file.mp4 :: ");
        writer.UpdateFinal(lw, "Complete - (5 MB)");

        writer.ReleaseLineWriter(lw);

        var allOutput = string.Join("\n", console.RowContents.Values);
        Assert.Contains("001) my-file.mp4 ::", allOutput);
        Assert.Contains("Complete - (5 MB)", allOutput);
    }

    [Fact]
    public void ManyItemsExceedingActiveSlots_CycleThroughWithoutCorruptingFooter()
    {
        // Regression test for the real-world bug: 714 downloads with only 10 concurrent
        // slots meant items were constantly created and released while the footer speed
        // bar updated every few seconds. This drives that same cycle - far more items than
        // active slots - and asserts the footer never drifts or duplicates.
        var console = new FakeConsole { BufferHeight = 2000, WindowWidth = 80 };
        var writer = CreateWriter(console, maxActiveSlots: 10);

        for (int i = 0; i < 50; i++)
        {
            var lw = writer.GetLineWriter();
            writer.Write(lw, $"{i:000}) item-{i}.mp4 :: ");
            writer.UpdateFinal(lw, "Complete");
            writer.ReleaseLineWriter(lw);

            if (i % 5 == 0)
            {
                writer.UpdateFooterStatus($"Active Downloads: {i} | Speed: {i}.0 MB/s");
            }
        }

        writer.UpdateFooterStatus("Active Downloads: 0 | Speed: 9.9 MB/s | Total: 99 MB");

        // Exactly one row should hold footer text, and it must be the latest value.
        var footerRows = console.RowContents.Where(kvp => kvp.Value.Contains("Active Downloads")).ToList();
        Assert.Single(footerRows);
        Assert.Contains("Speed: 9.9 MB/s", footerRows[0].Value);

        // All 50 completed items should have made it into scrolling history.
        var allOutput = string.Join("\n", console.RowContents.Values);
        Assert.Contains("000) item-0.mp4", allOutput);
        Assert.Contains("049) item-49.mp4", allOutput);
    }

    [Fact]
    public void GeneralLogMessage_DoesNotCorruptActiveRegionOrFooter()
    {
        var console = new FakeConsole { BufferHeight = 200, WindowWidth = 80 };
        var writer = CreateWriter(console, maxActiveSlots: 3);

        var lw = writer.GetLineWriter();
        writer.Write(lw, "001) still-downloading.mp4 :: ");
        writer.Update(lw, "Downloading");

        writer.UpdateFooterStatus("Active Downloads: 1 | Speed: 2.0 MB/s");

        // A general log line (e.g. a warning) is inserted while the item is still active.
        // Inserting it shifts the whole pinned region down by one row (as real terminal
        // scrolling would) - the footer's row number changes, but its content must survive
        // intact and remain the only row carrying footer text.
        writer.Warning("Stopped queuing new downloads (shutdown requested).");

        var footerRows = console.RowContents.Where(kvp => kvp.Value.Contains("Active Downloads")).ToList();
        Assert.Single(footerRows);
        Assert.Contains("Active Downloads: 1", footerRows[0].Value);

        var allOutput = string.Join("\n", console.RowContents.Values);
        Assert.Contains("Stopped queuing new downloads", allOutput);
        Assert.Contains("still-downloading.mp4", allOutput);
    }

    [Fact]
    public void EnsureBufferHeight_GrowsOnlyWhenBelowRegionMinimum()
    {
        var console = new FakeConsole { BufferHeight = 10, WindowWidth = 80 };
        var writer = CreateWriter(console, maxActiveSlots: 10); // region needs 12 + 5 = 17 minimum

        writer.EnsureBufferHeight(500); // expectedLineCount no longer drives sizing

        Assert.True(console.BufferHeight >= 17);
    }

    [Fact]
    public void EnsureBufferHeight_NoOpWhenBufferAlreadyLargeEnough()
    {
        var console = new FakeConsole { BufferHeight = 500, WindowWidth = 80 };
        var writer = CreateWriter(console);

        writer.EnsureBufferHeight(10);

        Assert.Equal(500, console.BufferHeight);
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
