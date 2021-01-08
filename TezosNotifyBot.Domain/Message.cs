using System;

namespace TezosNotifyBot.Domain
{
    public class Message
    {
        public int Id { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }

        public MessageKind Kind { get; set; } = MessageKind.Simple;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string Text { get; set; }
        public string CallbackQueryData { get; set; }
        public bool FromUser { get; set; }
        public int? TelegramMessageId { get; set; }

        public static Message Push(int userId, string text)
        {
            return new Message
            {
                Kind = MessageKind.Push,
                UserId = userId,
                FromUser = false,
                Text = text,
            };
        }

        public void Sent(in int telegramMessageId)
        {
            TelegramMessageId = telegramMessageId;
        }
    }

    public enum MessageKind
    {
        Simple,
        Push
    }
}