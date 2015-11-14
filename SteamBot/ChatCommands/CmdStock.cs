using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamTrade;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdStock : IChatCommand
	{
		public string CommandName => "stock";

		public bool IsAdminOnly => false;

		public string Purpose => "Lists the bot's current stock of an item.";

		public string Syntax => "stock [itemname]";

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			string itemname = null;
			if (args.Count > 0)
			{
				itemname = string.Join(" ", args).ToLower();
			}

			try
			{
				handler.Bot.GetInventory();
				Schema schema = Trade.CurrentSchema;

				List<_stockInfo> stock = new List<_stockInfo>();
				foreach (Inventory.Item i in handler.Bot.MyInventory.Items)
				{
					if (i.IsNotTradeable)
					{
						continue;
					}

					if (i.Defindex == TF2Value.SCRAP_DEFINDEX || 
						i.Defindex == TF2Value.RECLAIMED_DEFINDEX ||
						i.Defindex == TF2Value.REFINED_DEFINDEX ||
						i.Defindex == TF2Value.KEY_DEFINDEX)
					{
						continue;
					}

					Schema.Item item = schema.GetItem(i.Defindex);
					_stockInfo inf = stock.FirstOrDefault((_i) => _i.Item == item);
					if (inf != null)
					{
						inf.Count++;
					}
					else
					{
						Order b = handler.Bot.Orders.BuyOrders.FirstOrDefault((_o) => _o.Defindex == item.Defindex);
						Order s = handler.Bot.Orders.SellOrders.FirstOrDefault((_o) => _o.Defindex == item.Defindex);
						stock.Add(new _stockInfo(item, i.Quality, 1, b, s));
					}
				}

				if (itemname == null)
				{
					sendChatMessage("I currently have the following items in stock:");
					foreach (_stockInfo inf in stock)
					{
						sendChatMessage("> " + inf.ToString());
					}
				}
				else
				{
					stock.RemoveAll((si) => !si.FullName.ToLower().Contains(itemname));

					if (stock.Count == 0)
					{
						sendChatMessage("I do not have any items in stock that fit your search query.");
					}
					else
					{
						sendChatMessage("I currently have the following items in stock matching '" + itemname + "':");
						foreach (_stockInfo inf in stock)
						{
							sendChatMessage("> " + inf.ToString());
						}
					}
				}

				return true;
			}
			catch (Exception e)
			{
				handler.Log.Error("An error occurred during the {0} command: {1}", CommandName, e.Message);
				return false;
			}
		}

		private class _stockInfo
		{
			public Schema.Item Item
			{ get; private set; }
			public int Count
			{ get; set; }
			public Order BuyOrder
			{ get; private set; }
			public Order SellOrder
			{ get; private set; }
			public int Quality
			{ get; private set; }

			public string FullName => Order.GetQualityString(Quality) + Item.ItemName;

			public _stockInfo(Schema.Item item, int quality, int count, Order buy, Order sell)
			{
				Item = item;
				Quality = quality;
				Count = count;
				BuyOrder = buy;
				SellOrder = sell;
			}

			public override string ToString()
			{
				string res = Order.GetQualityString(Quality) + Item.ItemName + " x" + Count.ToString();
				
				if (SellOrder != null)
				{
					res += " [SELLING for " + SellOrder.Price.ToRefString() + "]";
				}
				if (BuyOrder != null)
				{
					if (BuyOrder.MaxStock > Count)
					{
						res += " [BUYING for " + BuyOrder.Price.ToRefString() + "]";
					}
					else
					{
						res += " [FULL STOCK. Not buying.]";
					}
				}

				return res;
			}
		}
	}
}
