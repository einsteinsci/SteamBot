﻿using System;
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

        public OrderManager()
        {
            BuyOrders = new List<Order>();
            SellOrders = new List<Order>();
        }

        public bool HasMatchingTrade(UserHandler handler, TradeOffer trade)
        {
            return BuyOrders.Exists((o) => o.TradeOfferMatches(handler, trade, true)) || 
                SellOrders.Exists((o) => o.TradeOfferMatches(handler, trade, false));
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Order
    {
        [JsonProperty]
        public double PriceRef
        { get; private set; }

        public TF2Value Price => TF2Value.FromRef(PriceRef);

        [JsonProperty]
        public ushort ItemID
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

        public Order(TF2Value price, Schema.Item item, int quality = 6, bool craftable = true, 
            bool allowKS = false, bool allowPaint = false)
        {
            PriceRef = price.RefinedTotal;
            ItemID = item.Defindex;
            Quality = quality;
            Craftable = craftable;
            AllowKillstreaks = allowKS;
            AllowPaint = allowPaint;
        }

        public bool TradeOfferMatches(UserHandler handler, TradeOffer offer, bool buyOrder)
        {
            handler.GetOtherInventory();

            if (buyOrder)
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

                    if (item.Defindex != ItemID)
                        continue;
                    if (item.Quality != Quality)
                        continue;
                    if (item.IsNotCraftable == Craftable)
                        continue;
                    if (item.HasKillstreak() != AllowKillstreaks)
                        continue;
                    if (item.HasPaint() != AllowPaint)
                        continue;

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

                    return item.Defindex == ItemID && item.Quality == Quality &&
                        item.IsNotCraftable != Craftable &&
                        item.HasKillstreak() == AllowKillstreaks && item.HasPaint() == AllowPaint;
                }
                return false;
            }
        }

        public string ToString(bool buyOrder, Schema schema)
        {
            string itemName = schema.GetItem(ItemID).ItemName;

            string res = GetQualityString(Quality) + itemName + " for " + Price.ToString();
            if (!Craftable)
                res = "Non-craftable " + res;
            if (AllowKillstreaks)
                res += " (Killstreaks allowed)";
            if (AllowPaint)
                res += " (Paint allowed)";

            if (buyOrder)
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
