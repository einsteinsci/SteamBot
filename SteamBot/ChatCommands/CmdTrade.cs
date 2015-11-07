using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdTrade : IChatCommand
	{
		public string CommandName => "trade";
		public string Purpose => "Sends you a trade request.";
		public string Syntax => CommandName;

		public bool IsAdminOnly => false;

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMsg)
		{
			if (handler.Trade != null)
			{
				handler.Log.Warn("User asked for trade during another trade.");
				sendChatMsg("I am currently in the middle of a trade. Please try again later.");
				
				return false;
			}

			sendChatMsg("I am sending you a trade request...");
			handler.Bot.OpenTrade(handler.OtherSID);
			return true;
		}
	}
}
