using System;
using System.Collections.Generic;
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
		public const string BPTF_TOKEN = "55f8e711b98d8871558b4601";

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

		public static OrderManager Load()
		{
			throw new NotImplementedException();
		}
	}
}
