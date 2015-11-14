using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamTrade;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdOrders : IChatCommand
	{
		public string CommandName => "orders";

		public bool IsAdminOnly => true;

		public string Purpose => "Modify orders in various ways";

		public string Syntax => "orders {add | remove | set | list | help} [args...]";

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			if (args.Count == 0)
			{
				sendChatMessage("No subcommand supplied.");
				return false;
			}

			string subcommand = args[0];
			args.RemoveAt(0);

			switch (subcommand.ToLower())
			{
			case "add":
				return _ordersAdd(args, handler, sendChatMessage);
			case "remove":
				return _ordersRemove(args, handler, sendChatMessage);
			case "set":
				return _ordersSet(args, handler, sendChatMessage);
			case "list":
				_ordersList(args, handler, sendChatMessage);
				return true;
			case "help":
				sendChatMessage("orders add {sell | buy} {defindex} {quality} {price}");
				sendChatMessage("orders remove {sell | buy} {defindex}");
				sendChatMessage("orders set {sell | buy} {defindex} {quality} {price}");
				sendChatMessage("orders list [sell | buy]");
				sendChatMessage("orders help");
				return true;
			default:
				sendChatMessage("Invalid subcommand: " + subcommand);
				return false;
			}
		}

		private bool _ordersAdd(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			if (args.Count < 4)
			{
				sendChatMessage("Syntax: orders add {sell | buy} {defindex} {quality} {price}");
				return false;
			}
			bool sell = args[0].ToLower() == "sell";

			string strDefindex = args[1];
			int defindex;
			if (!int.TryParse(strDefindex, out defindex))
			{
				sendChatMessage("Invalid defindex: " + strDefindex);
				return false;
			}
			Schema.Item item = Trade.CurrentSchema.GetItem(defindex);

			string strQuality = args[2];
			int quality;
			if (!int.TryParse(strQuality, out quality))
			{
				sendChatMessage("Invalid quality id: " + strQuality);
				return false;
			}

			string strPrice = args[3];
			double priceRef;
			if (!double.TryParse(strPrice, out priceRef))
			{
				sendChatMessage("Invalid price value: " + strPrice);
				return false;
			}
			TF2Value price = TF2Value.FromRef(priceRef);

			if (sell)
			{
				Order so = handler.Bot.Orders.SellOrders.FirstOrDefault((o) => o.Defindex == defindex && o.Quality == quality);
				if (so != null)
				{
					sendChatMessage("Order already exists selling for " + so.Price.ToRefString());
					return false;
				}

				Order added = new Order(price, item, quality, 3, true, false, false, false);
				handler.Bot.Orders.SellOrders.Add(added);
				sendChatMessage("Sell order added: " + added.ToString(Trade.CurrentSchema));
			}
			else
			{
				Order bo = handler.Bot.Orders.BuyOrders.FirstOrDefault((o) => o.Defindex == defindex && o.Quality == quality);
				if (bo != null)
				{
					sendChatMessage("Order already exists buying for " + bo.Price.ToRefString());
					return false;
				}

				Order added = new Order(price, item, quality, 3, true, false, false, true);
				handler.Bot.Orders.BuyOrders.Add(added);
				sendChatMessage("Buy order added: " + added.ToString(Trade.CurrentSchema));
			}

			handler.Bot.Orders.SaveAll();
			return true;
		}

		private bool _ordersRemove(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			if (args.Count < 2)
			{
				sendChatMessage("Syntax: orders remove {sell | buy} {defindex}");
				return false;
			}
			bool sell = args[0].ToLower() == "sell";

			string strDefindex = args[1];
			int defindex;
			if (!int.TryParse(strDefindex, out defindex))
			{
				sendChatMessage("Invalid defindex: " + strDefindex);
				return false;
			}

			if (sell)
			{
				Order so = handler.Bot.Orders.SellOrders.FirstOrDefault((o) => o.Defindex == defindex);
				if (so == null)
				{
					sendChatMessage("Sell order does not exist for defindex " + defindex.ToString());
				}

				handler.Bot.Orders.SellOrders.Remove(so);
				sendChatMessage("Sell order removed.");
			}
			else
			{
				Order bo = handler.Bot.Orders.BuyOrders.FirstOrDefault((o) => o.Defindex == defindex);
				if (bo == null)
				{
					sendChatMessage("Buy order does not exist for defindex " + defindex.ToString());
				}

				handler.Bot.Orders.BuyOrders.Remove(bo);
				sendChatMessage("Buy order removed.");
			}

			handler.Bot.Orders.SaveAll();
			return true;
		}

		private bool _ordersSet(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			if (args.Count < 4)
			{
				sendChatMessage("Syntax: orders set {sell | buy} {defindex} {quality} {price}");
				return false;
			}
			bool sell = args[0].ToLower() == "sell";

			string strDefindex = args[1];
			int defindex;
			if (!int.TryParse(strDefindex, out defindex))
			{
				sendChatMessage("Invalid defindex: " + strDefindex);
				return false;
			}
			Schema.Item item = Trade.CurrentSchema.GetItem(defindex);

			string strQuality = args[2];
			int quality;
			if (!int.TryParse(strQuality, out quality))
			{
				sendChatMessage("Invalid quality id: " + strQuality);
				return false;
			}

			string strPrice = args[3];
			double priceRef;
			if (!double.TryParse(strPrice, out priceRef))
			{
				sendChatMessage("Invalid price value: " + strPrice);
				return false;
			}

			if (sell)
			{
				Order so = handler.Bot.Orders.SellOrders.FirstOrDefault((o) => o.Defindex == defindex && o.Quality == quality);
				if (so == null)
				{
					sendChatMessage("Sell order does not exist for defindex " + defindex.ToString());
					return false;
				}

				so.PriceRef = priceRef;
				sendChatMessage("Sell order updated.");
			}
			else
			{
				Order bo = handler.Bot.Orders.BuyOrders.FirstOrDefault((o) => o.Defindex == defindex && o.Quality == quality);
				if (bo == null)
				{
					sendChatMessage("Buy order does not exist for defindex " + defindex.ToString());
					return false;
				}

				bo.PriceRef = priceRef;
				sendChatMessage("Buy order updated.");
			}

			handler.Bot.Orders.SaveAll();
			return true;
		}

		private void _ordersList(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			bool? sell = null;
			if (args.Count > 0)
			{
				sell = args[0].ToLower() == "sell";
			}

			sendChatMessage("Current orders: ");
			foreach (Order o in handler.Bot.Orders.AllOrders)
			{
				if (sell != null && sell.Value == o.IsBuyOrder)
				{
					continue;
				}

				sendChatMessage(" - " + o.ToString(Trade.CurrentSchema, true));
			}
		}
	}
}
