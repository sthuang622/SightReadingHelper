using System.Text.Json;
using SightReadingHelper.Models;

namespace SightReadingHelper.Services;

public class PracticeDataService
{
    private const string InstrumentSeedFileName = "instruments.json";
    private const string PracticeOptionsFileName = "practice-options.json";
    private const string SettingsFileName = "settings.json";
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private IReadOnlyList<InstrumentProfile>? _cachedInstruments;
    private PracticeOptionsConfig? _cachedPracticeOptions;

    public async Task<IReadOnlyList<InstrumentProfile>> GetInstrumentsAsync()
    {
        if (_cachedInstruments is not null)
        {
            return _cachedInstruments;
        }

        await using var stream = await OpenInstrumentSeedFileAsync();
        var instruments = await JsonSerializer.DeserializeAsync<List<InstrumentProfile>>(stream, _serializerOptions) ?? [];
        _cachedInstruments = instruments;
        return _cachedInstruments;
    }

    public async Task<InstrumentProfile> GetSelectedInstrumentAsync()
    {
        var instruments = await GetInstrumentsAsync();
        var settings = await GetPracticeSettingsAsync();

        return instruments.FirstOrDefault(instrument => instrument.InstrumentName == settings.InstrumentName)
            ?? instruments.First();
    }

    public async Task<PracticeOptionsConfig> GetPracticeOptionsAsync()
    {
        if (_cachedPracticeOptions is not null)
        {
            return _cachedPracticeOptions;
        }

        await using var stream = await OpenRawFileAsync(PracticeOptionsFileName);
        var options = await JsonSerializer.DeserializeAsync<PracticeOptionsConfig>(stream, _serializerOptions) ?? new PracticeOptionsConfig();
        _cachedPracticeOptions = NormalizePracticeOptions(options);
        return _cachedPracticeOptions;
    }

    public async Task<PracticeSettings> GetPracticeSettingsAsync()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            return new PracticeSettings();
        }

        var json = await File.ReadAllTextAsync(path);
        var settings = JsonSerializer.Deserialize<PracticeSettings>(json, _serializerOptions) ?? new PracticeSettings();
        return NormalizePracticeSettings(settings);
    }

    public async Task SavePracticeSettingsAsync(PracticeSettings settings)
    {
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, _serializerOptions));
    }

    public async Task<CalibrationProfile?> GetCalibrationAsync(string instrumentName)
    {
        var path = GetCalibrationPath(instrumentName);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<CalibrationProfile>(json, _serializerOptions);
    }

    public async Task SaveCalibrationAsync(CalibrationProfile calibration)
    {
        var path = GetCalibrationPath(calibration.InstrumentName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(calibration, _serializerOptions));
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(FileSystem.Current.AppDataDirectory, "Data", SettingsFileName);
    }

    private static string GetCalibrationPath(string instrumentName)
    {
        return Path.Combine(FileSystem.Current.AppDataDirectory, "Data", "Calibrations", $"{instrumentName}.json");
    }

    private static PracticeSettings NormalizePracticeSettings(PracticeSettings settings)
    {
        if (settings.ExerciseLength <= 0)
        {
            settings.ExerciseLength = 10;
        }

        if (settings.ToleranceCents <= 0)
        {
            settings.ToleranceCents = 50;
        }

        if (settings.BeatTempoBpm <= 0)
        {
            settings.BeatTempoBpm = 60;
        }

        if (settings.MaxBaseNoteJump is null or < 0)
        {
            settings.MaxBaseNoteJump = 1;
        }

        settings.AllowedBaseNoteNames ??= [];
        return settings;
    }

    private static PracticeOptionsConfig NormalizePracticeOptions(PracticeOptionsConfig options)
    {
        if (options.ExerciseLengths.Count == 0)
        {
            options.ExerciseLengths = [5, 10, 20];
        }

        if (options.ToleranceCents.Count == 0)
        {
            options.ToleranceCents = [50, 30, 15];
        }

        if (options.BeatTempoBpms.Count == 0)
        {
            options.BeatTempoBpms = [60, 80, 100, 120];
        }

        if (options.MaxStringJumpLabels.Count == 0)
        {
            options.MaxStringJumpLabels =
            [
                "Same string only",
                "Adjacent strings only",
                "Skip one string",
                "Any string"
            ];
        }

        return options;
    }

    private static async Task<Stream> OpenInstrumentSeedFileAsync()
    {
        return await OpenRawFileAsync(InstrumentSeedFileName);
    }

    private static async Task<Stream> OpenRawFileAsync(string fileName)
    {
        if (OperatingSystem.IsWindows())
        {
            return OpenRawFileFromDisk(fileName);
        }

        try
        {
            return await FileSystem.Current.OpenAppPackageFileAsync(fileName);
        }
        catch (InvalidOperationException)
        {
            return OpenRawFileFromDisk(fileName);
        }
    }

    private static Stream OpenRawFileFromDisk(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(outputPath))
        {
            return File.OpenRead(outputPath);
        }

        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Raw", fileName);
        if (File.Exists(sourcePath))
        {
            return File.OpenRead(sourcePath);
        }

        throw new FileNotFoundException(
            $"Could not load {fileName}. Tried {outputPath} and {sourcePath}.",
            fileName);
    }
}
