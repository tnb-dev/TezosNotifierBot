using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace TezosNotifyBot
{
	public class KeyboardMarkup
	{
		ReplyKeyboardMarkup replyKeyboardMarkup;
		InlineKeyboardMarkup inlineKeyboardMarkup;

		private KeyboardMarkup(List<List<KeyboardButton>> buttons) 
		{
			replyKeyboardMarkup = new ReplyKeyboardMarkup {
				Keyboard = buttons,
				ResizeKeyboard = true,
				IsPersistent = true
			};
		}

		private KeyboardMarkup(List<InlineKeyboardButton[]> buttons)
		{
			inlineKeyboardMarkup = new InlineKeyboardMarkup(buttons);
		}

		public static KeyboardMarkup ReplyKeyboard(string[][] buttons)
		{
			var btns = new List<List<KeyboardButton>>();
			foreach (var b in buttons)
			{
				var row = new List<KeyboardButton>();
				row.AddRange(b.Select(o => new KeyboardButton(o)));
				btns.Add(row);
			}
			return new KeyboardMarkup(btns);
		}

		public static KeyboardMarkup InlineKeyboard(List<List<(string Text, string Callback)>> buttons)
		{
			return new KeyboardMarkup(buttons.Select(o => o.Select(b => new InlineKeyboardButton(b.Text, b.Callback)).ToArray()).ToList());
		}

		public static KeyboardMarkup SearchInlineButton(string text)
		{
			return new KeyboardMarkup(new List<InlineKeyboardButton[]> { new[] {
				new InlineKeyboardButton
				{
					SwitchInlineQueryCurrentChat = "",
					Text = text
				}
			}});
		}

		public static implicit operator ReplyMarkup(KeyboardMarkup keyboardMarkup)
		{
			return (ReplyMarkup)keyboardMarkup?.inlineKeyboardMarkup ?? keyboardMarkup?.replyKeyboardMarkup;
		}

		public static implicit operator InlineKeyboardMarkup(KeyboardMarkup keyboardMarkup)
		{
			return keyboardMarkup?.inlineKeyboardMarkup;
		}
	}
}
