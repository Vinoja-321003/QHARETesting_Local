namespace QualiFrame.Framework.Screenplay
{
    /// <summary>
    /// Represents a question that an actor can ask to get information.
    /// </summary>
    /// <typeparam name="T">The type of the answer.</typeparam>
    public interface IQuestion<out T>
    {
        /// <summary>
        /// Asks the question and returns an answer.
        /// </summary>
        /// <param name="actor">The actor asking the question.</param>
        /// <returns>The answer to the question.</returns>
        T AnsweredBy(IActor actor);
    }
}
