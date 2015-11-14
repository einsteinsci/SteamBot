using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdHelp : IChatCommand
	{
		public string CommandName => "help";
		public string Purpose => "Lists all available commands";
		public string Syntax => "help [command]";

		public bool IsAdminOnly => false;

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendMsg)
		{
			if (args.Count > 0)
			{
				string cmdName = args[0].ToLower();

				foreach (IChatCommand cmd in ChatHandler.ChatCommands)
				{
					if (cmd.CommandName == cmdName)
					{
						sendMsg(cmd.Syntax);
						return true;
					}
				}
				return false;
			}

			foreach (IChatCommand cmd in ChatHandler.ChatCommands)
			{
				string result = cmd.Syntax + ": " + cmd.Purpose;
				if (cmd.IsAdminOnly && handler.IsAdmin)
				{
					result = "[ADMIN] " + result;
				}

				sendMsg(result);
			}
			return true;
		}
	}
}
