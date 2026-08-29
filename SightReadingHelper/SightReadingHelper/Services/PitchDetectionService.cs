using SightReadingHelper.Models;

namespace SightReadingHelper.Services;

public class PitchDetectionService
{
    private const double MinimumRms = 0.012;
    private const double MinimumConfidence = 0.55;
    private const double MinimumFrequencyHz = 90;
    private const double MaximumFrequencyHz = 1000;

    public PitchDetectionResult AnalyzeFrequency(double frequencyHz)
    {
        if (frequencyHz <= 0)
        {
            return CreateNoPitchResult();
        }

        return CreatePitchResult(frequencyHz, 1);
    }

    public PitchDetectionResult AnalyzeSamples(float[] samples, int sampleRate)
    {
        if (samples.Length == 0 || sampleRate <= 0)
        {
            return CreateNoPitchResult();
        }

        var rms = CalculateRms(samples);
        if (rms < MinimumRms)
        {
            return CreateNoPitchResult(rms);
        }

        var frequencyHz = EstimatePitchYin(samples, sampleRate, out var confidence);
        if (frequencyHz <= 0 || confidence < MinimumConfidence)
        {
            return CreateNoPitchResult(rms, confidence);
        }

        var result = CreatePitchResult(frequencyHz, confidence);
        result.Volume = rms;
        return result;
    }

    private static PitchDetectionResult CreatePitchResult(double frequencyHz, double confidence)
    {
        var midiNote = PitchMath.FrequencyToMidi(frequencyHz);
        var referenceFrequency = PitchMath.MidiToFrequency(midiNote);

        return new PitchDetectionResult
        {
            HasPitch = true,
            FrequencyHz = frequencyHz,
            ClosestMidiNote = midiNote,
            ClosestNoteName = PitchMath.MidiToNoteName(midiNote),
            CentsDifference = PitchMath.CalculateCentsDifference(frequencyHz, referenceFrequency),
            Confidence = confidence
        };
    }

    private static PitchDetectionResult CreateNoPitchResult(double volume = 0, double confidence = 0)
    {
        return new PitchDetectionResult
        {
            HasPitch = false,
            Confidence = confidence,
            Volume = volume
        };
    }

    private static double CalculateRms(IReadOnlyList<float> samples)
    {
        double sum = 0;
        for (var index = 0; index < samples.Count; index++)
        {
            sum += samples[index] * samples[index];
        }

        return Math.Sqrt(sum / samples.Count);
    }

    private static double EstimatePitchYin(IReadOnlyList<float> samples, int sampleRate, out double confidence)
    {
        var minTau = Math.Max(2, sampleRate / (int)MaximumFrequencyHz);
        var maxTau = Math.Min(samples.Count / 2, sampleRate / (int)MinimumFrequencyHz);
        var differences = new double[maxTau + 1];

        for (var tau = minTau; tau <= maxTau; tau++)
        {
            double sum = 0;
            for (var index = 0; index < samples.Count - tau; index++)
            {
                var delta = samples[index] - samples[index + tau];
                sum += delta * delta;
            }

            differences[tau] = sum;
        }

        double runningSum = 0;
        var bestTau = -1;
        var bestValue = double.MaxValue;

        for (var tau = minTau; tau <= maxTau; tau++)
        {
            runningSum += differences[tau];
            var normalized = runningSum <= 0
                ? 1
                : differences[tau] * tau / runningSum;

            if (normalized < bestValue)
            {
                bestValue = normalized;
                bestTau = tau;
            }

            if (tau > minTau && normalized < 0.12)
            {
                bestTau = tau;
                bestValue = normalized;
                break;
            }
        }

        if (bestTau <= 0)
        {
            confidence = 0;
            return 0;
        }

        confidence = Math.Clamp(1 - bestValue, 0, 1);
        return sampleRate / (double)bestTau;
    }
}
