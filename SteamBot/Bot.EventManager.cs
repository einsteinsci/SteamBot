using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamTrade;

namespace SteamBot
{
	public partial class Bot
	{
		[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
		public sealed class BotEventAttribute : Attribute
		{
			public BotEventAttribute()
			{ }

			public static List<MethodInfo> GetMethodsWithinType(Type t)
			{
				List<MethodInfo> res = new List<MethodInfo>();
				foreach (MethodInfo m in t.GetMethods())
				{
					if (m.GetCustomAttributes<BotEventAttribute>().Count() > 0)
					{
						res.Add(m);
					}
				}

				return res;
			}
		}

		public class EventManager : IDisposable
		{
			public Bot Bot
			{ get; private set; }

			public Log Log
			{ get; private set; }

			private List<IDisposable> _subscriptions = new List<IDisposable>();

			private bool _disposed = false;

			public EventManager(Bot owner, CallbackManager callbackManager)
			{
				Bot = owner;
				Log = owner.Log;

				Log.Info("Subscribing events...");

				MethodInfo subscribeMethod = null;
				foreach (MethodInfo m in callbackManager.GetType().GetMethods())
				{
					if (m.Name == "Subscribe" && m.GetParameters().Length == 1)
					{
						subscribeMethod = m;
					}
				}

				List<MethodInfo> methods = BotEventAttribute.GetMethodsWithinType(GetType());
				foreach (MethodInfo m in methods)
				{
					ParameterInfo[] pars = m.GetParameters();
					if (pars.Length == 0)
					{
						Log.Error("Method is missing parameters: " + m.Name);
						continue;
					}
					if (pars.Length > 1)
					{
						Log.Error("Method has too many parameters: " + m.Name);
						continue;
					}

					Type parType = pars.First().ParameterType;
					if (!typeof(CallbackMsg).IsAssignableFrom(parType))
					{
						Log.Error("Method {0} has invalid parameter type: {1}", m.Name, parType.ToString());
						continue;
					}

					MethodInfo genericSubscribe = subscribeMethod.MakeGenericMethod(parType);
					object del = CreateDelegateByParameter(parType, this, m);

					// manager.Subscribe<parType>(del); // <-- essentially this
					object subRes = genericSubscribe.Invoke(callbackManager, new object[] { del });

					_subscriptions.Add(subRes as IDisposable);
				}

				Log.Success("{0} events subscribed.", _subscriptions.Count);
			}

			~EventManager()
			{
				Dispose(false);
			}

			#region reflection
			public static object CreateDelegateByParameter(Type parameterType, object target, MethodInfo method)
			{

				MethodInfo createDelegate = typeof(EventManager).GetMethod(
					"CreateDelegate").MakeGenericMethod(parameterType);

				object del = createDelegate.Invoke(null, new object[] { target, method });

				return del;
			}

			public static Action<T> CreateDelegate<T>(object target, MethodInfo method)
			{
				Action<T> del = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), target, method);

				return del;
			}
			#endregion reflection

			#region login
			[BotEvent]
			public void OnConnected(SteamClient.ConnectedCallback e)
			{
				Log.Debug("Connection Callback: {0}", e.Result);

				if (e.Result == EResult.OK)
				{
					Bot._userLogOn();
				}
				else
				{
					Log.Error("Failed to connect to Steam Community, trying again...");
					Bot.SteamClient.Connect();
				}
			}

			[BotEvent]
			public void OnLogon(SteamUser.LoggedOnCallback e)
			{
				Log.Debug("Logged On Callback: {0}", e.Result);

				if (e.Result == EResult.OK)
				{
					Bot._myUserNonce = e.WebAPIUserNonce;
				}
				else
				{
					Log.Error("Login Error: {0}", e.Result);
				}

				if (e.Result == EResult.InvalidPassword)
				{
					Log.Interface("Invalid password. Press enter to log in again.");
					Program.PasswordRequestingBot = Bot;
					Program.IsMaskedInput = true;

					while (Program.IsMaskedInput)
					{
						Thread.Sleep(2000);
					}
				}

				if (e.Result == EResult.AccountLogonDenied)
				{
					Log.Interface("This account is SteamGuard enabled. Enter the code via the 'auth' command.");

					// try to get the steamguard auth code from the event callback
					SteamGuardRequiredEventArgs eva = new SteamGuardRequiredEventArgs();
					Bot._fireOnSteamGuardRequired(eva);
					if (!string.IsNullOrEmpty(eva.SteamGuard))
						Bot.logOnDetails.AuthCode = eva.SteamGuard;
					else
						Bot.logOnDetails.AuthCode = Console.ReadLine();
				}

				if (e.Result == EResult.InvalidLoginAuthCode)
				{
					Log.Interface("The given SteamGuard code was invalid. Try again using the 'auth' command.");
					Bot.logOnDetails.AuthCode = Console.ReadLine();
				}
			}

			[BotEvent]
			public void OnLoginKey(SteamUser.LoginKeyCallback e)
			{
				Bot._myUniqueId = e.UniqueID.ToString();

				Bot._userWebLogOn();

				if (Trade.CurrentSchema == null)
				{
					Log.Info("Downloading Schema...");
					Trade.CurrentSchema = Schema.FetchSchema(Bot.ApiKey, Bot.schemaLang);
					Log.Success("Schema Downloaded!");
				}

				Bot.SteamFriends.SetPersonaName(Bot.DisplayNamePrefix + Bot.DisplayName);
				Bot.SteamFriends.SetPersonaState(EPersonaState.Online);

				Log.Success("Steam Bot Logged In Completely!");

				Bot._getUserHandler(Bot.SteamClient.SteamID).OnLoginCompleted();
			}

			[BotEvent]
			public void OnWebAPIUserNonce(SteamUser.WebAPIUserNonceCallback e)
			{
				Log.Debug("Received new WebAPIUserNonce.");

				if (e.Result == EResult.OK)
				{
					Bot._myUserNonce = e.Nonce;
					Bot._userWebLogOn();
				}
				else
				{
					Log.Error("WebAPIUserNonce Error: " + e.Result);
				}
			}

			[BotEvent]
			public void OnUpdateMachineAuth(SteamUser.UpdateMachineAuthCallback e)
			{
				Bot._onUpdateMachineAuthCallback(e);
			}
			#endregion login

			#region friends
			[BotEvent]
			public void OnFriendsList(SteamFriends.FriendsListCallback e)
			{
				foreach (SteamFriends.FriendsListCallback.Friend friend in e.FriendList)
				{
					if (Bot.Admins.Contains(friend.SteamID))
					{
						Bot.SteamFriends.SendChatMessage(friend.SteamID, EChatEntryType.ChatMsg, "Hello, sir.");
					}

					switch (friend.SteamID.AccountType)
					{
					case EAccountType.Clan:
						if (friend.Relationship == EFriendRelationship.RequestRecipient)
						{
							if (Bot._getUserHandler(friend.SteamID).OnGroupAdd())
							{
								Bot.AcceptGroupInvite(friend.SteamID);
							}
							else
							{
								Bot.DeclineGroupInvite(friend.SteamID);
							}
						}
						break;
					default:
						Bot._createFriendsListIfNecessary();
						if (friend.Relationship == EFriendRelationship.None)
						{
							Bot._friends.Remove(friend.SteamID);
							Bot._getUserHandler(friend.SteamID).OnFriendRemove();
							Bot._removeUserHandler(friend.SteamID);
						}
						else if (friend.Relationship == EFriendRelationship.RequestRecipient)
						{
							if (Bot._getUserHandler(friend.SteamID).OnFriendAdd())
							{
								if (!Bot._friends.Contains(friend.SteamID))
								{
									Bot._friends.Add(friend.SteamID);
								}
								else
								{
									Log.Error("Friend was added who was already in friends list: " + friend.SteamID);
								}
								Bot.SteamFriends.AddFriend(friend.SteamID);
							}
							else
							{
								Bot.SteamFriends.RemoveFriend(friend.SteamID);
								Bot._removeUserHandler(friend.SteamID);
							}
						}
						break;
					}
				}
			}

			[BotEvent]
			public void OnFriendMessage(SteamFriends.FriendMsgCallback e)
			{
				EChatEntryType type = e.EntryType;

				if (e.EntryType == EChatEntryType.ChatMsg)
				{
					Log.Info("Chat Message from {0}: {1}", Bot.SteamFriends.
						GetFriendPersonaName(e.Sender), e.Message);
					Bot._getUserHandler(e.Sender).OnMessageHandler(e.Message, type);
				}
			}

			[BotEvent]
			public void OnGroupChatMessage(SteamFriends.ChatMsgCallback e)
			{
				Bot._getUserHandler(e.ChatterID).OnChatRoomMessage(
					e.ChatRoomID, e.ChatterID, e.Message);
			}
			#endregion friends

			#region trading
			[BotEvent]
			public void OnTradeSessionStart(SteamTrading.SessionStartCallback e)
			{
				bool started = Bot.HandleTradeSessionStart(e.OtherClient);

				if (!started)
					Log.Error("Could not start the trade session.");
				else
					Log.Debug("SteamTrading.SessionStartCallback handled successfully. Trade Opened.");
			}

			[BotEvent]
			public void OnTradeProposed(SteamTrading.TradeProposedCallback e)
			{
				if (Bot._checkCookies() == false)
				{
					Bot.SteamTrade.RespondToTrade(e.TradeID, false);
					return;
				}

				try
				{
					Bot._tradeManager.InitializeTrade(Bot.SteamUser.SteamID, e.OtherClient);
				}
				catch (WebException we)
				{
					Bot.SteamFriends.SendChatMessage(e.OtherClient,
							 EChatEntryType.ChatMsg, "Trade error: " + we.Message);

					Bot.SteamTrade.RespondToTrade(e.TradeID, false);
					return;
				}
				catch (Exception)
				{
					Bot.SteamFriends.SendChatMessage(e.OtherClient, EChatEntryType.ChatMsg,
						"Trade declined. Could not correctly fetch your backpack.");

					Bot.SteamTrade.RespondToTrade(e.TradeID, false);
					return;
				}

				if (Bot._tradeManager.OtherInventory.IsPrivate)
				{
					Bot.SteamFriends.SendChatMessage(e.OtherClient, EChatEntryType.ChatMsg,
						"Trade declined. Your backpack cannot be private.");

					Bot.SteamTrade.RespondToTrade(e.TradeID, false);
					return;
				}

				if (Bot.CurrentTrade == null && Bot._getUserHandler(e.OtherClient).OnTradeRequest())
				{
					Bot.SteamTrade.RespondToTrade(e.TradeID, true);
				}
				else
				{
					Bot.SteamTrade.RespondToTrade(e.TradeID, false);
				}
			}

			[BotEvent]
			public void OnTradeResult(SteamTrading.TradeResultCallback e)
			{
				if (e.Response == EEconTradeResponse.Accepted)
				{
					Log.Debug("Trade Status: {0}", e.Response);
					Log.Info("Trade Accepted!");
					Bot._getUserHandler(e.OtherClient).OnTradeRequestReply(true, e.Response.ToString());
				}
				else
				{
					Log.Warn("Trade failed: {0}", e.Response);
					Bot.CloseTrade();
					Bot._getUserHandler(e.OtherClient).OnTradeRequestReply(false, e.Response.ToString());
				}
			}
			#endregion trading

			#region disconnect
			[BotEvent]
			public void OnLoggedOff(SteamUser.LoggedOffCallback e)
			{
				Bot.IsLoggedIn = false;
				Log.Warn("Logged off Steam. Reason: {0}", e.Result);
			}

			[BotEvent]
			public void OnDisconnected(SteamClient.DisconnectedCallback e)
			{
				if (Bot.IsLoggedIn)
				{
					Bot.IsLoggedIn = false;
					Bot.CloseTrade();
					Log.Warn("Disconnected from Steam Network!");
				}

				Bot.SteamClient.Connect();
			}
			#endregion disconnect

			#region notifications
			[BotEvent]
			public void OnNotification(SteamNotifications.NotificationCallback e)
			{
				//currently only appears to be of trade offer
				if (e.Notifications.Count != 0)
				{
					foreach (var notification in e.Notifications)
					{
						Log.Info(notification.UserNotificationType + " notification");
					}
				}

				// Get offers only if cookies are valid
				if (Bot._checkCookies())
				{
					Bot._tradeOfferManager.GetOffers();
				}
			}

			[BotEvent]
			public void OnCommentNotification(SteamNotifications.CommentNotificationCallback e)
			{
				//various types of comment notifications on profile/activity feed etc
				Log.Info("received CommentNotificationCallback");
				Log.Info("New Commments " + e.CommentNotifications.CountNewComments);
				Log.Info("New Commments Owners " + e.CommentNotifications.CountNewCommentsOwner);
				Log.Info("New Commments Subscriptions" + e.CommentNotifications.CountNewCommentsSubscriptions);
			}
			#endregion

			#region IDisposable
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			public void Dispose(bool recursive)
			{
				if (_disposed)
				{
					return;
				}

				if (recursive)
				{
					foreach (IDisposable d in _subscriptions)
					{
						d.Dispose();
					}
				}
				_disposed = true;
			}
			#endregion IDisposable
		}
	}
}
