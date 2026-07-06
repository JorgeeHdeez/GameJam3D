namespace Core.Runtime
{
    /// <summary>
    /// Implemented by any object that needs to receive a managed tick from the
    /// <see cref="UpdateManager"/> instead of using MonoBehaviour.Update().
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>
        /// Called once per managed frame.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the previous tick.</param>
        void Tick(float deltaTime);
    }
}