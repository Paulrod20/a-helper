using System.Text.Json;
using AHelper.Models;

namespace AHelper.Services;

/// <summary>
/// Persists lightweight user preferences without coupling the UI to file-system details.
/// </summary>
public sealed class SettingsService
{
    private const string ApplicationDirectoryName = "AHelper";
    private const string SettingsFileName = "config.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    /// <summary>
    /// Uses the current user's roaming application-data directory so preferences survive upgrades.
    /// </summary>
    public SettingsService()
    {
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ApplicationDirectoryName,
            SettingsFileName);
    }

    /// <summary>
    /// Returns defaults when preferences have not been created or cannot be read safely.
    /// </summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves preferences best-effort because a settings failure should not stop hardware control.
    /// </summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var directory = Path.GetDirectoryName(_settingsPath)
                ?? throw new InvalidOperationException("The settings path has no parent directory.");

            Directory.CreateDirectory(directory);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
        }
        catch (IOException)
        {
            // Preferences are non-critical; hardware functionality should remain available.
        }
        catch (UnauthorizedAccessException)
        {
            // Preferences are non-critical; hardware functionality should remain available.
        }
    }
}
