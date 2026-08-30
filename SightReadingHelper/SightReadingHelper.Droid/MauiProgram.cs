namespace SightReadingHelper.Droid
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp();

            builder.Services.AddSingleton<SightReadingHelper.Services.IAudioInputService, Services.AndroidAudioInputService>();

            return builder.Build();
        }
    }
}
