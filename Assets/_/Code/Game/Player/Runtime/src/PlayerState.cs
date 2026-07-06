namespace Player.Runtime
{
    /// <summary>
    /// High-level locomotion / stealth posture of the player character.
    /// Used to drive movement, animation and emitted noise.
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Crouching,
        Crawling,
        WallHugging,
        KnockedOut
    }
}