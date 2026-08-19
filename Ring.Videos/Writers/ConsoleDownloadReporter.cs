using System;
using System.Threading.Tasks;

using KoenZomers.Ring.Api;

using Spectre.Console;

namespace Ring.Videos.Writers
{
    /// <summary>
    /// Implements <see cref="IDownloadReporter"/> on top of <see cref="ConsoleWriter"/> and
    /// Spectre.Console, so <see cref="RingVideoService"/> never has to know how progress is
    /// actually rendered.
    /// </summary>
    public class ConsoleDownloadReporter : IDownloadReporter
    {
        private readonly ConsoleWriter cw;

        public ConsoleDownloadReporter(ConsoleWriter consoleWriter)
        {
            cw = consoleWriter;
        }

        public void Info(string message) => cw.Info(message);
        public void Warning(string message) => cw.Warning(message);
        public void Error(string message) => cw.Error(message);
        public void Highlight(string message) => cw.Highlight(message);

        public object BeginItem(string initialMessage)
        {
            var lw = cw.GetLineWriter();
            cw.Write(lw, initialMessage ?? string.Empty);
            return lw;
        }

        public void WriteItem(object item, string message) => cw.Write((LineWriter)item, message);
        public void UpdateItem(object item, string message) => cw.Update((LineWriter)item, message);
        public void CompleteItem(object item, string message) => cw.UpdateFinal((LineWriter)item, message);
        public void ErrorItem(object item, string message) => cw.UpdateError((LineWriter)item, message);
        public void WarnItem(object item, string message) => cw.UpdateWarning((LineWriter)item, message);
        public void ReleaseItem(object item) => cw.ReleaseLineWriter((LineWriter)item);
        public void UpdateFooter(string message) => cw.UpdateFooterStatus(message);
        public void EnsureCapacity(int expectedItemCount) => cw.EnsureBufferHeight(expectedItemCount);
        public void ClearItems() => cw.ClearLineWriters();

        public async Task<T> RunWithStatusAsync<T>(string initialMessage, Func<Func<string, Task>, Task<T>> operation)
        {
            T result = default;
            await AnsiConsole.Status()
                .StartAsync(initialMessage, async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots2);
                    ctx.SpinnerStyle(Style.Parse("yellow"));

                    Task UpdateStatus(string message)
                    {
                        ctx.Status(message);
                        return Task.CompletedTask;
                    }

                    result = await operation(UpdateStatus);
                });
            return result;
        }

        public async Task RunWithStatusAsync(string initialMessage, Func<Func<string, Task>, Task> operation)
        {
            await AnsiConsole.Status()
                .StartAsync(initialMessage, async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots2);
                    ctx.SpinnerStyle(Style.Parse("yellow"));

                    Task UpdateStatus(string message)
                    {
                        ctx.Status(message);
                        return Task.CompletedTask;
                    }

                    await operation(UpdateStatus);
                });
        }

        public Task<string> PromptTwoFactorCodeAsync()
        {
            return Task.FromResult(Console.ReadLine());
        }
    }
}
