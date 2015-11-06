using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamTrade
{
	public class Inventory
	{
		/// <summary>
		/// Fetches the inventory for the given Steam ID using the Steam API.
		/// </summary>
		/// <returns>The give users inventory.</returns>
		/// <param name='steamId'>Steam identifier.</param>
		/// <param name='apiKey'>The needed Steam API key.</param>
		/// <param name="steamWeb">The SteamWeb instance for this Bot</param>
		public static Inventory FetchInventory(ulong steamId, string apiKey, SteamWeb steamWeb)
		{
			int attempts = 1;
			InventoryResponse result = null;
			while ((result == null || result.result.items == null) && attempts <= 3)
			{
				var url = "http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=" + apiKey + "&steamid=" + steamId;
				string response = steamWeb.Fetch(url, "GET", null, false);
				result = JsonConvert.DeserializeObject<InventoryResponse>(response);
				attempts++;
			}
			return new Inventory(result.result);
		}

		/// <summary>
		/// Gets the inventory for the given Steam ID using the Steam Community website.
		/// </summary>
		/// <returns>The inventory for the given user. </returns>
		/// <param name='steamid'>The Steam identifier. </param>
		/// <param name="steamWeb">The SteamWeb instance for this Bot</param>
		public static dynamic GetInventory(SteamID steamid, SteamWeb steamWeb)
		{
			string url = string.Format (
				"http://steamcommunity.com/profiles/{0}/inventory/json/440/2/?trading=1",
				steamid.ConvertToUInt64 ()
			);
			
			try
			{
				string response = steamWeb.Fetch (url, "GET");
				return JsonConvert.DeserializeObject (response);
			}
			catch (Exception)
			{
				return JsonConvert.DeserializeObject ("{\"success\":\"false\"}");
			}
		}

		public uint NumSlots { get; set; }
		public Item[] Items { get; set; }
		public bool IsPrivate { get; private set; }
		public bool IsGood { get; private set; }

		protected Inventory (InventoryResult apiInventory)
		{
			NumSlots = apiInventory.num_backpack_slots;
			Items = apiInventory.items;
			IsPrivate = (apiInventory.status == "15");
			IsGood = (apiInventory.status == "1");
		}

		/// <summary>
		/// Check to see if user is Free to play
		/// </summary>
		/// <returns>Yes or no</returns>
		public bool IsFreeToPlay()
		{
			return this.NumSlots % 100 == 50;
		}

		public Item GetItem (ulong id)
		{
			// Check for Private Inventory
			if( this.IsPrivate )
				throw new Exceptions.TradeException("Unable to access Inventory: Inventory is Private!");

			return (Items == null ? null : Items.FirstOrDefault(item => item.Id == id));
		}

		public TF2Value TotalPure()
		{
			List<Item> keys = GetItemsByDefindex(TF2Value.KEY_DEFINDEX);
			List<Item> refined = GetItemsByDefindex(TF2Value.REFINED_DEFINDEX);
			List<Item> rec = GetItemsByDefindex(TF2Value.RECLAIMED_DEFINDEX);
			List<Item> scrap = GetItemsByDefindex(TF2Value.SCRAP_DEFINDEX);

			keys.RemoveAll((i) => i.IsNotTradeable);

			TF2Value res = TF2Value.Zero;
			res += TF2Value.Key * keys.Count;
			res += TF2Value.Refined * refined.Count;
			res += TF2Value.Reclaimed * rec.Count;
			res += TF2Value.Scrap * scrap.Count;

			return res;
		}

		public List<Item> GetItemsByDefindex (int defindex)
		{
			// Check for Private Inventory
			if( this.IsPrivate )
				throw new Exceptions.TradeException("Unable to access Inventory: Inventory is Private!");

			return Items.Where(item => item.Defindex == defindex).ToList();
		}

		public List<Item> GetItemsByDefindexAndQuality(int defindex, int quality)
		{
			if (IsPrivate)
			{
				throw new Exceptions.TradeException("Unable to access Inventory: Inventory is Private!");
			}

			return Items.Where((i) => i.Defindex == defindex && i.Quality == quality).ToList();
		}

		public class Item
		{
			public int AppId = 440;
			public long ContextId = 2;

			[JsonProperty("id")]
			public ulong Id { get; set; }

			[JsonProperty("original_id")]
			public ulong OriginalId { get; set; }

			[JsonProperty("defindex")]
			public ushort Defindex { get; set; }

			[JsonProperty("level")]
			public byte Level { get; set; }

			[JsonProperty("quality")]
			public int Quality { get; set; }

			[JsonProperty("quantity")]
			public int RemainingUses { get; set; }

			[JsonProperty("origin")]
			public int Origin { get; set; }

			[JsonProperty("custom_name")]
			public string CustomName { get; set; }

			[JsonProperty("custom_desc")]
			public string CustomDescription { get; set; }

			[JsonProperty("flag_cannot_craft")]
			public bool IsNotCraftable { get; set; }

			[JsonProperty("flag_cannot_trade")]
			public bool IsNotTradeable { get; set; }

			[JsonProperty("attributes")]
			public ItemAttribute[] Attributes { get; set; }

			[JsonProperty("contained_item")]
			public Item ContainedItem { get; set; }

			public bool HasKillstreak()
			{
				ItemAttribute att = Attributes.FirstOrDefault((a) => a.Defindex == ItemAttribute.KILLSTREAK_DEFINDEX);

				return att != null;
			}

			public bool HasPaint()
			{
				ItemAttribute att = Attributes.FirstOrDefault((a) => a.Defindex == ItemAttribute.PAINT_DEFINDEX);

				return att != null;
			}
		}

		public class ItemAttribute
		{
			public const ushort KILLSTREAK_DEFINDEX = 2025;
			public const ushort PAINT_DEFINDEX = 142;

			[JsonProperty("defindex")]
			public ushort Defindex { get; set; }

			[JsonProperty("value")]
			public string Value { get; set; }

			[JsonProperty("float_value")]
			public float FloatValue { get; set; }

			[JsonProperty("account_info")]
			public AccountInfo AccountInfo { get; set; } 
		}

		public class AccountInfo
		{
			[JsonProperty("steamid")]
			public ulong SteamID { get; set; }

			[JsonProperty("personaname")]
			public string PersonaName { get; set; }
		}

		protected class InventoryResult
		{
			public string status { get; set; }

			public uint num_backpack_slots { get; set; }

			public Item[] items { get; set; }
		}

		protected class InventoryResponse
		{
			public InventoryResult result;
		}
	}
}

