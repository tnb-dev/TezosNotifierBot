using System;
using System.Collections.Generic;
using System.Text;
using TezosNotifyBot.Model;

namespace TezosNotifyBot
{
    public static class TextStrings
    {
		/*
        public static string _Welcome(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "💚 Добро пожаловать, " + (u.Firstname + " " + u.Lastname).Trim() + @"!

С помощью Tezos Notifier Bot Вы можете мониторить различные события в блокчейне Tezos, например, транзакции, делегирование, пропуски выпечки делегатом, двойная выпечка и т.д.

Поддержать нас донейтом XTZ: tz1g5jJc6MWdmmUXdp5eb1KTj8TTU5U74cry

💡 <b>Первые шаги</b>:
 - нажмите кнопку ✳️ <b>Новый адрес</b> и введите адрес Tezos, за которым хотите следить. Управлять списком адресом и их настройками можно по кнопке 👛 <b>Мои адреса</b>
 - или просто не делайте ничего и вы будете получать уведомления о 🐋 <b>больших транзакциях</b>, отключить или настроить которые вы можете по кнопке ⚙️ <b>Настройки</b>

Полное описание и инструкция: http://tzsnt.fr/";
                    default:
                    return "💚 Welcome " + (u.Firstname + " " + u.Lastname).Trim() + @"!

With Tezos Notifier Bot you can simply monitor various events in Tezos blockchain, like transactions, delegations, missing block endorsing, double baking, etc.

Donate XTZ to support us: tz1g5jJc6MWdmmUXdp5eb1KTj8TTU5U74cry

💡 <b>First steps</b>:
 - click the ✳️ <b>New address</b> button and type the Tezos address you want to follow. Use the 👛 <b>My Addresses</b> button to manage address list and special settings.
 - or simply do nothing and you will be notified about 🐋 <b>whale transactions</b>, which you can disable or configure using ⚙️ <b>Settings</b> button

Full description & manual: http://tzsnt.fr/";
            }
        }

        public static string _WelcomeBack(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "💚 С возвращением, " + (u.Firstname + " " + u.Lastname).Trim() + "!";
                default:
                    return "💚 Welcome back " + (u.Firstname + " " + u.Lastname).Trim() + "!";
            }
        }

        internal static string _HashTags(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Хэштеги";
                default:
                    return "Hashtags";
            }
		}
		internal static string Search(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "🔎 Поиск";
				default:
					return "🔎 Search";
			}
		}
		internal static string VotingNotify(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Голосования";
				default:
					return "Votings";
			}
		}
		internal static string NewAddress(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "✳️ Новый адрес";
                default:
                    return "✳️ New Address";
            }
        }
        internal static string MyAddresses(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "👛 Мои адреса";
                default:
                    return "👛 My Addresses";
			}
        }
        internal static string Contact(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "✉️ Обратная связь";
                default:
                    return "✉️ Feedback";
            }
        }
        internal static string Settings(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "⚙️ Настройки";
                default:
                    return "⚙️ Settings";
            }
        }
		internal static string GoBack(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "⬅️ Назад";
                default:
                    return "⬅️ Go back";
            }
        }
        internal static string Delete(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "🗑 Удалить";
                default:
                    return "🗑 Delete";
            }
		}
		internal static string ManageAddress(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "🛠 Настроить";
				default:
					return "🛠 Tune";
			}
		}
		internal static string RenameAddress(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "📝 Переименовать";
				default:
					return "📝 Rename";
			}
		}
		internal static string SetThreshold(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "✂️ Порог транзакций";
                default:
                    return "✂️ Transaction Threshold";
            }
		}
		internal static string SetDlgThreshold(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "✂️ Порог делегирования";
				default:
					return "✂️ Delegation Threshold";
			}
		}
        internal static string RewardNotify(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Вознаграждения";
                default:
                    return "Rewards";
            }
        }
        internal static string CycleNotify(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Завершение цикла";
                default:
                    return "Cycle completion";
            }
		}
		internal static string MissesNotify(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Пропуски";
				default:
					return "Misses";
			}
		}
		internal static string TransactionNotify(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Транзакции";
                default:
                    return "Transactions";
            }
		}
		internal static string DelegationNotify(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Делегирования";
				default:
					return "Delegations";
			}
		}

		internal static string WhaleAlerts(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "🐋 Оповещения о китах";
				default:
					return "🐋 Whale alerts";
			}
		}

		internal static string Explorer(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "🌐 Эксплорер";
				default:
					return "🌐 Explorer";
			}
		}

		internal static string NetworkIssueAlerts(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "⚠️ Оповещения о сбоях сети";
				default:
					return "⚠️ Network failure alerts";
			}
		}
        internal static string AddAddress(string name, User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "👀 Следить за " + name;
                default:
                    return "👀 Monitor delegate " + name;
            }
        }
		internal static string Off(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Выкл";
				default:
					return "Off";
			}
		}
        internal static string MessageDelivered(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Сообщение доставлено для русскоязычных пользователей";
                default:
                    return "Message delivered for english speaking users";
            }
        }
        internal static string MessageSentToSupport(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Сообщение отправлено. Спасибо за ваше обращение 💛";
                default:
                    return "Message sent. Thanks for contacting 💛";
            }
        }
        internal static string WriteHere(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Напишите ваше сообщение";
                default:
                    return "Please, write here your message";
            }
        }
        internal static string SeeYou(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "🙋 До связи!";
                default:
                    return "🙋 Ok, see you later";
            }
        }
        internal static string AddressDeleted(User u, string addr, string name)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"Адрес {addr} {name} удален";
                default:
                    return $"Address {addr} {name} deleted";
            }
        }
        internal static string AddressNotExist(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "🚫 Адрес не существует";
                default:
                    return "🚫 Address doesn't exists";
            }
        }
        internal static string SupportReply(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "📩 Сообщение от поддержки:";
                default:
                    return "📩 Message from support:";
            }
        }
        internal static string UnrecognizedCommand(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "🙈 Команда не распознана";
                default:
                    return "🙈 Command not recognized";
            }
        }

		internal static string ChooseExplorer(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Выберите эксплорер блокчейна";
				default:
					return "Choose blockchain explorer";
			}
		}
		internal static string WhaleAlertsTip(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Выберите примерный порог транзакций китов";
				default:
					return "Choose whale transactions average threshold";
			}

		}

		internal static string NetworkIssueAlertsTip(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Выберите время простоя сети Tezos в минутах до получения уведомления о сбое в работе блокчейна";
				default:
					return "Choose Tezos network downtime in minutes before sending notifications about blockchain faults";
			}

		}
		
		internal static string WhaleAlertSet(User u, string amount)
		{
			switch (u.Language)
			{
				case "ru":
					return $"Порог транзакций китов установлен: {amount}";
				default:
					return $"Whale transactions threshold set: {amount}";
			}

		}
		internal static string IncorrectTezosAddress(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Некорректный адрес";
                default:
                    return "Incorrect Tezos Address";
            }
        }
		internal static string NoAddresses(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "У вас нет адресов";
                default:
                    return "You have no addresses";
            }
        }
        internal static string NewAddressHint(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Отправьте адрес кошелька Тезос, который вы хотите отслеживать и название адреса (опционально). Пример:\n\n<i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf Артур</i>\n\nИли воспользуйтесь поиском:";
                default:
                    return "Send me your Tezos address you want to monitor and the title for this address (optional). For example:\n\n<i>tz1XuPMB8X28jSoy7cEsXok5UVR5mfhvZLNf Arthur</i>\n\nOr use the search:";
            }
        }
        internal static string AmountThreshold(User u, string threshold)
        {
            switch (u.Language)
            {
                case "cn":
                    return "";
                case "ru":
                    return $"Порог суммы транзакции: <b>{threshold}</b>";
                default:
                    return $"Transaction threshold: <b>{threshold}</b>";
            }
		}
		internal static string DelegationAmountThreshold(User u, string threshold)
		{
			switch (u.Language)
			{
				case "cn":
					return "";
				case "ru":
					return $"Порог суммы делегирования: <b>{threshold}</b>";
				default:
					return $"Delegation threshold: <b>{threshold}</b>";
			}
		}
		internal static string EnterAmountThreshold(User u, string addr, string name)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"Введите минимальную сумму транзакции для <a href='{t.account(addr)}'>{name}</a> для получения уведомлений";
                default:
                    return $"Enter minimum transaction amount for <a href='{t.account(addr)}'>{name}</a> to receive notifications";
            }
		}
		internal static string EnterNewName(User u, string addr, string name)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"Введите новое название для <a href='{t.account(addr)}'>{name}</a>";
				default:
					return $"Enter new name for <a href='{t.account(addr)}'>{name}</a>";
			}
		}
		internal static string EnterDlgAmountThreshold(User u, string addr, string name)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"Введите минимальную сумму делегирования для <a href='{t.account(addr)}'>{name}</a> для получения уведомлений";
				default:
					return $"Enter minimum delegation amount for <a href='{t.account(addr)}'>{name}</a> to receive notifications";
			}
		}
		internal static string ThresholdEstablished(User u, string addr, string name, string amount)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"Для <a href='{t.account(addr)}'>{name}</a> установлен порог в {amount} для получения уведомлений. Вы будете получать уведомление о транзакциях на сумму более {amount}";
                default:
                    return $"For <a href='{t.account(addr)}'>{name}</a> transaction amount threshold of {amount} was set. You will receive notifications about transactions above {amount}";
            }
		}
		internal static string DlgThresholdEstablished(User u, string addr, string name, string amount)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"Для <a href='{t.account(addr)}'>{name}</a> установлен порог в {amount} для получения уведомлений. Вы будете получать уведомление о делегированиях на сумму более {amount}";
				default:
					return $"For <a href='{t.account(addr)}'>{name}</a> delegation amount threshold {amount} was set. You will receive notifications about delegations above {amount}";
			}
		}
		internal static string AddressRenamed(User u, string addr, string name)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"Для адреса <a href='{t.account(addr)}'>{addr}</a> установлено название {name}";
				default:
					return $"Address <a href='{t.account(addr)}'>{addr}</a> named as {name}.";
			}
		}

		internal static string NetworkIssueAlertSet(User u, int minutes)
		{
			if (minutes == 0)
			{
				switch (u.Language)
				{
					case "ru":
						return $"Уведомления о сбоях сети отключены";
					default:
						return $"Network failure notifications disabled";
				}
			}

			switch (u.Language)
			{
				case "ru":
					return $"Установлено время простоя сети до уведомления: {minutes} минут";
				default:
					return $"Network downtime prior to notification: {minutes} minutes";
			}

		}
		internal static string RewardNotifications(User u, bool on)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Вознаграждения пекаря: " + (on ? "🔔 вкл" : "🔕 выкл");
                default:
                    return "Baker rewards: " + (on ? "🔔 on" : "🔕 off");
            }
        }
		internal static string Events(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "События: ";
				default:
					return "Events: ";
			}
		}
        internal static string TransactionNotifications(User u, bool on)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Транзакции: " + (on ? "🔔 вкл" : "🔕 выкл");
                default:
                    return "Transactions: " + (on ? "🔔 on" : "🔕 off");
            }
		}
		internal static string DelegationNotifications(User u, bool on)
		{
			switch (u.Language)
			{
				case "ru":
					return "Делегирования: " + (on ? "🔔 вкл" : "🔕 выкл");
				default:
					return "Delegations: " + (on ? "🔔 on" : "🔕 off");
			}
		}
		internal static string CycleCompletionNotifications(User u, bool on)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Завершения циклов: " + (on ? "🔔 вкл" : "🔕 выкл");
                default:
                    return "Cycle completions: " + (on ? "🔔 on" : "🔕 off");
            }
		}
		internal static string MissesNotifications(User u, bool on)
		{
			switch (u.Language)
			{
				case "ru":
					return "Пропуски пекаря: " + (on ? "🔔 вкл" : "🔕 выкл");
				default:
					return "Missed baking/endorsing: " + (on ? "🔔 on" : "🔕 off");
			}
		}
        internal static string Added(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Добавлен";
                default:
                    return "Added";
            }
        }
        internal static string As(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "под названием";
                default:
                    return "as";
            }
        }
        internal static string YouWillReceive(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Вы будете получать уведомления по всем транзакциям.";
                default:
                    return "You will receive notifications on any transactions.";
            }
        }
		internal static string CurrentBalance(User u, decimal balance, Tezos.MarketData md)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"Доступный баланс: <b>{balance.TezToString()}</b> ({balance.TezToUsd(md)} USD / {balance.TezToBtc(md)} BTC)\n";
                default:
                    return $"Spendable Balance: <b>{balance.TezToString()}</b> ({balance.TezToUsd(md)} USD / {balance.TezToBtc(md)} BTC)\n";
            }
        }
		internal static string ActualBalance(User u, decimal balance, Tezos.MarketData md)
        {
			if (md == null)
			{
				switch (u.Language)
				{
					case "ru":
						return $"Полный баланс: <b>{balance.TezToString()}</b>\n";
					default:
						return $"Full Balance: <b>{balance.TezToString()}</b>\n";
				}
			}
            switch (u.Language)
            {
                case "ru":
                    return $"Полный баланс: <b>{balance.TezToString()}</b> ({balance.TezToUsd(md)} USD / {balance.TezToBtc(md)} BTC)\n";
                default:
                    return $"Full Balance: <b>{balance.TezToString()}</b> ({balance.TezToUsd(md)} USD / {balance.TezToBtc(md)} BTC)\n";
            }
        }
		internal static string StakingBalance(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Выпекающий капитал";
                default:
                    return "Staking Balance";
            }
		}
		internal static string NotifyIn(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "Группа: ";
				default:
					return "Group: ";
			}
		}
		internal static string Delegators(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "делегаторов";
				default:
					return "delegators";
			}
		}
		internal static string Rolls(User u)
		{
			switch (u.Language)
			{
				case "ru":
					return "роллов";
				default:
					return "rolls";
			}
		}
		internal static string FreeSpace(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Доступно для делегирования";
                default:
                    return "Free delegation space";
            }
        }
        internal static string ClarifyBeforeDelegation(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "прибл. сумма, уточняйте перед делегированием";
                default:
                    return "approx. amount, clarify before delegation";
            }
        }
        internal static string FreeSpaceOverdelegated(User u, decimal free)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"Доступно для делегирования: <b>-{Math.Abs(free).TezToString()} (переделегировано❗️)</b>";
                default:
                    return $"Free delegation space: <b>-{Math.Abs(free).TezToString()} (overdelegated❗️)</b>";
            }
        }
        internal static string AveragePerformance(User u, int cycleCount, string performance)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"Ср. эффективность за {cycleCount} циклов: <b>{performance}%</b>";
                default:
                    return $"Avg. {cycleCount}-cycles performance: <b>{performance}%</b>";
            }
        }
		internal static string Delegate(User u)
        {
            switch (u.Language)
            {
                case "ru":
                    return "Делегат";
                default:
                    return "Delegate";
            }
        }
		internal static string Overdelegated(User u, string addr)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"❗️ Делегат <a href='{t.account(addr)}'>{addr}</a> переделегирован";
                default:
                    return $"❗️ Delegate <a href='{t.account(addr)}'>{addr}</a> is overdelegated";
            }
        }
		internal static string QuorumReached(User u, string proposal, string proposalname)
		{			
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"🎉 Кворум по предложению <a href='{t.url_vote(proposal)}'>{proposalname}</a> достигнут!\n";
				default:
					return $"🎉 Quorum on the proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> reached!\n";
			}
		}
		internal static string TestingVoteFailed(User u, string proposal, string proposalname)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"😐 Предложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> отклонено. Начинается период подачи предложений.\n";
				default:
					return $"😐 Proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> declined. The proposal period begins.\n";
			}
		}
		internal static string TestingVoteSuccess(User u, string proposal, string proposalname)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"✌️ Предложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> одобрено. Начинается период тестирования.\n";
				default:
					return $"✌️ Proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> approved. Testing period begins.\n";
			}
		}
		internal static string PromotionVoteFailed(User u, string proposal, string proposalname)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"🚽 Предложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> отклонено. Начинается период подачи предложений.\n";
				default:
					return $"🚽 Proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> declined. The proposal period begins.\n";
			}
		}
		internal static string PromotionVoteSuccess(User u, string proposal, string proposalname)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"🥳 Предложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> одобрено. Новый протокол внедрен. Начинается период подачи предложений.\n";
				default:
					return $"🥳 Proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> approved. New protocol implemented. The proposal period begins.\n";
			}
		}
		internal static string NewProposal(User u, string hash, string from, string fromName, string proposal, string proposalname, int rolls)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"💡 <a href='{t.op(hash)}'>Поступило</a> новое предложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> по модификации протокола от <a href='{t.account(from)}'>{fromName}</a> с {rolls} роллами\n";
				default:
					return $"💡 <a href='{t.op(hash)}'>Injected</a> new proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> by <a href='{t.account(from)}'>{fromName}</a> with {rolls} rolls\n";
			}
		}
		internal static string SupplyProposal(User u, string hash, string from, string fromName, string proposal, string proposalname, int rolls, int allrolls, int votedrolls)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"👍 Делегат <a href='{t.account(from)}'>{fromName}</a> с {rolls} роллами <a href='{t.op(hash)}'>поддержал</a> предложение <a href='{t.url_vote(proposal)}'>{proposalname}</a>\n\n{votedrolls} роллов поддержало, {(100 * votedrolls / allrolls).ToString("n1")}% от общего количества\n";
				default:
					return $"👍 Delegate <a href='{t.account(from)}'>{fromName}</a> with {rolls} rolls <a href='{t.op(hash)}'>upvoted</a> proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a>\n\n{votedrolls} rolls voted, {(100 * votedrolls / allrolls).ToString("n1")}% of total rolls\n";
			}
		}
		internal static string DelegateDidNotVoted(User u, string from, string fromName, string proposal, string proposalname)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"😴 Делегат <a href='{t.account(from)}'>{fromName}</a> не проголосовал по предложению <a href='{t.url_vote(proposal)}'>{proposalname}</a>\n";
				default:
					return $"😴 Delegate <a href='{t.account(from)}'>{fromName}</a> didn't vote on proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a>\n";
			}
		}
		internal static string BallotProposal(User u, string hash, string from, string fromName, string proposal, string proposalname, int rolls, int allrolls, string ballot)
		{
			string icon = "🙆‍♂️";
			if (ballot == "yay")
				icon = "🙋‍♂️";
			if (ballot == "nay")
				icon = "🙅‍♂️";
			switch (u.Language)
			{
				case "ru":
					switch (ballot)
					{
						case "yay":
							ballot = "ДА";
							break;
						case "nay":
							ballot = "НЕТ";
							break;
						default:
							ballot = "ПАС";
							break;
					}
					break;
				default:
					ballot = ballot.ToUpper();
					break;
			}
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"{icon} Делегат <a href='{t.account(from)}'>{fromName}</a> с {rolls} роллами <a href='{t.op(hash)}'>проголосовал</a> «{ballot}» по предложению <a href='{t.url_vote(proposal)}'>{proposalname}</a>\n";
				default:
					return $"{icon} Delegate <a href='{t.account(from)}'>{fromName}</a> with {rolls} rolls <a href='{t.op(hash)}'>voted</a> «{ballot}» on the proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a>\n";
			}
		}
		internal static string ProposalSelectedOne(User u, string proposal, string proposalname, string delegateList)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			if (delegateList != "")
			{
				switch (u.Language)
				{
					case "ru":
						return $"💡Завершен период подачи предложений. Предложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> поддержали: {delegateList}\n\nПредложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> ставится на голосование";
					default:
						return $"💡 Proposal period has been completed. Proposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> supported by: {delegateList}\n\nProposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> is selected for voting";
				}
			}
			else
			{
				switch (u.Language)
				{
					case "ru":
						return $"💡 Завершен период подачи предложений.\n\nПредложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> выносится на голосование";
					default:
						return $"💡 Proposal period has been completed.\n\nProposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> is selected for voting";
				}
			}
		}
		internal static string ProposalSelectedMany(User u, string proposal, string proposalname, int count, string items)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"💡 Завершен период подачи предложений. Было подано {count} предложения:\n{items}\nПредложение <a href='{t.url_vote(proposal)}'>{proposalname}</a> выносится на голосование";
				default:
					return $"💡 Proposal period has been completed. {count} proposals have been submitted:\n{items}\nProposal <a href='{t.url_vote(proposal)}'>{proposalname}</a> is selected for voting";
			}
		}

		internal static string ProposalSelectedItem(User u, string proposal, string proposalname, int rolls, string delegateList)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			if (delegateList != "")
			{
				switch (u.Language)
				{
					case "ru":
						return $"<a href='{t.url_vote(proposal)}'>{proposalname}</a> - {rolls} роллов, поддержали: {delegateList}\n";
					default:
						return $"<a href='{t.url_vote(proposal)}'>{proposalname}</a> - {rolls} rolls, supported by: {delegateList}\n";
				}
			}
			else
			{
				switch (u.Language)
				{
					case "ru":
						return $"<a href='{t.url_vote(proposal)}'>{proposalname}</a> - {rolls} роллов\n";
					default:
						return $"<a href='{t.url_vote(proposal)}'>{proposalname}</a> - {rolls} rolls\n";
				}
			}
		}
		internal static string IncomingTransactions(User u, int block, string amount, string to, string toName)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"✅ Входящие <a href='{t.block(block)}'>транзакции</a> на сумму <b>{amount}</b> к <a href='{t.account(to)}'>{toName}</a>:\n";
				default:
					return $"✅ Incoming <a href='{t.block(block)}'>transactions</a> of <b>{amount}</b> to <a href='{t.account(to)}'>{toName}</a>:\n";
			}
		}
		internal static string IncomingTransaction(User u, string hash, decimal amount, string from, string fromName, string to, string toName, Tezos.MarketData md)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"✅ Входящая <a href='{t.op(hash)}'>транзакция</a> <b>{amount.TezToString()} ({amount.TezToUsd(md)} USD)</b> к <a href='{t.account(to)}'>{toName}</a> от <a href='{t.account(from)}'>{fromName}</a>\n";
				default:
					return $"✅ Incoming <a href='{t.op(hash)}'>transaction</a> of <b>{amount.TezToString()} ({amount.TezToUsd(md)} USD)</b> to <a href='{t.account(to)}'>{toName}</a> from <a href='{t.account(from)}'>{fromName}</a>\n";
			}
		}
		internal static string OutgoingTransactions(User u, int block, string amount, string from, string fromName)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"❎ Исходящие <a href='{t.block(block)}'>транзакции</a> на сумму <b>{amount}</b> от <a href='{t.account(from)}'>{fromName}</a>:\n";
				default:
					return $"❎ Outgoing <a href='{t.block(block)}'>transactions</a> of <b>{amount}</b> from <a href='{t.account(from)}'>{fromName}</a>:\n";
			}
		}
		internal static string OutgoingTransaction(User u, string hash, decimal amount, string from, string fromName, string to, string toName, Tezos.MarketData md)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"❎ Исходящая <a href='{t.op(hash)}'>транзакция</a> <b>{amount.TezToString()} ({amount.TezToUsd(md)} USD)</b> от <a href='{t.account(from)}'>{fromName}</a> к <a href='{t.account(to)}'>{toName}</a>\n";
				default:
					return $"❎ Outgoing <a href='{t.op(hash)}'>transaction</a> of <b>{amount.TezToString()} ({amount.TezToUsd(md)} USD)</b> from <a href='{t.account(from)}'>{fromName}</a> to <a href='{t.account(to)}'>{toName}</a>\n";
			}
		}
		internal static string WhaleTransaction(User u, string hash, decimal amount, string fromLink, string toLink, Tezos.MarketData md)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"🐋 Крупная <a href='{t.op(hash)}'>транзакция</a> <b>{amount.TezToString()} ({amount.TezToUsd(md)} USD)</b> от {fromLink} к {toLink}\n";
				default:
					return $"🐋 Whale <a href='{t.op(hash)}'>transaction</a> of <b>{amount.TezToString()} ({amount.TezToUsd(md)} USD)</b> from {fromLink} to {toLink}\n";
			}
		}
		internal static string From(User u, string from, string fromName)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"от <a href='{t.account(from)}'>{fromName}</a>";
				default:
					return $"from <a href='{t.account(from)}'>{fromName}</a>";
			}
		}
		internal static string To(User u, string to, string toName)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"к <a href='{t.account(to)}'>{toName}</a>";
				default:
					return $"to <a href='{t.account(to)}'>{toName}</a>";
			}
		}
		internal static string NotAllShown(User u, string hash)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"ℹ️ Не все операции показаны, полный список <a href='{t.op(hash)}'>здесь</a>\n";
				default:
					return $"ℹ️ Not all operations are shown, full list <a href='{t.op(hash)}'>here</a>\n";
			}
		}
		internal static string DoubleBakingOccured(User u, string ophash, string offenderAddress, string offenderName, string lost, int block)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"😱 Делегат <a href='{t.account(offenderAddress)}'>{offenderName}</a> осуществил <a href='{t.op(ophash)}'>двойную выпечку/заверение</a> блока <a href='{t.block(block)}'>{block}</a> и потерял <b>{lost}</b>\n🛑 Бейкер должен немедленно прекратить выпечку и заверение до конца цикла\n";
				default:
					return $"😱 Delegate <a href='{t.account(offenderAddress)}'>{offenderName}</a> made <a href='{t.op(ophash)}'>double baking/endorsement</a> of block <a href='{t.block(block)}'>{block}</a> and lost <b>{lost}</b>\n🛑 Baker should immediately stop both baking and endorsing for the rest of cycle\n";
			}
		}
		internal static string DoubleBakingEvidence(User u, string ophash, string bakerAddress, string bakerName, string rewards, int block)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"👮 Делегат <a href='{t.account(bakerAddress)}'>{bakerName}</a> обнаружил <a href='{t.op(ophash)}'>двойную выпечку/заверение</a> блока <a href='{t.block(block)}'>{block}</a> и получил награду <b>{rewards}</b>\n";
				default:
					return $"👮 Delegate <a href='{t.account(bakerAddress)}'>{bakerName}</a> detected <a href='{t.op(ophash)}'>double baking/endorsement</a> of block <a href='{t.block(block)}'>{block}</a> and rewarded <b>{rewards}</b>\n";
			}
		}
		internal static string SkippedEndorsing(User u, string name, string address, int level, string hash)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"😯 Делегат <a href='{t.account_baking(address)}'>{name}</a> не будет вознагражден за заверение блока <a href='{t.block(level)}'>{level}</a> по причине отсутствия заверяющих операций в блоке <a href='{t.block(level + 1)}'>{level + 1}</a>";
                default:
                    return $"😯 Delegate <a href='{t.account_baking(address)}'>{name}</a> shall not be rewarded for block <a href='{t.block(level)}'>{level}</a> endorsing due to lack of endorsment operations in block <a href='{t.block(level + 1)}'>{level + 1}</a>";
            }
        }
		internal static string NewDelegation(User u, string hash, string amount, string from, string fromName, string to, string toName)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"🤝 Новое <a href='{t.op(hash)}'>делегирование</a> <b>{amount}</b> к <a href='{t.account(to)}'>{toName}</a> от <a href='{t.account(from)}'>{fromName}</a>\n";
                default:
                    return $"🤝 New <a href='{t.op(hash)}'>delegation</a> of <b>{amount}</b> to <a href='{t.account(to)}'>{toName}</a> from <a href='{t.account(from)}'>{fromName}</a>\n";
            }
        }
		internal static string UnDelegation(User u, string from, string fromName, string to, string toName, string amount)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"👋 Делегатор <a href='{t.account(from)}'>{fromName}</a> с балансом <b>{amount}</b> покинул делегата <a href='{t.account(to)}'>{toName}</a>\n";
                default:
                    return $"👋 Delegator <a href='{t.account(from)}'>{fromName}</a> with balance <b>{amount}</b> left delegate <a href='{t.account(to)}'>{toName}</a>\n";
            }
        }
        internal static string StoleBaking(User u, string name, string address, int level, string hash, int priority, string reward)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"😎 Делегат <a href='{t.account_baking(address)}'>{name}</a> выпек блок <a href='{t.block(level)}'>{level}</a> с приоритетом {priority} и получает дополнительную награду {reward}";
                default:
                    return $"😎 Delegate <a href='{t.account_baking(address)}'>{name}</a> stole block <a href='{t.block(level)}'>{level}</a> baking (priority {priority}) and gets extra reward {reward}";
            }
        }
        internal static string NetworkIssue(User u, int block, int minutes)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
			{
				case "ru":
					return $"⚠ Сеть Tezos, возможно, испытывает проблемы.\n\nПоследний блок <a href='{t.block(block)}'>{block}</a> испечён {minutes} минут назад.\n\nВсем бейкерам необходимо проверить свой софт";
				default:
					return $"⚠ Probably Tezos network is experiencing problems.\n\nLast block <a href='{t.block(block)}'>{block}</a> baked {minutes} minutes ago.\n\nAll bakers need to check their software";
			}
		}		
		internal static string MissedBaking(User u, string name, string address, int level, string hash, string reward, string lowBalanceAmount)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"🤷🏻‍♂️ Делегат <a href='{t.account_baking(address)}'>{name}</a> пропустил выпечку блока <a href='{t.block(level)}'>{level}</a>{LowBalance(u, lowBalanceAmount)} и не получает награду {reward}";
                default:
                    return $"🤷🏻‍♂️ Delegate <a href='{t.account_baking(address)}'>{name}</a> missed baking block <a href='{t.block(level)}'>{level}</a>{LowBalance(u, lowBalanceAmount)} and does not receive a reward {reward}";
            }
		}
		internal static string LowBalance(User u, string amount)
		{
			if (amount == null)
				return "";
			switch (u.Language)
			{
				case "ru":
					return $" из-за нехватки средств (текущий баланс {amount})";
				default:
					return $" due to lack of funds (current balance {amount})";
			}
		}
		internal static string MissedEndorsing(User u, string name, string address, int level, string hash, string reward, string lowBalanceAmount)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"🤷🏻‍♂️ Делегат <a href='{t.account_baking(address)}'>{name}</a> пропустил заверение блока <a href='{t.block(level)}'>{level}</a>{LowBalance(u, lowBalanceAmount)} и не получает награду {reward}";
                default:
                    return $"🤷🏻‍♂️ Delegate <a href='{t.account_baking(address)}'>{name}</a> missed endorsement for block <a href='{t.block(level)}'>{level}</a>{LowBalance(u, lowBalanceAmount)} and does not receive a reward {reward}";
            }
        }
		internal static string CycleCompleted(User u, int cycle)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"🏁 Цикл {cycle} завершен!";
                default:
                    return $"🏁 Cycle {cycle} is completed!";
            }
        }
        internal static string RewardDelivered(User u, int cycle)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"💰 Награды за цикл {cycle} выплачены делегатам!\n\n";
                default:
                    return $"💰 Rewards for cycle {cycle} delivered to delegates!\n\n";
            }
		}
        internal static string Accrued(User u, int cycle, string tez)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"Начислено за цикл {cycle}: <b>{tez}</b>";
                default:
                    return $"Accrued per cycle {cycle}: <b>{tez}</b>";
            }
        }
        
		internal static string RewardDeliveredItem(User u, string name, string tezAmount)
		{
			switch (u.Language)
			{
				case "ru":
					return $"<b>{tezAmount}</b> для {name}.";
				default:
					return $"<b>{tezAmount}</b> to {name}.";
			}
		}
		internal static string Performance(User u, string addr, string name, string perfomance)
		{
			var t = new ExplorerUrl((ExplorerUrl.Type)u.Explorer);
			switch (u.Language)
            {
                case "ru":
                    return $"Эффективность <a href='{t.account(addr)}'>{name}</a> составила <b>{perfomance}%</b>";
                default:
                    return $"Performance of <a href='{t.account(addr)}'>{name}</a> is <b>{perfomance}%</b>";
            }
        }

		internal static string DelegatePerformance(User u, int cycle, string performance)
        {
            switch (u.Language)
            {
                case "ru":
                    return $"Эффективность за цикл {cycle}: <b>{performance}%</b>";
                default:
                    return $"Cycle {cycle} performance: <b>{performance}%</b>";
            }
        }
		*/


	}
}
