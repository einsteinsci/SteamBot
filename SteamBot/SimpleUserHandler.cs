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
		{ }

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
			SendTradeMessage("Success. Please put up your items.");
		}

		public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{ }

		public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
		{ }

		public override void OnTradeMessage(string message)
		{ }

		public override void OnTradeReady(bool ready)
		{
			if (!ready)
			{
				Trade.SetReady(false);
			}
			else
			{
				if (Validate())
				{
					Trade.SetReady(true);
				}
				SendTradeMessage("Amount added: " + AmountAdded.ToRefString());
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
				offer.Decline();
				Log.Warn("Declined trade offer from user {0}.", OtherSID.ToString());
				SendChatMessage("I don't know you. I cannot accept your trade offer.");
			}
		}

		public bool Validate()
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
				//else if (SimpleWeapons.IsSimpleWeapon(item.Defindex))
				//{
				//	AmountAdded += TF2Value.Scrap / 2.0;
				//}
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

