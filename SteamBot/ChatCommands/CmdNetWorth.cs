using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamTrade;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdNetWorth : IChatCommand
	{
		public string CommandName => "networth";

		public bool IsAdminOnly => true;

		public string Purpose => "Returns the net worth of the bot's pure and selling items";

		public string Syntax => "networth";

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			try
			{
				handler.Bot.GetInventory();

				TF2Value total = TF2Value.Zero;
				foreach (Inventory.Item item in handler.Bot.MyInventory.Items)
				{
					if (item.IsNotTradeable)
					{
						continue;
					}

					if (item.Defindex == TF2Value.KEY_DEFINDEX)
						total += TF2Value.Key;
					if (item.Defindex == TF2Value.REFINED_DEFINDEX)
						total += TF2Value.Refined;
					if (item.Defindex == TF2Value.RECLAIMED_DEFINDEX)
						total += TF2Value.Reclaimed;
					if (item.Defindex == TF2Value.SCRAP_DEFINDEX)
						total += TF2Value.Scrap;

					Order order = handler.Bot.Orders.SellOrders.FirstOrDefault((o) => o.MatchesItem(item));
					if (order != null)
					{
						total += order.Price;
						continue;
					}
				}

				sendChatMessage("Net worth: " + total.ToRefString());

				return true;
			}
			catch (Exception e)
			{
				handler.Log.Error("An error occurred during the {0} command: {1}", CommandName, e.Message);
				return false;
			}
		}
	}
}
