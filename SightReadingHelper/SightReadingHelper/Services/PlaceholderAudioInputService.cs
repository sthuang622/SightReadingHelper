namespace SightReadingHelper.Services;

public class PlaceholderAudioInputService : IAudioInputService
{
    public event Action<float[], int>? SamplesAvailable;

    public Task StartAsync(int sampleRate)
    {
        SamplesAvailable?.Invoke([], sampleRate);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}
