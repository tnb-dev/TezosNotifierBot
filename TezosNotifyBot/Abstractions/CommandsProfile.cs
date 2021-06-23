using System;
using System.Collections.Generic;

namespace TezosNotifyBot.Abstractions
{
    public abstract class CommandsProfile
    {

        private readonly Dictionary<string, Type> _updateHandlers = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> _callbackHandlers = new Dictionary<string, Type>();

        public IReadOnlyDictionary<string, Type> UpdateHandlers => _updateHandlers;
        public IReadOnlyDictionary<string, Type> CallbackHandlers => _callbackHandlers;

        protected void AddCommand<T>(string pattern) where T : IUpdateHandler
        {
            _updateHandlers.Add(pattern, typeof(T));
        }
        
        protected void AddCallback<T>(string pattern) where T : ICallbackHandler
        {
            _callbackHandlers.Add(pattern, typeof(T));
        }
    }
}