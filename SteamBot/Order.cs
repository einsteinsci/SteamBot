using System;
using System.Linq;

using Newtonsoft.Json;

using SteamTrade;
using SteamTrade.TradeOffer;

namespace SteamBot
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class Order
	{
		[JsonProperty]
		public double PriceRef
		{ get; private set; }

		public TF2Value Price => TF2Value.FromRef(PriceRef);

		[JsonProperty]
		public ushort Defindex
		{ get; private set; }

		[JsonProperty]
		public int Quality
		{ get; private set; }

		[JsonProperty]
		public bool Craftable
		{ get; private set; }

		[JsonProperty]
		public bool AllowKillstreaks
		{ get; private set; }

		[JsonProperty]
		public bool AllowPaint
		{ get; private set; }

		[JsonProperty]
		public bool IsBuyOrder
		{ get; private set; }

		[JsonProperty]
		public int MaxStock
		{ get; private set; }

		public Order()
		{
			PriceRef = 0;
			Defindex = 0;
			Quality = 6;
			MaxStock = 5;
			Craftable = true;
			AllowKillstreaks = false;
			AllowPaint = false;
		}

		public Order(TF2Value price, Schema.Item item, int quality = 6, int maxStock = 5, bool craftable = true, 
			bool allowKS = false, bool allowPaint = false)
		{
			PriceRef = price.RefinedTotal;
			Defindex = item.Defindex;
			Quality = quality;
			MaxStock = maxStock;
			Craftable = craftable;
			AllowKillstreaks = allowKS;
			AllowPaint = allowPaint;
		}

		public bool TradeOfferMatches(UserHandler handler, TradeOffer offer)
		{
			handler.GetOtherInventory();
			handler.Bot.GetInventory();

			if (IsBuyOrder)
			{
				TF2Value paying = TF2Value.Zero;
				foreach (var asset in offer.Items.GetMyItems())
				{
					Inventory.Item item = handler.Bot.MyInventory.GetItem((ulong)asset.AssetId);

					if (item.Defindex == TF2Value.SCRAP_DEFINDEX)
						paying += TF2Value.Scrap;
					else if (item.Defindex == TF2Value.RECLAIMED_DEFINDEX)
						paying += TF2Value.Reclaimed;
					else if (item.Defindex == TF2Value.REFINED_DEFINDEX)
						paying += TF2Value.Refined;
					else if (item.Defindex == TF2Value.KEY_DEFINDEX)
						paying += TF2Value.Key;
					else
						return false; // I only pay in pure if it's a buy order.
				}
				if (paying > Price)
				{
					return false;
				}

				bool hasWantedStuff = false;
				foreach (var asset in offer.Items.GetTheirItems())
				{
					Inventory.Item item = handler.OtherInventory.GetItem((ulong)asset.AssetId);

					if (!MatchesItem(item))
					{
						continue;
					}

					hasWantedStuff = true;
					break;
				}

				return hasWantedStuff;
			}
			else
			{
				TF2Value paid = TF2Value.Zero;
				foreach (var asset in offer.Items.GetTheirItems())
				{
					Inventory.Item item = handler.OtherInventory.GetItem((ulong)asset.AssetId);

					if (item.Defindex == TF2Value.SCRAP_DEFINDEX)
						paid += TF2Value.Scrap;
					else if (item.Defindex == TF2Value.RECLAIMED_DEFINDEX)
						paid += TF2Value.Reclaimed;
					else if (item.Defindex == TF2Value.REFINED_DEFINDEX)
						paid += TF2Value.Refined;
					else if (item.Defindex == TF2Value.KEY_DEFINDEX)
						paid += TF2Value.Key;
				}

				if (paid < Price)
				{
					return false;
				}
				
				var myAssets = offer.Items.GetMyItems();
				if (myAssets.Count == 1)
				{
					var asset = myAssets.First();
					Inventory.Item item = handler.Bot.MyInventory.GetItem((ulong)asset.AssetId);

					return MatchesItem(item);
				}
				return false;
			}
		}

		public bool MatchesItem(Inventory.Item item)
		{
			return item.Defindex == Defindex && item.Quality == Quality &&
				   item.IsNotCraftable != Craftable &&
				   item.HasKillstreak() == AllowKillstreaks && item.HasPaint() == AllowPaint;
		}

		public string GetSearchString(Schema schema)
		{
			string itemName = schema.GetItem(Defindex).ItemName;

			string res = GetQualityString(Quality) + itemName;
			if (!Craftable)
				res += "Non-craftable " + res;

			return res;
		}

		public string ToString(Schema schema)
		{
			string itemName = schema.GetItem(Defindex).ItemName;

			string res = GetQualityString(Quality) + itemName + " for " + Price.ToString();
			if (!Craftable)
				res = "Non-craftable " + res;
			if (AllowKillstreaks)
				res += " (Killstreaks allowed)";
			if (AllowPaint)
				res += " (Paint allowed)";

			if (IsBuyOrder)
			{
				res = "Buying " + res;
			}
			else
			{
				res = "Selling " + res;
			}

			return res;
		}

		public static string GetQualityString(int quality)
		{
			switch (quality)
			{
				case 0:
					return "Stock ";
				case 1:
					return "Genuine ";
				case 2:
					return "Vintage ";
				case 5:
					return "Unusual ";
				case 6:
					return "";
				case 7:
					return "Community ";
				case 8:
					return "Valve ";
				case 9:
					return "Self-Made ";
				case 11:
					return "Strange ";
				case 13:
					return "Haunted ";
				case 14:
					return "Collector's ";
				case 15:
					return "SKIN_WEAPON ";
				default:
					return "ERR_UNKNOWN_QUALITY ";
			}
		}
	}
}