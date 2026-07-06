namespace Enemy.Runtime
{
    /// <summary>
    /// Awareness level of an enemy toward the player, driven by a detection meter.
    /// </summary>
    public enum PerceptionState
    {
        Unaware,
        Suspicious,
        Alerted
    }
}