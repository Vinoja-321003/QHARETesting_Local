namespace QualiFrame.Framework.Screenplay
{
    /// <summary>
    /// Collection of abilities that an actor possesses.
    /// </summary>
    public interface IAbilities
    {
        /// <summary>
        /// Gets an ability of the specified type.
        /// </summary>
        T? Get<T>() where T : class, IAbility;

        /// <summary>
        /// Adds an ability.
        /// </summary>
        void Add<T>(T ability) where T : IAbility;

        /// <summary>
        /// Checks if the actor has a specific ability.
        /// </summary>
        bool Has<T>() where T : IAbility;
    }
}
