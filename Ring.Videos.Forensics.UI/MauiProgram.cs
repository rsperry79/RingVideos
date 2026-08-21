using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Ring.Videos.Forensics.UI.ViewModels;
using Ring.Videos.Forensics.UI.Views;

namespace Ring.Videos.Forensics.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .Services.AddSingleton<MainPage>()
            .AddSingleton<MainPageViewModel>()
            .AddSingleton<EvidenceDetailsPage>()
            .AddSingleton<EvidenceDetailsViewModel>()
            .AddSingleton<ForensicReportsPage>()
            .AddSingleton<ForensicReportsViewModel>()
            .AddSingleton<SignalAnomaliesPage>()
            .AddSingleton<SignalAnomaliesViewModel>()
            .AddSingleton<ChainOfCustodyPage>()
            .AddSingleton<ChainOfCustodyViewModel>()
            .AddSingleton<AccessControlPage>()
            .AddSingleton<AccessControlViewModel>();

        return builder.Build();
    }
}
