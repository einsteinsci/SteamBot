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
			Log.Warn("I lost a friend today.");
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
			return true;
		}

		public override void OnTradeError(string error)
		{
			SendChatMessage("Oh, there was an error: {0}.", error);
			Log.Warn(error);
		}

		public override void OnTradeTimeout()
		{
			SendChatMessage("Sorry, but you were AFK and the trade was canceled.");
			Log.Info("User was kicked because he was AFK.");
		}

		public override void OnTradeInit()
		{
			SendTradeMessage("Trade started. To sell me an item, put it in the trade window." +
				" To buy an item, enter the item name here and add appropriate payment in pure," + 
				" or enter 'help' to get a list of the items I am selling.");
		}

		public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			Log.Info("Added item {0}{1} (defindex #{2}).", Order.GetQualityString(inventoryItem.Quality),
				schemaItem.ItemName, schemaItem.Defindex);

			if (!schemaItem.IsPure())
			{
				foreach (Order o in Bot.Orders.BuyOrders)
				{
					if (o.MatchesItem(inventoryItem))
					{
						if (Bot.MyInventory.TotalPure() < o.Price)
						{
							Log.Warn("Out of metal for buy orders! Not enough metal to buy {0}.", schemaItem.ItemName);
							SendTradeMessage("Unfortunately I am out of metal and cannot buy anything at the moment. " +
								"Enter 'help' to get a list of the items I am selling.");
						}
						else if (Bot.MyInventory.GetItemsByDefindexAndQuality(o.ItemID, o.Quality).Count >= o.MaxStock)
						{
							Log.Warn("Full stock for item {0}. ", schemaItem.ItemName);
							SendTradeMessage("Unfortunately I have full stock and cannot buy your {0}. " +
								"Enter 'help' to get a list of the items I am selling.");
						}
						else
						{
							ActiveOrder = o;
							SendTradeMessage(o.ToString(true, Trade.CurrentSchema));

							AddMyPayment();
						}
					}
				}

				ActiveOrder = null;
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
			}
		}

		public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{
			if (ActiveOrder.MatchesItem(inventoryItem))
			{
				ActiveOrder = null;
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

		public override void OnTradeMessage(string message)
		{

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
			while (myPayment + currentIteration < ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.KEY_DEFINDEX))
				{
					break;
				}

				Log.Debug("Added key in trade with user #{0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			currentIteration = TF2Value.Refined;
			while (myPayment + currentIteration < ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.REFINED_DEFINDEX))
				{
					break;
				}

				Log.Debug("Added refined metal in trade with user #{0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			currentIteration = TF2Value.Reclaimed;
			while (myPayment + currentIteration < ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.RECLAIMED_DEFINDEX))
				{
					break;
				}

				Log.Debug("Added reclaimed metal in trade with user #{0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			currentIteration = TF2Value.Scrap;
			while (myPayment + currentIteration < ActiveOrder.Price)
			{
				if (!Trade.AddItemByDefindex(TF2Value.SCRAP_DEFINDEX))
				{
					break;
				}

				Log.Debug("Added scrap metal in trade with user #{0}.", OtherSID.ToString());
				myPayment += currentIteration;
			}

			if (myPayment != ActiveOrder.Price)
			{
				Log.Error("Could not correct amount of {0}. Instead paid {1}.", ActiveOrder.Price.ToRefString(),
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

		public bool ValidateOld()
		{
			AmountAdded = TF2Value.Zero;

			List<string> errors = new List<string>();
			
			foreach (TradeUserAssets asset in Trade.OtherOfferedItems)
			{
				Inventory.Item item = Trade.OtherInventory.GetItem(asset.assetid);

				if (item.Defindex == TF2Value.SCRAP_DEFINDEX)
					AmountAdded += TF2Value.Scrap;
				else if (item.Defindex == TF2Value.RECLAIMED_DEFINDEX)
					AmountAdded += TF2Value.Reclaimed;
				else if (item.Defindex == TF2Value.REFINED_DEFINDEX)
					AmountAdded += TF2Value.Refined;
				else if (item.Defindex == TF2Value.KEY_DEFINDEX)
					AmountAdded += TF2Value.Key;
				else
				{
					Schema.Item schemaItem = Trade.CurrentSchema.GetItem(item.Defindex);
					errors.Add("Item " + schemaItem.Name + " is not a metal.");
				}
			}

			if (AmountAdded == TF2Value.Zero)
			{
				errors.Add("You must put up at least 1 scrap.");
			}

			// send the errors
			if (errors.Count != 0)
				SendTradeMessage("There were errors in your trade: ");
			foreach (string error in errors)
			{
				SendTradeMessage(error);
			}

			return errors.Count == 0;
		}

	}

}

