using NAudio.Wave;
using SightReadingHelper.Services;

namespace SightReadingHelper.WinUI.Services;

public class WindowsAudioInputService : IAudioInputService, IDisposable
{
    private WaveInEvent? _waveIn;

    public event Action<float[], int>? SamplesAvailable;

    public Task StartAsync(int sampleRate)
    {
        Stop();

        _waveIn = new WaveInEvent
        {
            DeviceNumber = 0,
            BufferMilliseconds = 50,
            WaveFormat = new WaveFormat(sampleRate, 16, 1)
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.StartRecording();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Stop();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_waveIn is null || e.BytesRecorded <= 0)
        {
            return;
        }

        var sampleCount = e.BytesRecorded / 2;
        var samples = new float[sampleCount];

        for (var index = 0; index < sampleCount; index++)
        {
            var sample = BitConverter.ToInt16(e.Buffer, index * 2);
            samples[index] = sample / 32768f;
        }

        SamplesAvailable?.Invoke(samples, _waveIn.WaveFormat.SampleRate);
    }

    private void Stop()
    {
        if (_waveIn is null)
        {
            return;
        }

        _waveIn.DataAvailable -= OnDataAvailable;
        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
    }
}
