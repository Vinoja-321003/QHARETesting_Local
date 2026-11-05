using System;
using OpenQA.Selenium;
using QualiFrame.Framework.Screenplay.Abilities;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Screenplay
{
    /// <summary>
    /// Represents an actor in the Screenplay pattern.
    /// </summary>
    public class Actor : IActor
    {
        public string Name { get; }
        public IAbilities Abilities { get; }

        public Actor(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Abilities = new ActorAbilities();
            Logger.Information("Actor '{Name}' created", name);
        }

        /// <summary>
        /// Creates an actor with the ability to browse the web.
        /// </summary>
        public static Actor Named(string name)
        {
            return new Actor(name);
        }

        /// <summary>
        /// Adds the ability to browse the web.
        /// </summary>
        public Actor WhoCan(BrowseTheWeb ability)
        {
            Abilities.Add(ability);
            return this;
        }

        /// <summary>
        /// Adds a custom ability.
        /// </summary>
        public Actor WhoCan<T>(T ability) where T : IAbility
        {
            Abilities.Add(ability);
            return this;
        }

        public T AttemptsTo<T>(ITask<T> task)
        {
            Logger.Debug("Actor '{Name}' attempts to perform task: {TaskType}", Name, typeof(T).Name);
            return task.PerformAs(this);
        }

        public void AttemptsTo(ITask task)
        {
            Logger.Debug("Actor '{Name}' attempts to perform task: {TaskType}", Name, task.GetType().Name);
            task.PerformAs(this);
        }

        public T AsksFor<T>(IQuestion<T> question)
        {
            Logger.Debug("Actor '{Name}' asks question: {QuestionType}", Name, question.GetType().Name);
            return question.AnsweredBy(this);
        }

        public bool Can<T>() where T : IAbility
        {
            return Abilities.Has<T>();
        }

        public T Using<T>() where T : class, IAbility
        {
            var ability = Abilities.Get<T>();
            if (ability == null)
            {
                throw new InvalidOperationException($"Actor '{Name}' does not have the ability '{typeof(T).Name}'");
            }
            return ability;
        }
    }
}
