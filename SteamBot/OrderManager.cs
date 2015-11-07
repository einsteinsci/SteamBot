using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamTrade;
using SteamTrade.TradeOffer;

namespace SteamBot
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class OrderManager
	{
		public static readonly string ORDERS_FILENAME = "orders.json";

		[JsonProperty]
		public List<Order> BuyOrders
		{ get; private set; }

		[JsonProperty]
		public List<Order> SellOrders
		{ get; private set; }

		public List<Order> AllOrders
		{
			get
			{
				List<Order> res = new List<Order>();

				res.AddRange(SellOrders);
				res.AddRange(BuyOrders);

				return res;
			}
		}

		public OrderManager()
		{
			BuyOrders = new List<Order>();
			SellOrders = new List<Order>();
		}

		public bool HasMatchingTrade(UserHandler handler, TradeOffer trade)
		{
			return BuyOrders.Exists((o) => o.TradeOfferMatches(handler, trade)) || 
				SellOrders.Exists((o) => o.TradeOfferMatches(handler, trade));
		}

		public static OrderManager Load(Log logger)
		{
			string filepath = Path.Combine(BotManager.DATA_FOLDER, ORDERS_FILENAME);
			Directory.CreateDirectory(BotManager.DATA_FOLDER);

			OrderManager res = new OrderManager();
			if (File.Exists(filepath))
			{
				try
				{
					string contents = File.ReadAllText(filepath);
					res = JsonConvert.DeserializeObject<OrderManager>(contents);
					return res;
				}
				catch (Exception e)
				{
					logger.Error(e.Message + "\n" + e.StackTrace);
				}
			}
			
			string json = JsonConvert.SerializeObject(res, Formatting.Indented);

			if (!File.Exists(filepath))
			{
				File.WriteAllText(filepath, json);
			}
			return res;
		}
	}
}
