namespace QualiFrame.Framework.Screenplay
{
    /// <summary>
    /// Represents an actor in the Screenplay pattern - a user or system that performs actions.
    /// </summary>
    public interface IActor
    {
        /// <summary>
        /// Gets the actor's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the actor's abilities (e.g., WebDriver).
        /// </summary>
        IAbilities Abilities { get; }

        /// <summary>
        /// Performs a task.
        /// </summary>
        T AttemptsTo<T>(ITask<T> task);

        /// <summary>
        /// Performs a task that returns void.
        /// </summary>
        void AttemptsTo(ITask task);

        /// <summary>
        /// Asks a question and gets an answer.
        /// </summary>
        T AsksFor<T>(IQuestion<T> question);

        /// <summary>
        /// Checks if the actor can perform a specific ability.
        /// </summary>
        bool Can<T>() where T : IAbility;

        /// <summary>
        /// Gets a specific ability.
        /// </summary>
        T Using<T>() where T : class, IAbility;
    }
}
