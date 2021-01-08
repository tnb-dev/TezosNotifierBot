using System;

namespace TezosNotifyBot.Domain
{
    public class Message
    {
        public int Id { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }

        public MessageKind Kind { get; set; } = MessageKind.Simple;

        public MessageStatus Status { get; set; } = MessageStatus.Sent;
        
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
                Status = MessageStatus.Queued,
                Text = text,
            };
        }

        public void Sent(in int telegramMessageId)
        {
            TelegramMessageId = telegramMessageId;
            Status = MessageStatus.Sent;
        }

        public void Failed()
        {
            Status = MessageStatus.Failed;
        }
    }

    public enum MessageKind
    {
        Simple,
        Push
    }

    public enum MessageStatus
    {
        Sent,
        Queued,
        Failed
    }
}