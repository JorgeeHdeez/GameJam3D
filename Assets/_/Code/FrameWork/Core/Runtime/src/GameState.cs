namespace Core.Runtime
{
    /// <summary>
    /// High-level flow state of a run: normal play, the rewind-to-start sequence
    /// triggered when the player is caught, or the terminal win state reached at the
    /// exit checkpoint.
    /// </summary>
    public enum GameState
    {
        Playing,
        Rewinding,
        Won
    }
}
