namespace SightReadingHelper.WinUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp();

            builder.Services.AddSingleton<SightReadingHelper.Services.IAudioInputService, Services.WindowsAudioInputService>();

            return builder.Build();
        }
    }
}
