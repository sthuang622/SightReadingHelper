using Android.Media;
using Microsoft.Maui.ApplicationModel;
using SightReadingHelper.Services;

namespace SightReadingHelper.Droid.Services;

public sealed class AndroidAudioInputService : IAudioInputService, IDisposable
{
    private AudioRecord? _audioRecord;
    private CancellationTokenSource? _captureCancellation;
    private Task? _captureTask;

    public event Action<float[], int>? SamplesAvailable;

    public async Task StartAsync(int sampleRate)
    {
        await StopAsync();

        var permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permissionStatus != PermissionStatus.Granted)
        {
            throw new UnauthorizedAccessException("Microphone permission is required for practice and calibration.");
        }

        var minBufferSize = AudioRecord.GetMinBufferSize(
            sampleRate,
            ChannelIn.Mono,
            Encoding.Pcm16bit);

        if (minBufferSize <= 0)
        {
            throw new InvalidOperationException("This device cannot open the microphone at the requested sample rate.");
        }

        var bufferSize = Math.Max(minBufferSize, sampleRate / 10);
        _audioRecord = new AudioRecord(
            AudioSource.Mic,
            sampleRate,
            ChannelIn.Mono,
            Encoding.Pcm16bit,
            bufferSize);

        if (_audioRecord.State != State.Initialized)
        {
            _audioRecord.Release();
            _audioRecord.Dispose();
            _audioRecord = null;
            throw new InvalidOperationException("Could not initialize the Android microphone.");
        }

        _captureCancellation = new CancellationTokenSource();
        _audioRecord.StartRecording();
        _captureTask = Task.Run(() => CaptureAsync(sampleRate, bufferSize, _captureCancellation.Token));
    }

    public async Task StopAsync()
    {
        var cancellation = _captureCancellation;
        _captureCancellation = null;
        cancellation?.Cancel();

        if (_captureTask is not null)
        {
            try
            {
                await _captureTask;
            }
            catch (OperationCanceledException)
            {
            }

            _captureTask = null;
        }

        cancellation?.Dispose();

        if (_audioRecord is null)
        {
            return;
        }

        if (_audioRecord.RecordingState == RecordState.Recording)
        {
            _audioRecord.Stop();
        }

        _audioRecord.Release();
        _audioRecord.Dispose();
        _audioRecord = null;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    private void CaptureAsync(int sampleRate, int bufferSize, CancellationToken cancellationToken)
    {
        var audioRecord = _audioRecord;
        if (audioRecord is null)
        {
            return;
        }

        var shortBuffer = new short[bufferSize / 2];

        while (!cancellationToken.IsCancellationRequested)
        {
            var samplesRead = audioRecord.Read(shortBuffer, 0, shortBuffer.Length);
            if (samplesRead <= 0)
            {
                continue;
            }

            var samples = new float[samplesRead];
            for (var index = 0; index < samplesRead; index++)
            {
                samples[index] = shortBuffer[index] / 32768f;
            }

            SamplesAvailable?.Invoke(samples, sampleRate);
        }
    }
}
