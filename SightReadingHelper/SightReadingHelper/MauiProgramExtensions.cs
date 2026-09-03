using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SightReadingHelper.Services;

namespace SightReadingHelper;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<PracticeDataService>();
        builder.Services.AddSingleton<MusicXmlService>();
        builder.Services.AddSingleton<ExerciseGeneratorService>();
        builder.Services.AddSingleton<PitchDetectionService>();
        builder.Services.AddSingleton<PitchCalibrationService>();
        builder.Services.AddSingleton<NoteJudgmentService>();
        builder.Services.TryAddSingleton<IAudioInputService, PlaceholderAudioInputService>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<CalibrationPage>();
        builder.Services.AddTransient<PracticePage>();
        builder.Services.AddTransient<MusicXmlPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder;
    }
}
