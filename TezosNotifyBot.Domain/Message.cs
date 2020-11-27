using System;

namespace TezosNotifyBot.Domain
{
    public class Message
    {
        public int Id { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public DateTime CreateDate { get; set; }
        public string Text { get; set; }
        public string CallbackQueryData { get; set; }
        public bool FromUser { get; set; }
        public int TelegramMessageId { get; set; }
    }
}