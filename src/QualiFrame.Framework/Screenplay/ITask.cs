namespace QualiFrame.Framework.Screenplay
{
    /// <summary>
    /// Represents a task that an actor can perform.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Performs the task.
        /// </summary>
        /// <param name="actor">The actor performing the task.</param>
        void PerformAs(IActor actor);
    }

    /// <summary>
    /// Represents a task that returns a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    public interface ITask<out T>
    {
        /// <summary>
        /// Performs the task and returns a result.
        /// </summary>
        /// <param name="actor">The actor performing the task.</param>
        /// <returns>The result of the task.</returns>
        T PerformAs(IActor actor);
    }
}
