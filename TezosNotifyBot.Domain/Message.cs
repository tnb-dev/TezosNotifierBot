using System;

namespace TezosNotifyBot.Domain
{
#nullable disable
	public class Message
    {
        public int Id { get; set; }
        public User User { get; set; }
        public long UserId { get; set; }

        public MessageKind Kind { get; set; } = MessageKind.Simple;

        public MessageStatus Status { get; set; } = MessageStatus.Sent;
        
        public DateTime CreateDate { get; set; }
        public string Text { get; set; }
        public string CallbackQueryData { get; set; }
        public bool FromUser { get; set; }
        public int? TelegramMessageId { get; set; }
        public string Image { get; set; }

        public static Message Push(long userId, string text)
        {
            return new Message
            {
                Kind = MessageKind.Push,
                UserId = userId,
                FromUser = false,
                CreateDate = DateTime.UtcNow,
                Text = text,
                Status = MessageStatus.Sending,
            };
        }

        public void Sent(in int telegramMessageId)
        {
            TelegramMessageId = telegramMessageId;
            Status = MessageStatus.Sent;
        }

        public void SentFailed()
        {
            Status = MessageStatus.SentFailed;
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
        SentFailed,
        Sending
    }
}