using System;
using System.Collections.Generic;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Screenplay
{
    /// <summary>
    /// Collection of abilities that an actor possesses.
    /// </summary>
    public class ActorAbilities : IAbilities
    {
        private readonly Dictionary<Type, IAbility> _abilities = new();

        public T? Get<T>() where T : class, IAbility
        {
            var type = typeof(T);
            return _abilities.TryGetValue(type, out var ability) ? ability as T : null;
        }

        public void Add<T>(T ability) where T : IAbility
        {
            var type = typeof(T);
            _abilities[type] = ability;
            Logger.Debug("Ability {AbilityType} added to actor", type.Name);
        }

        public bool Has<T>() where T : IAbility
        {
            return _abilities.ContainsKey(typeof(T));
        }
    }
}
