using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TezosNotifyBot.Domain;

namespace TezosNotifyBot.Model
{
	public record Notification(UserAddress UserAddress, User User, string Text);
}
