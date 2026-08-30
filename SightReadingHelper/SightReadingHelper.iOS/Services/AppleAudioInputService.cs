using AVFoundation;
using Microsoft.Maui.ApplicationModel;
using SightReadingHelper.Services;
using System.Runtime.InteropServices;

namespace SightReadingHelper.iOS.Services;

public sealed class AppleAudioInputService : IAudioInputService, IDisposable
{
    private AVAudioEngine? _audioEngine;
    private AVAudioNodeTapBlock? _tapBlock;

    public event Action<float[], int>? SamplesAvailable;

    public async Task StartAsync(int sampleRate)
    {
        await StopAsync();

        var permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permissionStatus != PermissionStatus.Granted)
        {
            throw new UnauthorizedAccessException("Microphone permission is required for practice and calibration.");
        }

        var session = AVAudioSession.SharedInstance();
        session.SetCategory(AVAudioSessionCategory.Record);
        session.SetPreferredSampleRate(sampleRate, out _);
        session.SetActive(true);

        _audioEngine = new AVAudioEngine();
        var inputNode = _audioEngine.InputNode;
        var inputFormat = inputNode.GetBusOutputFormat(0);
        var actualSampleRate = (int)Math.Round(inputFormat.SampleRate);

        _tapBlock = (buffer, when) =>
        {
            var channelData = buffer.FloatChannelData;
            if (channelData == 0 || buffer.FrameLength == 0)
            {
                return;
            }

            var frameCount = (int)buffer.FrameLength;
            var samples = new float[frameCount];
            var firstChannel = Marshal.ReadIntPtr(new IntPtr(channelData));
            Marshal.Copy(firstChannel, samples, 0, frameCount);

            SamplesAvailable?.Invoke(samples, actualSampleRate);
        };

        inputNode.InstallTapOnBus(0, 2048, inputFormat, _tapBlock);
        _audioEngine.Prepare();

        if (!_audioEngine.StartAndReturnError(out var error))
        {
            inputNode.RemoveTapOnBus(0);
            _audioEngine.Dispose();
            _audioEngine = null;
            throw new InvalidOperationException(error?.LocalizedDescription ?? "Could not start the Apple microphone.");
        }
    }

    public Task StopAsync()
    {
        if (_audioEngine is null)
        {
            return Task.CompletedTask;
        }

        _audioEngine.InputNode.RemoveTapOnBus(0);
        _audioEngine.Stop();
        _audioEngine.Dispose();
        _audioEngine = null;
        _tapBlock = null;

        AVAudioSession.SharedInstance().SetActive(false);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}
