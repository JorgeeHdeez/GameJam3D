namespace Core.Runtime
{
    /// <summary>
    /// Anything that produces a 0..1 awareness level and can be aggregated by a
    /// <see cref="DetectionRegistrySO"/> (e.g. an enemy perception). Living in Core
    /// lets the player-side stress meter read enemy awareness without the Player
    /// assembly depending on the Enemy assembly.
    /// </summary>
    public interface IDetectionSource
    {
        /// <summary>Current detection level, 0 (unaware) to 1 (fully alerted).</summary>
        float DetectionLevel { get; }
    }
}
