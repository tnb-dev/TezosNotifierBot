using System;

namespace TezosNotifyBot.Domain
{
    public class TwitterMessage
    {
        public int Id { get; set; }

        public string Text { get; set; }
        public DateTime CreateDate { get; set; }
        public string TwitterId { get; set; }
    }
}