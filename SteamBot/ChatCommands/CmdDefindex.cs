using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamTrade;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdDefindex : IChatCommand
	{
		public string CommandName => "defindex";

		public bool IsAdminOnly => false;

		public string Purpose => "Get the game schema defindex of an item";

		public string Syntax => "defindex {query}";

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			if (args.Count == 0)
			{
				sendChatMessage("No query supplied.");
				return false;
			}

			string query = string.Join(" ", args).ToLower();

			List<Schema.Item> matches = new List<Schema.Item>();
			foreach (Schema.Item item in Trade.CurrentSchema.Items)
			{
				if (item.ItemName.ToLower().Contains(query))
				{
					matches.Add(item);
				}
			}

			sendChatMessage("Matching items:");
			foreach (Schema.Item i in matches)
			{
				sendChatMessage("> " + i.ItemName + " (#" + i.Defindex.ToString() + ")");
			}
			return true;
		}
	}
}
