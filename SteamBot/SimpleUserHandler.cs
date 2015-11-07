using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeWebAPI;
using System.Linq;
using SteamTrade.TradeOffer;

namespace SteamBot
{
	public class SimpleUserHandler : UserHandler
	{
		public TF2Value AmountAdded;

		public Order ActiveOrder
		{ get; private set; }

		public SimpleUserHandler(Bot bot, SteamID sid) : base(bot, sid)
		{ }

		public override bool OnGroupAdd()
		{
			return IsAdmin;
		}

		public override bool OnFriendAdd()
		{
			Log.Info("{0} has added me to their friends list.", OtherSID.ToString());
			Bot.ResetFriendsList();
			return true;
		}

		public override void OnLoginCompleted()
		{ }

		public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
		{
			Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
			base.OnChatRoomMessage(chatID, sender, message);
		}

		public override void OnFriendRemove()
		{
			Log.Info("{0} has unfriended me.", OtherSID.ToString());
			Bot.ResetFriendsList();
		}

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

		public override bool OnTradeRequest()
		{
			SendChatMessage("I am accepting your trade request.");
			return true;
		}

		public override void OnTradeError(string error)
		{
			if (error.Contains("was cancelled by other user"))
			{
				SendChatMessage("The trade has been canceled.");
				Log.Info("Trade with user has been cancelled.");
				return;
			}

			if (IsAdmin)
			{
				SendChatMessage("I have encountered an error: {0}.", error);
			}
			else
			{
				SendChatMessage("I have encountered an error. Please cancel and restart the trade.");
			}

			Log.Error(error);
		}

		public override void OnTradeTimeout()
		{
			SendChatMessage("I have cancelled the trade as you were AFK.");
			Log.Info("User was kicked because he was AFK.");
		}

		public override void OnTradeInit()
		{
			SendTradeMessage("Trade started. To sell me an item, put it in the trade window.");
			SendTradeMessage("To buy an item, enter 'buy' followed by the item name,");
			SendTradeMessage("or on its own to get a list of the items I am selling.");
		}

		public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			Log.Info("Added item {0}{1} (defindex #{2}).", Order.GetQualityString(inventoryItem.Quality),
				schemaItem.ItemName, schemaItem.Defindex);

			if (!schemaItem.IsPure())
			{
				Bot.GetInventory();

				bool found = false;
				foreach (Order o in Bot.Orders.BuyOrders)
				{
					if (o.MatchesItem(inventoryItem))
					{
						found = true;

						if (Bot.MyInventory.TotalPure() < o.Price)
						{
							Log.Warn("Out of metal for buy orders! Not enough metal to buy {0}.", schemaItem.ItemName);
							SendTradeMessage("Unfortunately I am out of metal and cannot buy anything at the moment.");
							SendTradeMessage("Enter 'buy' to get a list of the items I am selling.");
						}
						else if (Bot.MyInventory.GetItemsByDefindex(o.Defindex, o.Quality).Count >= o.MaxStock)
						{
							Log.Warn("Full stock for item {0}.", schemaItem.ItemName);
							SendTradeMessage("Unfortunately I have full stock and cannot buy your {0}.", schemaItem.ItemName);
							SendTradeMessage("Enter 'buy' to get a list of the items I am selling.");
						}
						else
						{
							ActiveOrder = o;
							SendTradeMessage(o.ToString(Trade.CurrentSchema));

							AddMyPayment();
						}
					}
				}

				if (!found)
				{
					SendTradeMessage("I am currently not buying that item at the moment.");
					SendTradeMessage("Enter 'buy' to get a list of the items I am selling.");
				}
			}
			else
			{
				if (schemaItem.Defindex == TF2Value.SCRAP_DEFINDEX)
					AmountAdded += TF2Value.Scrap;
				else if (schemaItem.Defindex == TF2Value.RECLAIMED_DEFINDEX)
					AmountAdded += TF2Value.Reclaimed;
				else if (schemaItem.Defindex == TF2Value.REFINED_DEFINDEX)
					AmountAdded += TF2Value.Refined;
				else if (schemaItem.Defindex == TF2Value.KEY_DEFINDEX)
					AmountAdded += TF2Value.Key;
				else if (ActiveOrder != null)
				{
					SendTradeMessage("Sorry, but I cannot accept any {0} as valid payment.",
						schemaItem.ItemName);
				}

				if (AmountAdded == ActiveOrder.Price)
				{
					SendTradeMessage("You have paid the correct amount.");
				}
				else if (AmountAdded > ActiveOrder.Price)
				{
					SendTradeMessage("You are paying too much! The price for the {0} is {1}.",
						ActiveOrder.GetSearchString(Trade.CurrentSchema), ActiveOrder.Price.ToRefString());
				}
			}
		}

		public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			if (ActiveOrder != null && ActiveOrder.MatchesItem(inventoryItem))
			{
				ActiveOrder = null;
				Trade.RemoveAllItems();
			}
			else
			{
				if (schemaItem.Defindex == TF2Value.SCRAP_DEFINDEX)
					AmountAdded -= TF2Value.Scrap;
				else if (schemaItem.Defindex == TF2Value.RECLAIMED_DEFINDEX)
					AmountAdded -= TF2Value.Reclaimed;
				else if (schemaItem.Defindex == TF2Value.REFINED_DEFINDEX)
					AmountAdded -= TF2Value.Refined;
				else if (schemaItem.Defindex == TF2Value.KEY_DEFINDEX)
					AmountAdded -= TF2Value.Key;
			}
		}

		public override void OnTradeMessage(string messageRaw)
		{
			string message = messageRaw.Trim().ToLower();

			Bot.GetInventory();

			if (message == "help" || message == "!help" || message == "?")
			{
				SendTradeMessage("These are all the available trade commands:");
				SendTradeMessage("> help: List all trade commands.");
				SendTradeMessage("> buy, listall, showall: Show all items I am selling.");
				SendTradeMessage("> buy {item name}: Specify an item you wish to buy from me.");
				SendTradeMessage("> clear: Clear my items from the trade window.");
				SendTradeMessage("> cancel: Cancel the trade.");
			}

			if (message == "buy" || message == "listall" || message == "showall")
			{
				SendTradeMessage("These are the items I am currently selling: ");
				foreach (Order o in Bot.Orders.SellOrders)
				{
					List<Inventory.Item> inStock = Bot.MyInventory.
						GetItemsByDefindex(o.Defindex, o.Quality);
					if (inStock != null && inStock.Count > 0)
					{
						SendTradeMessage("> {0} [{1} in stock]",
							o.ToString(Trade.CurrentSchema), inStock.Count.ToString());
						Trade.AddItemByDefindex(o.Defindex, o.Quality);
					}
				}
				return;
			}

			if (message == "clear")
			{
				Trade.RemoveAllItems();
			}

			if (message == "cancel" || message == "exit")
			{
				SendTradeMessage("I am now cancelling the trade.");
				Trade.CancelTrade();
				return;
			}

			#region buy itemname
			if (message.StartsWith("buy "))
			{
				string query = message.Substring("buy ".Length);
				query = query.Trim();

				bool found = false;
				foreach (Order o in Bot.Orders.SellOrders)
				{
					if (o.GetSearchString(Trade.CurrentSchema).ToLower() == query)
					{
						List<Inventory.Item> inStock = Bot.MyInventory.GetItemsByDefindex(o.Defindex, o.Quality);
						if (inStock.Count == 0)
						{
							SendTradeMessage("Unfortunately I seem to be out of that item.");

							return;
						}

						ActiveOrder = o;
						SendTradeMessage("I currently have {0} of those in stock.", inStock.Count.ToString());

						Trade.RemoveAllItems();
						Trade.AddItemByDefindex(o.Defindex, o.Quality);
						found = true;

						SendTradeMessage("Now add your payment of {0}.", o.Price.ToRefString());
					}
				}

				if (!found)
				{
					List<Order> validOrders = new List<Order>();
					foreach (Order o in Bot.Orders.SellOrders)
					{
						if (o.GetSearchString(Trade.CurrentSchema).ToLower().Contains(query))
						{
							validOrders.Add(o);
						}
					}

					if (validOrders.Count > 1)
					{
						SendTradeMessage("It seems I am selling multiple items that may fit that name:");
						foreach (Order o in validOrders)
						{
							SendTradeMessage("> {0} [{1} in stock]", o.ToString(Trade.CurrentSchema),
								Bot.MyInventory.GetItemsByDefindex(o.Defindex, o.Quality).Count.ToString());
						}
						SendTradeMessage("Perhaps being more specific in your query may help.");
						return;
					}
					else if (validOrders.Count == 0)
					{
						SendTradeMessage("Unfortunately I am not selling any items that fit that name.");
						return;
					}
					else // 1
					{
						Order o = validOrders.First();
						ActiveOrder = o;
						SendTradeMessage("I currently have {0} of those in stock.",
							Bot.MyInventory.GetItemsByDefindex(o.Defindex, o.Quality).Count.ToString());

						Trade.RemoveAllItems();
						Trade.AddItemByDefindex(o.Defindex, o.Quality);

						SendTradeMessage("Now add your payment of {0}.", o.Price.ToRefString());
					}
				}
			}
			#endregion buy itemname
		}

		public override void OnTradeReady(bool otherGuyReady)
		{
			if (!otherGuyReady)
			{
				Trade.SetReady(false);
			}
			else
			{
				if (Validate())
				{
					Trade.SetReady(true);
				}
				//SendTradeMessage("Amount added: " + AmountAdded.ToRefString());
			}
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
			if (Validate() || IsAdmin)
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
				if (Bot.Orders.HasMatchingTrade(this, offer))
				{
					offer.Accept();
					Log.Success("Accepted valid trade offer from user #{0}.", OtherSID.ToString());
					SendChatMessage("I have accepted your trade offer.");
				}
				else
				{
					offer.Decline();
					Log.Warn("Declined trade offer from user #{0}.", OtherSID.ToString());
					SendChatMessage("There seems to be a problem with your trade offer. It has been declined.");
				}

				//offer.Decline();
				//Log.Warn("Declined trade offer from user {0}.", OtherSID.ToString());
				//SendChatMessage("I don't know you. I cannot accept your trade offer.");
			}
		}

		public void AddMyPayment()
		{
			if (ActiveOrder == null)
			{
				Log.Error("Trade with user #{0}: SimpleUserHandler.ActiveOrder == null.", OtherSID.ToString());
				Trade.CancelTrade();
				SendChatMessage("I have encountered an error. Please send the trade again.");

				return;
			}

			SendTradeMessage("Removing items from previous trades...");
			Trade.RemoveAllItems();
			
			SendTradeMessage("Adding payment for this trade...");
			TF2Value myPayment = TF2Value.Zero;
			TF2Value currentIteration = TF2Value.Key;
			while (myPayment + currentIteration <= ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.KEY_DEFINDEX))
				{
					Log.Warn("[TRADE-BUY] No more keys found. Moving on to refined metal.");
					break;
				}

				Log.Debug("Added key in trade with user {0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			currentIteration = TF2Value.Refined;
			while (myPayment + currentIteration <= ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.REFINED_DEFINDEX))
				{
					Log.Warn("[TRADE-BUY] No more refined metal found. Moving on to reclaimed metal.");
					break;
				}

				Log.Debug("Added refined metal in trade with user {0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			currentIteration = TF2Value.Reclaimed;
			while (myPayment + currentIteration <= ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.RECLAIMED_DEFINDEX))
				{
					Log.Warn("[TRADE-BUY] No more reclaimed metal found. Moving on to scrap metal.");
					break;
				}

				Log.Debug("Added reclaimed metal in trade with user {0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			currentIteration = TF2Value.Scrap;
			while (myPayment + currentIteration <= ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.SCRAP_DEFINDEX))
				{
					Log.Warn("[TRADE-BUY] No more scrap metal found.");
					break;
				}

				Log.Debug("Added scrap metal in trade with user {0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			if (myPayment != ActiveOrder.Price)
			{
				Log.Error("Could not add correct amount of {0}. Instead paid {1}.", ActiveOrder.Price.ToRefString(),
					myPayment.ToRefString());
				Trade.CancelTrade();
				SendChatMessage("I have encountered an error. Please send the trade again.");
			}

			SendTradeMessage("Finished paying {0}.", myPayment.ToRefString());
		}

		public bool Validate()
		{
			List<string> errors = new List<string>();

			if (ActiveOrder == null)
			{
				if (Trade.OtherOfferedItems.Count() == 0)
				{
					errors.Add("There is nothing you are offering.");
				}
				else
				{
					errors.Add("I am currently not buying any of those items.");
				}
			}
			else
			{
				// in buy orders, assume the bot calculates the pricing correctly.
				if (!ActiveOrder.IsBuyOrder)
				{
					if (HasNonPureInTrade())
					{
						errors.Add("You have non-pure items in payment.");
					}

					if (AmountAdded < ActiveOrder.Price)
					{
						errors.Add(string.Format("You have only paid {0}. You still have {2} to go to reach the price of {1}.",
							AmountAdded.ToRefString(), ActiveOrder.Price.ToRefString(),
							(ActiveOrder.Price - AmountAdded).ToRefString()));
					}
					else if (AmountAdded > ActiveOrder.Price)
					{
						errors.Add(string.Format("You are paying too much. The price is {0}, but you have paid {1}.",
							ActiveOrder.Price.ToRefString(), AmountAdded.ToRefString()));
					}
				}
				else if (Trade.OtherOfferedItems.Count() > 1)
				{
					errors.Add("You have other items in your trade besides the one I am buying.");
				}
			}

			if (errors.Count > 0)
			{
				SendTradeMessage("There were problems with your trade:");
				foreach (string e in errors)
				{
					SendTradeMessage("> " + e);
				}
			}

			return errors.Count == 0;
		}

		// returns true if the user has weird crap in their trade window.
		public bool HasNonPureInTrade()
		{
			foreach (TradeUserAssets asset in Trade.OtherOfferedItems)
			{
				Inventory.Item item = Trade.OtherInventory.GetItem(asset.assetid);
				Schema.Item schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);

				if (!schemaItem.IsPure())
				{
					return true;
				}
			}

			return false;
		}
	}
}

