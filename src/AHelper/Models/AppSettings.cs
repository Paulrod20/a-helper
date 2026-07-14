namespace AHelper.Models;

/// <summary>
/// Stores user interface preferences independently from hardware state.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Remembers the most recently selected placeholder until hardware-backed profiles replace it.
    /// </summary>
    public PerformanceMode SelectedMode { get; set; } = PerformanceMode.Balanced;
}
