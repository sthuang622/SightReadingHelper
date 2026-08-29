namespace SightReadingHelper.Services;

public interface IAudioInputService
{
    event Action<float[], int>? SamplesAvailable;

    Task StartAsync(int sampleRate);

    Task StopAsync();
}
