using System.Collections.Generic;
using System.Linq;
using System.Text;
using TezosNotifyBot.Shared;
using TezosNotifyBot.Shared.Extensions;

namespace TezosNotifyBot.Abstractions
{
    public class MessageBuilder
    {
        private readonly StringBuilder _text = new StringBuilder();
        private readonly List<string> _tags = new List<string>();

        public MessageBuilder AddEmptyLine()
        {
            _text.AppendLine();
            return this;
        }
        
        public MessageBuilder AddLine(string text)
        {
            _text.AppendLine(text);
            return this;
        }
        
        public MessageBuilder WithHashTag(string tag)
        {
            _tags.Add(tag.Trim().Replace("#", string.Empty));
            return this;
        }
        
        public MessageBuilder WithHashTag(IHasHashTag tag)
        {
            _tags.Add(tag.HashTag().Trim().Replace("#", string.Empty));
            return this;
        }
        
        public string Build(bool includeTags)
        {
            if (includeTags)
            {
                _text.AppendLine();
                _text.Append(_tags.Select(tag => $"#{tag}").Join(" "));
            }

            return _text.ToString();
        }
    }
}