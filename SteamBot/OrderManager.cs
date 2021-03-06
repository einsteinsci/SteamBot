﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using SteamTrade;
using SteamTrade.TradeOffer;

namespace SteamBot
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class OrderManager
	{
		public static readonly string ORDERS_FILENAME = "orders.json";
		public static readonly string ORDERS_FOLDER = Path.Combine(GetOneDrivePath(), "SteamBot");

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

		public bool? HasMatchingOrder(UserHandler handler, TradeOffer trade)
		{
			foreach (Order o in AllOrders)
			{
				bool? matches = o.TradeOfferMatches(handler, trade);
				if (matches == null)
				{
					handler.Log.Error("Unable to retreive inventory. Ignoring trade.");
					return null;
				}
				else if (matches == true)
				{
					return true;
				}
			}
			return false;
		}

		public Order GetMatchingOrder(UserHandler handler, TradeOffer trade)
		{
			Order buy = BuyOrders.FirstOrDefault((o) => o.TradeOfferMatches(handler, trade) == true);
			if (buy != null)
			{
				return buy;
			}

			return SellOrders.FirstOrDefault((o) => o.TradeOfferMatches(handler, trade) == true);
		}

		public static OrderManager Load(Log logger)
		{
			string filepath = Path.Combine(ORDERS_FOLDER, ORDERS_FILENAME);
			Directory.CreateDirectory(ORDERS_FOLDER);

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

		public void SaveAll()
		{
			string filepath = Path.Combine(ORDERS_FOLDER, ORDERS_FILENAME);
			Directory.CreateDirectory(ORDERS_FOLDER);

			string json = JsonConvert.SerializeObject(this, Formatting.Indented);

			File.WriteAllText(filepath, json);
		}

		public static string GetOneDrivePath()
		{
			const string onedriveRegPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\OneDrive";

			return Registry.GetValue(onedriveRegPath, "UserFolder", null) as string;
		}
	}
}
