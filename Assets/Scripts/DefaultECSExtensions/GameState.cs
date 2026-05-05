namespace DefaultECSExtensions
{
    /// <summary>Carries per-frame data passed to all update systems each Unity Update tick.</summary>
    public readonly struct GameState
    {
        /// <summary>Elapsed time in seconds since the previous frame.</summary>
        public readonly float DeltaTime;

        /// <param name="deltaTime">Elapsed time in seconds since the previous frame.</param>
        public GameState(float deltaTime) => DeltaTime = deltaTime;
    }
}
