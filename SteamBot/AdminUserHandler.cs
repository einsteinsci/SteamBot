using System;
using System.Windows.Forms;
using SteamKit2;
using SteamTrade;
using System.Collections.Generic;
using System.Linq;
using SteamTrade.TradeOffer;

namespace SteamBot
{
	/// <summary>
	/// A user handler class that implements basic text-based commands entered in
	/// chat or trade chat.
	/// </summary>
	public class AdminUserHandler : UserHandler
	{
		private const string ADD_CMD = "add";
		private const string REMOVE_CMD = "remove";
		private const string HELP_CMD = "help";

		private const string ADD_CRATE_SUB = "crates";
		private const string ADD_WEAPS_SUB = "weapons";
		private const string ADD_METAL_SUB = "metal";
		private const string ADD_ALL_SUB = "all";
		private const string ADD_ITEMS_SUB = "items";

		public AdminUserHandler(Bot bot, SteamID sid) : base(bot, sid)
		{ }

		#region Overrides of UserHandler

		/// <summary>
		/// Called when the bot is fully logged in.
		/// </summary>
		public override void OnLoginCompleted()
		{ }

		/// <summary>
		/// Triggered when a clan invites the bot.
		/// </summary>
		/// <returns>
		/// Whether to accept.
		/// </returns>
		public override bool OnGroupAdd()
		{
			return IsAdmin;
		}

		/// <summary>
		/// Called when a the user adds the bot as a friend.
		/// </summary>
		/// <returns>
		/// Whether to accept.
		/// </returns>
		public override bool OnFriendAdd()
		{
			// if the other is an admin then accept add
			if (IsAdmin)
			{
				return true;
			}

			Log.Warn("Arbitrary SteamID: " + OtherSID + " tried to add the bot as a friend");
			return false;
		}

		public override void OnFriendRemove()
		{
			Log.Info("I lost a friend today.");
		}

		public override void OnNewTradeOffer(TradeOffer offer)
		{
			if (IsAdmin)
			{
				offer.Accept();
				Log.Success("Accepted trade offer from admin {0}.", Bot.SteamFriends.GetFriendPersonaName(OtherSID));
				SendChatMessage("Trade offer complete.");
			}
			else
			{
				offer.Decline();
				Log.Warn("Declined trade offer from user {0}.", OtherSID.ToString());
				SendChatMessage("I don't know you. I cannot accept your trade offer.");
			}
		}

		/// <summary>
		/// Called whenever a message is sent to the bot.
		/// This is limited to regular and emote messages.
		/// </summary>
		public override void OnMessage(string message, EChatEntryType type)
		{
			if (type != EChatEntryType.ChatMsg)
			{
				return;
			}

			if (message.StartsWith("!") ||
				message.StartsWith("/") ||
				message.StartsWith("#"))
			{
				List<string> args = message.Split(' ').ToList();
				string cmdName = args[0].Substring(1);
				args.RemoveAt(0);

				ChatHandler.RunCommand(cmdName, args, this);
			}
			else if (ChatHandler.ChatCommands.Exists((cmd) => message.ToLower().StartsWith(cmd.CommandName)))
			{
				List<string> args = message.Split(' ').ToList();
				string cmdName = args[0];
				args.RemoveAt(0);

				ChatHandler.RunCommand(cmdName, args, this);
			}
		}

		/// <summary>
		/// Called whenever a user requests a trade.
		/// </summary>
		/// <returns>
		/// Whether to accept the request.
		/// </returns>
		public override bool OnTradeRequest()
		{
			if (IsAdmin)
				return true;

			return false;
		}

		public override void OnTradeError(string error)
		{
			Log.Error(error);
		}

		public override void OnTradeTimeout()
		{
			Log.Warn("Trade timed out.");
		}

		public override void OnTradeInit()
		{
			SendTradeMessage("Success. (Type {0} for commands)", HELP_CMD);
		}

		public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			Log.Info("Admin added item to trade: " + schemaItem.ItemName);
		}

		public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			Log.Info("Admin removed item from trade: " + schemaItem.ItemName);
		}

		public override void OnTradeMessage(string message)
		{
			ProcessTradeMessage(message);
		}

		public override void OnTradeReady(bool ready)
		{
			if (!IsAdmin)
			{
				SendTradeMessage("You are not my master.");
				Trade.SetReady(false);
				return;
			}

			Trade.SetReady(true);
		}

		public override void OnTradeSuccess()
		{
			Log.Success("Trade Complete.");
		}

		public override void OnTradeAwaitingEmailConfirmation(long tradeOfferID)
		{
			Log.Warn("Trade ended awaiting email confirmation");
			SendChatMessage("Please complete the email confirmation to finish the trade");
		}

		public override void OnTradeAccept()
		{
			if (IsAdmin)
			{
				//Even if it is successful, AcceptTrade can fail on
				//trades with a lot of items so we use a try-catch
				try
				{
					if (Trade.AcceptTrade())
						Log.Success("Trade Accepted!");
				}
				catch
				{
					Log.Warn("The trade might have failed, but we can't be sure.");
				}
			}
		}

		#endregion

		private void ProcessTradeMessage(string message)
		{
			if (message.Equals(HELP_CMD) || message.Trim() == "?")
			{
				PrintHelpMessage();
				return;
			}

			if (message.StartsWith(ADD_CMD))
			{
				HandleAddCommand(message);
				SendTradeMessage("Done adding.");
			}
			else if (message.StartsWith(REMOVE_CMD))
			{
				HandleRemoveCommand(message);
				SendTradeMessage("Done removing.");
			}
		}

		private void PrintHelpMessage()
		{
			SendTradeMessage("{0} {1} [amount] [series] - adds all crates " +
				"(optionally by series number, use 0 for amount to add all)", ADD_CMD, ADD_CRATE_SUB);
			SendTradeMessage("{0} {1} [amount] - adds metal", ADD_CMD, ADD_METAL_SUB);
			SendTradeMessage("{0} {1} [amount] - adds weapons", ADD_CMD, ADD_WEAPS_SUB);
			SendTradeMessage("{0} {1} [amount] - adds everything", ADD_CMD, ADD_ALL_SUB);
			SendTradeMessage("{0} {1} [amount] - adds (non-pure) items", ADD_CMD, ADD_ITEMS_SUB);
			SendTradeMessage("{0} <craft_material_type> [amount] - adds all or a given amount of items of a given crafting type.", ADD_CMD);
			SendTradeMessage("{0} <defindex> [amount] - adds all or a given amount of items of a given defindex.", ADD_CMD);

			//SendTradeMessage("See http://wiki.teamfortress.com/wiki/WebAPI/GetSchema for info about craft_material_type or defindex.");
		}

		private void HandleAddCommand(string command)
		{
			string[] args = command.Split(' ');
			string typeToAdd;

			bool subCmdOk = GetSubCommand(args, out typeToAdd);

			if (!subCmdOk)
				return;

			uint amount = GetAddAmount(args);

			if (typeToAdd.ToLower() == "key")
				typeToAdd = TF2Value.KEY_DEFINDEX.ToString();
			else if (typeToAdd.ToLower() == "ref" || typeToAdd.ToLower() == "refined")
				typeToAdd = TF2Value.REFINED_DEFINDEX.ToString();
			else if (typeToAdd.ToLower() == "rec" || typeToAdd.ToLower() == "reclaimed")
				typeToAdd = TF2Value.RECLAIMED_DEFINDEX.ToString();
			else if (typeToAdd.ToLower() == "scrap")
				typeToAdd = TF2Value.SCRAP_DEFINDEX.ToString();

			// if user supplies the defindex directly use it to add.
			int defindex;
			if (int.TryParse(typeToAdd, out defindex))
			{
				Trade.AddAllItemsByDefindex(defindex, amount);
				return;
			}

			switch (typeToAdd)
			{
				case ADD_METAL_SUB:
					AddItemsByCraftType("craft_bar", amount);
					break;
				case ADD_ITEMS_SUB:
					AddAllNonPure();
					break;
				case ADD_WEAPS_SUB:
					AddItemsByCraftType("weapon", amount);
					break;
				case ADD_CRATE_SUB:
					// data[3] is the optional series number
					if (!string.IsNullOrEmpty(args[3]))
						AddCrateBySeries(args[3], amount);
					else
						AddItemsByCraftType("supply_crate", amount);
					break;
				case ADD_ALL_SUB:
					AddAllItems();
					break;
				default:
					AddItemsByCraftType(typeToAdd, amount);
					break;
			}
		}
		
		private void HandleRemoveCommand(string command)
		{
			string[] data = command.Split(' ');

			List<string> args = new List<string>();
			for (int i = 1; i < data.Length; i++)
			{
				args.Add(data[i]);
			}

			if (args.Count == 0 || args[0].ToLower() == "all")
			{
				Trade.RemoveAllItems();
				return;
			}

			if (args[0].ToLower() == "scrap")
			{
				args[0] = TF2Value.SCRAP_DEFINDEX.ToString();
			}
			else if (args[0].ToLower() == "reclaimed" || args[0].ToLower() == "rec")
			{
				args[0] = TF2Value.RECLAIMED_DEFINDEX.ToString();
			}
			else if (args[0].ToLower() == "refined" || args[0].ToLower() == "ref")
			{
				args[0] = TF2Value.REFINED_DEFINDEX.ToString();
			}

			int n;
			if (int.TryParse(args[0], out n))
			{
				ushort count = 0;
				if (args.Count > 1)
				{
					if (!ushort.TryParse(args[1], out count))
					{
						count = 0;
					}
				}

				Trade.RemoveAllItemsByDefindex(n, count);
			}
		}
		
		private void AddItemsByCraftType(string typeToAdd, uint amount)
		{
			var items = Trade.CurrentSchema.GetItemsByCraftingMaterial(typeToAdd);

			uint added = 0;

			foreach (var item in items)
			{
				added += Trade.AddAllItemsByDefindex(item.Defindex, amount);

				// if bulk adding something that has a lot of unique
				// defindex (weapons) we may over add so limit here also
				if (amount > 0 && added >= amount)
					return;
			}
		}

		private void AddAllItems()
		{
			List<Schema.Item> items = Trade.CurrentSchema.GetItems();

			foreach (Schema.Item item in items)
			{
				Trade.AddAllItemsByDefindex(item.Defindex);
			}
		}

		private void AddAllNonPure()
		{
			List<Schema.Item> items = Trade.CurrentSchema.GetItems();

			foreach (Schema.Item item in items)
			{
				if (!item.IsPure())
				{
					Trade.AddAllItemsByDefindex(item.Defindex);
				}
			}
		}

		private void AddCrateBySeries(string series, uint amount)
		{
			int ser;
			bool parsed = int.TryParse(series, out ser);

			if (!parsed)
				return;

			var l = Trade.CurrentSchema.GetItemsByCraftingMaterial("supply_crate");


			List<Inventory.Item> invItems = new List<Inventory.Item>();

			foreach (var schemaItem in l)
			{
				ushort defindex = schemaItem.Defindex;
				invItems.AddRange(Trade.MyInventory.GetItemsByDefindex(defindex));
			}

			uint added = 0;

			foreach (var item in invItems)
			{
				int crateNum = 0;
				for (int count = 0; count < item.Attributes.Length; count++)
				{
					// FloatValue will give you the crate's series number
					crateNum = (int)item.Attributes[count].FloatValue;

					if (crateNum == ser)
					{
						bool ok = Trade.AddItem(item.Id);

						if (ok)
							added++;

						// if bulk adding something that has a lot of unique
						// defindex (weapons) we may over add so limit here also
						if (amount > 0 && added >= amount)
							return;
					}
				}
			}
		}

		bool GetSubCommand(string[] data, out string subCommand)
		{
			if (data.Length < 2)
			{
				SendTradeMessage("No parameter for cmd");
				subCommand = null;
				return false;
			}

			if (string.IsNullOrEmpty(data[1]))
			{
				SendTradeMessage("No parameter for cmd");
				subCommand = null;
				return false;
			}

			subCommand = data[1];

			return true;
		}

		static uint GetAddAmount(string[] data)
		{
			uint amount = 0;

			if (data.Length > 2)
			{
				// get the optional amount parameter
				if (!String.IsNullOrEmpty(data[2]))
				{
					uint.TryParse(data[2], out amount);
				}
			}

			return amount;
		}
	}
}