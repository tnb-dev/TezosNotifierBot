using System;
using System.Collections.Generic;

namespace TezosNotifyBot.Abstractions
{
    public abstract class CommandsProfile
    {

        private readonly Dictionary<string, Type> _updateHandlers = new Dictionary<string, Type>();

        public IReadOnlyDictionary<string, Type> UpdateHandlers => _updateHandlers;

        protected void Handle<T>(string pattern) where T : IUpdateHandler
        {
            _updateHandlers.Add(pattern, typeof(T));
        }
    }
}