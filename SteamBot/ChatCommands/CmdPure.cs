using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamTrade;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdPure : IChatCommand
	{
		public string CommandName => "pure";

		public bool IsAdminOnly => false;

		public string Purpose => "Counts amount of pure in stock";

		public string Syntax => CommandName;

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			TF2Value total = TF2Value.Zero;

			try
			{
				handler.Bot.GetInventory();
				Inventory inv = handler.Bot.MyInventory;

				List<Inventory.Item> keys = inv.GetItemsByDefindex(TF2Value.KEY_DEFINDEX);
				List<Inventory.Item> refi = inv.GetItemsByDefindex(TF2Value.REFINED_DEFINDEX);
				List<Inventory.Item> rec = inv.GetItemsByDefindex(TF2Value.RECLAIMED_DEFINDEX);
				List<Inventory.Item> scrap = inv.GetItemsByDefindex(TF2Value.SCRAP_DEFINDEX);

				total += TF2Value.Key * keys.Count;
				total += TF2Value.Refined * refi.Count;
				total += TF2Value.Reclaimed * rec.Count;
				total += TF2Value.Scrap * scrap.Count;
			}
			catch (Exception e)
			{
				handler.Log.Error("An error occurred during the {0} command: {1}", CommandName, e.Message);
				return false;
			}

			sendChatMessage(string.Format("I currently have {0} in pure.", total.ToRefString()));
			return true;
		}
	}
}
