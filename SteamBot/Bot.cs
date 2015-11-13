using System;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Linq;
using System.Collections.Specialized;
using System.Security;
using System.Security.AccessControl;
using System.Windows.Forms;
using Microsoft.Win32;

using SteamKit2;
using SteamKit2.Internal;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using SteamTrade;
using SteamTrade.TradeOffer;

using SteamBot.SteamGroups;
using SteamBot.TF2GC;

namespace SteamBot
{
	public partial class Bot : IDisposable
	{
		public const string BPTF_TOKEN_SEALEDINTERFACE = "55f8e711b98d8871558b4601";
		public const string BPTF_TOKEN_SEALEDBOT = "5622ef31b98d88ea15cc1ac9";
		internal const string REGISTRY_KEY_PATH = "SOFTWARE\\SealedInterface\\SteamBot";
		
		public delegate UserHandler UserHandlerCreator(Bot bot, SteamID id);

		#region Private readonly variables
		[SecurityCritical]
		private readonly SteamUser.LogOnDetails logOnDetails;
		private readonly string schemaLang;
		private readonly string logFile;
		private readonly Dictionary<SteamID, UserHandler> userHandlers;
		private readonly Log.LogLevel consoleLogLevel;
		private readonly Log.LogLevel fileLogLevel;
		private readonly UserHandlerCreator createHandler;
		private readonly bool isProccess;
		private readonly BackgroundWorker botSteamThread;
		private readonly Thread heartBeatThread;
		private readonly Thread crafterThread;
		#endregion

		#region Private variables
		private Task<Inventory> _myInventoryTask;
		private TradeManager _tradeManager;
		private TradeOfferManager _tradeOfferManager;
		private int _tradePollingInterval;
		private string _myUserNonce;
		private string _myUniqueId;
		private bool _cookiesAreInvalid = true;
		private List<SteamID> _friends;
		private bool _disposed = false;
		#endregion

		#region Public readonly variables
		/// <summary>
		/// Userhandler class bot is running.
		/// </summary>
		public readonly string BotControlClass;
		/// <summary>
		/// The display name of bot to steam.
		/// </summary>
		public readonly string DisplayName;
		/// <summary>
		/// The chat response from the config file.
		/// </summary>
		public readonly string ChatResponse;
		/// <summary>
		/// An array of admins for bot.
		/// </summary>
		public readonly IEnumerable<SteamID> Admins;
		public readonly SteamClient SteamClient;
		public readonly SteamUser SteamUser;
		public readonly SteamFriends SteamFriends;
		public readonly SteamTrading SteamTrade;
		public readonly SteamGameCoordinator SteamGameCoordinator;
		public readonly SteamNotifications SteamNotifications;

		public readonly EventManager EventMgr;
		public readonly CallbackManager CallbackMgr;
		/// <summary>
		/// The amount of time the bot will trade for.
		/// </summary>
		public readonly int MaximumTradeTime;
		/// <summary>
		/// The amount of time the bot will wait between user interactions with trade.
		/// </summary>
		public readonly int MaximumActionGap;
		/// <summary>
		/// The api key of bot.
		/// </summary>
		public readonly string ApiKey;
		public readonly SteamWeb SteamWeb;
		/// <summary>
		/// The prefix shown before bot's display name.
		/// </summary>
		public readonly string DisplayNamePrefix;
		/// <summary>
		/// The instance of the Logger for the bot.
		/// </summary>
		public readonly Log Log;
		#endregion

		#region Public variables
		public string AuthCode;
		public bool IsRunning;
		/// <summary>
		/// Is bot fully Logged in.
		/// Set only when bot did successfully Log in.
		/// </summary>
		public bool IsLoggedIn
		{ get; private set; }

		/// <summary>
		/// The current trade the bot is in.
		/// </summary>
		public Trade CurrentTrade
		{ get; private set; }

		/// <summary>
		/// The current game bot is in.
		/// Default: 0 = No game.
		/// </summary>
		public int CurrentGame
		{ get; private set; }

		public OrderManager Orders
		{ get; private set; }
		#endregion

		public IEnumerable<SteamID> FriendsList
		{
			get
			{
				_createFriendsListIfNecessary();
				return _friends;
			}
		}

		public Inventory MyInventory
		{
			get
			{
				_myInventoryTask.Wait();
				return _myInventoryTask.Result;
			}
		}

		public Bot(Configuration.BotInfo config, string apiKey, UserHandlerCreator handlerCreator, bool debug = false,
			bool process = false)
		{
			userHandlers = new Dictionary<SteamID, UserHandler>();
			logOnDetails = new SteamUser.LogOnDetails
			{
				Username = config.Username,
				Password = _sec_RetrievePassword(config.Username)
			};
			DisplayName  = config.DisplayName;
			ChatResponse = config.ChatResponse;
			MaximumTradeTime = config.MaximumTradeTime;
			MaximumActionGap = config.MaximumActionGap;
			DisplayNamePrefix = config.DisplayNamePrefix;
			_tradePollingInterval = config.TradePollingInterval <= 100 ? 800 : config.TradePollingInterval;
			schemaLang = config.SchemaLang != null ? config.SchemaLang : "en_US";
			Admins = config.Admins;
			ApiKey = !string.IsNullOrEmpty(config.ApiKey) ? config.ApiKey : apiKey;
			isProccess = process;
			try
			{
				if( config.LogLevel != null )
				{
					consoleLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.LogLevel, true);
					Console.WriteLine("(Console) LogLevel configuration parameter used in bot {0} is depreciated and may be " + 
						"removed in future versions. Please use ConsoleLogLevel instead.", DisplayName);
				}
				else consoleLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.ConsoleLogLevel, true);
			}
			catch (ArgumentException)
			{
				Console.WriteLine("(Console) ConsoleLogLevel invalid or unspecified for bot {0}. Defaulting to 'Info'", DisplayName);
				consoleLogLevel = Log.LogLevel.Info;
			}

			try
			{
				fileLogLevel = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), config.FileLogLevel, true);
			}
			catch (ArgumentException)
			{
				Console.WriteLine("(Console) FileLogLevel invalid or unspecified for bot {0}. Defaulting to 'Info'", DisplayName);
				fileLogLevel = Log.LogLevel.Info;
			}

			logFile = config.LogFile;
			Log = new Log(logFile, DisplayName, consoleLogLevel, fileLogLevel);
			createHandler = handlerCreator;
			BotControlClass = config.BotControlClass;
			SteamWeb = new SteamWeb();

			Orders = OrderManager.Load(Log);

			// Hacking around https
			ServicePointManager.ServerCertificateValidationCallback += SteamWeb.ValidateRemoteCertificate;

			Log.Debug("Initializing Steam Bot...");

			SteamClient = new SteamClient();
			SteamClient.AddHandler(new SteamNotifications());
			SteamTrade = SteamClient.GetHandler<SteamTrading>();
			SteamUser = SteamClient.GetHandler<SteamUser>();
			SteamFriends = SteamClient.GetHandler<SteamFriends>();
			SteamGameCoordinator = SteamClient.GetHandler<SteamGameCoordinator>();
			SteamNotifications = SteamClient.GetHandler<SteamNotifications>();

			CallbackMgr = new CallbackManager(SteamClient);

			EventMgr = new EventManager(this, CallbackMgr);

			botSteamThread = new BackgroundWorker { WorkerSupportsCancellation = true };
			botSteamThread.DoWork += _backgroundWorkerOnDoWork;
			botSteamThread.RunWorkerCompleted += _backgroundWorkerOnRunWorkerCompleted;
			botSteamThread.RunWorkerAsync();

			heartBeatThread = new Thread(HeartbeatLoop);
			heartBeatThread.Name = "bp.tf Heartbeat Thread: " + config.Username;
			heartBeatThread.Start();

			crafterThread = new Thread(CrafterLoop);
			crafterThread.Name = "Crafting Loop Thread: " + config.Username;
			crafterThread.Start();
		}

		~Bot()
		{
			_dispose(false);
		}

		private void _createFriendsListIfNecessary()
		{
			if (_friends != null)
				return;

			ResetFriendsList();
		}

		public void ResetFriendsList()
		{
			_friends = new List<SteamID>();
			for (int i = 0; i < SteamFriends.GetFriendCount(); i++)
				_friends.Add(SteamFriends.GetFriendByIndex(i));

			Log.Debug("Created friends list.");
		}

		/// <summary>
		/// Occurs when the bot needs the SteamGuard authentication code.
		/// </summary>
		/// <remarks>
		/// Return the code in <see cref="SteamGuardRequiredEventArgs.SteamGuard"/>
		/// </remarks>
		public event EventHandler<SteamGuardRequiredEventArgs> OnSteamGuardRequired;

		/// <summary>
		/// Starts the callback thread and connects to Steam via SteamKit2.
		/// </summary>
		/// <remarks>
		/// THIS NEVER RETURNS.
		/// </remarks>
		/// <returns><c>true</c>. See remarks</returns>
		public bool StartBot()
		{
			IsRunning = true;
			Log.Info("Connecting...");
			if (!botSteamThread.IsBusy)
				botSteamThread.RunWorkerAsync();
			SteamClient.Connect();
			Log.Success("Done Loading Bot!");
			return true; // never get here
		}

		/// <summary>
		/// Disconnect from the Steam network and stop the callback
		/// thread.
		/// </summary>
		public void StopBot()
		{
			IsRunning = false;

			if (!_disposed)
				Log.Debug("Trying to shut down bot thread.");

			SteamClient.Disconnect();
			botSteamThread.CancelAsync();
			heartBeatThread.Abort();
			crafterThread.Abort();

			while (botSteamThread.IsBusy)
				Thread.Yield();

			userHandlers.Clear();
		}

		/// <summary>
		/// Creates a new trade with the given partner.
		/// </summary>
		/// <returns>
		/// <c>true</c>, if trade was opened,
		/// <c>false</c> if there is another trade that must be closed first.
		/// </returns>
		public bool OpenTrade(SteamID other)
		{
			if (CurrentTrade != null || _checkCookies() == false)
				return false;
			SteamTrade.Trade(other);
			return true;
		}

		/// <summary>
		/// Closes the current active trade.
		/// </summary>
		public void CloseTrade() 
		{
			if (CurrentTrade == null)
				return;
			UnsubscribeTrade (_getUserHandler (CurrentTrade.OtherSID), CurrentTrade);
			_tradeManager.StopTrade ();
			CurrentTrade = null;
		}

		private void _onTradeTimeout(object sender, EventArgs args) 
		{
			// ignore event params and just null out the trade.
			_getUserHandler(CurrentTrade.OtherSID).OnTradeTimeout();
		}

		/// <summary>
		/// Create a new trade offer with the specified partner
		/// </summary>
		/// <param name="other">SteamId of the partner</param>
		/// <returns></returns>
		public TradeOffer NewTradeOffer(SteamID other)
		{
			return _tradeOfferManager.NewOffer(other);
		}

		/// <summary>
		/// Try to get a specific trade offer using the offerid
		/// </summary>
		/// <param name="offerId"></param>
		/// <param name="tradeOffer"></param>
		/// <returns></returns>
		public bool TryGetTradeOffer(string offerId, out TradeOffer tradeOffer)
		{
			return _tradeOfferManager.GetOffer(offerId, out tradeOffer);
		}

		public void HandleBotCommand(string command)
		{
			try
			{
				_getUserHandler(SteamClient.SteamID).OnBotCommand(command);
			}
			catch (ObjectDisposedException e)
			{
				// Writing to console because odds are the error was caused by a disposed Log.
				Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
				if (!IsRunning)
				{
					Console.WriteLine("The Bot is no longer running and could not write to the Log. Try starting this bot first.");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(string.Format("Exception caught in BotCommand Thread: {0}", e));
			}
		}

		public bool HandleTradeSessionStart(SteamID other)
		{
			if (CurrentTrade != null)
				return false;
			try
			{
				_tradeManager.InitializeTrade(SteamUser.SteamID, other);
				CurrentTrade = _tradeManager.CreateTrade(SteamUser.SteamID, other);
				CurrentTrade.OnClose += CloseTrade;
				SubscribeTrade(CurrentTrade, _getUserHandler(other));
				_tradeManager.StartTradeThread(CurrentTrade);
				return true;
			}
			catch (SteamTrade.Exceptions.InventoryFetchException)
			{
				// we shouldn't get here because the inv checks are also
				// done in the TradeProposedCallback handler.
				/*string response = String.Empty;
				if (ie.FailingSteamId.ConvertToUInt64() == other.ConvertToUInt64())
				{
					response = "Trade failed. Could not correctly fetch your backpack. Either the inventory is inaccessible or your backpack is private.";
				}
				else 
				{
					response = "Trade failed. Could not correctly fetch my backpack.";
				}
				
				SteamFriends.SendChatMessage(other, 
											 EChatEntryType.ChatMsg,
											 response);

				Log.Info ("Bot sent other: {0}", response);
				
				CurrentTrade = null;*/
				return false;
			}
		}

		public void SetGamePlaying(int id)
		{
			var gamePlaying = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
			if (id != 0)
			{
				gamePlaying.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
				{
					game_id = new GameID(id)
				});
			}
			SteamClient.Send(gamePlaying);
			CurrentGame = id;
		}

		private void _userLogOn()
		{
			// get sentry file which has the machine hw info saved 
			// from when a steam guard code was entered
			Directory.CreateDirectory(Path.Combine(System.Windows.Forms.Application.StartupPath, "sentryfiles"));
			FileInfo fi = new FileInfo(Path.Combine("sentryfiles", string.Format("{0}.sentryfile", logOnDetails.Username)));

			if (fi.Exists && fi.Length > 0)
			{
				logOnDetails.SentryFileHash = _sec_SHAHash(File.ReadAllBytes(fi.FullName));
				Log.Debug("Sentry file read.");
			}
			else
			{
				logOnDetails.SentryFileHash = null;
				Log.Warn("Sentry file is null.");
			}

			SteamUser.LogOn(logOnDetails);
		}

		private void _userWebLogOn()
		{
			do
			{
				IsLoggedIn = SteamWeb.Authenticate(_myUniqueId, SteamClient, _myUserNonce);

				if(!IsLoggedIn)
				{
					Log.Warn("Authentication failed, retrying in 2s...");
					Thread.Sleep(2000);
				}
			} while(!IsLoggedIn);

			Log.Success("User Authenticated!");

			_tradeManager = new TradeManager(ApiKey, SteamWeb, _sendChatToEveryone);
			_tradeManager.SetTradeTimeLimits(MaximumTradeTime, MaximumActionGap, _tradePollingInterval);
			_tradeManager.OnTimeout += _onTradeTimeout;
			_tradeOfferManager = new TradeOfferManager(ApiKey, SteamWeb);
			_tradeOfferManager.LogDebug = (s) => Log.Debug(s);
			_tradeOfferManager.LogError = (s) => Log.Error(s);
			SubscribeTradeOffer(_tradeOfferManager);
			_cookiesAreInvalid = false;
			// Success, check trade offers which we have received while we were offline
			_tradeOfferManager.GetOffers();
		}

		/// <summary>
		/// Checks if sessionId and token cookies are still valid.
		/// Sets cookie flag if they are invalid.
		/// </summary>
		/// <returns>true if cookies are valid; otherwise false</returns>
		private bool _checkCookies()
		{
			// We still haven't re-authenticated
			if (_cookiesAreInvalid)
				return false;

			try
			{
				if (!SteamWeb.VerifyCookies())
				{
					// Cookies are no longer valid
					Log.Warn("Cookies are invalid. Need to re-authenticate.");
					_cookiesAreInvalid = true;
					SteamUser.RequestWebAPIUserNonce();
					return false;
				}
			}
			catch
			{
				// Even if exception is caught, we should still continue.
				Log.Warn("Cookie check failed. http://api.steamcommunity.com is possibly down.");
			}

			return true;
		}

		private UserHandler _getUserHandler(SteamID sid)
		{
			if (!userHandlers.ContainsKey(sid))
				userHandlers[sid] = createHandler(this, sid);
			return userHandlers[sid];
		}

		public void RemoveUserHandler(SteamID sid)
		{
			if (userHandlers.ContainsKey(sid))
				userHandlers.Remove(sid);
		}

		#region security
		[SecuritySafeCritical]
		internal void DoSetPassword()
		{
			logOnDetails.Password = _sec_RetrievePassword(logOnDetails.Username, true);
		}

		[SecuritySafeCritical]
		private static string _sec_RetrievePassword(string username, bool force = false)
		{
			bool couldRetrieveFromRegistry = true;
			RegistryKey reg = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH + "\\" + username);
			if (reg != null && !force)
			{
				object objEntropy = reg.GetValue("datum_0");
				object objCypherText = reg.GetValue("datum_1");
				reg.Close();

				if (objEntropy == null || objCypherText == null)
				{
					couldRetrieveFromRegistry = false;
				}
				else
				{
					byte[] cypherText = (byte[])objCypherText;
					byte[] entropy = (byte[])objEntropy;

					byte[] plainText = ProtectedData.Unprotect(cypherText, entropy, DataProtectionScope.CurrentUser);
					string password = Encoding.UTF8.GetString(plainText);

					return password;
				}

			}
			else
			{
				couldRetrieveFromRegistry = false;
			}

			if (!couldRetrieveFromRegistry)
			{
				Console.WriteLine("Password not found for user {0}.", username);
				Console.Write("  Password: ");
				string password = _sec_ReadPassword();

				byte[] plainText = Encoding.UTF8.GetBytes(password);
				byte[] entropy = new byte[64];
				using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
				{
					rng.GetBytes(entropy);
				}
				byte[] cypherText = ProtectedData.Protect(plainText, entropy, DataProtectionScope.CurrentUser);

				RegistryKey myReg = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH + "\\" + username);
				myReg.SetAccessControl(_sec_MakeRegSecurity());

				myReg.SetValue("datum_0", entropy);
				myReg.SetValue("datum_1", cypherText);

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Password saved.");
				Console.ForegroundColor = ConsoleColor.White;

				myReg.Close();

				return password;
			}
			
			return null; // this can't happen
		}

		[SecuritySafeCritical]
		private static RegistrySecurity _sec_MakeRegSecurity()
		{
			string user = Environment.UserDomainName + "\\" + Environment.UserName;

			RegistrySecurity rs = new RegistrySecurity();

			rs.AddAccessRule(new RegistryAccessRule(user,
				RegistryRights.ReadKey | RegistryRights.Delete | RegistryRights.WriteKey,
				InheritanceFlags.None,
				PropagationFlags.None,
				AccessControlType.Allow));
			
			rs.AddAccessRule(new RegistryAccessRule(user,
				RegistryRights.ChangePermissions,
				InheritanceFlags.None,
				PropagationFlags.None,
				AccessControlType.Deny));

			return rs;
		}

		[SecuritySafeCritical]
		private static string _sec_ReadPassword(char mask = '*')
		{
			const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
			int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

			var pass = new Stack<char>();
			char chr = (char)0;

			while ((chr = Console.ReadKey(true).KeyChar) != ENTER)
			{
				if (chr == BACKSP)
				{
					if (pass.Count > 0)
					{
						Console.Write("\b \b");
						pass.Pop();
					}
				}
				else if (chr == CTRLBACKSP)
				{
					while (pass.Count > 0)
					{
						Console.Write("\b \b");
						pass.Pop();
					}
				}
				else if (FILTERED.Count(x => chr == x) > 0)
				{ }
				else
				{
					pass.Push(chr);
					Console.Write(mask);
				}
			}

			Console.WriteLine();

			return new string(pass.Reverse().ToArray());
		}

		[SecuritySafeCritical]
		private static byte[] _sec_SHAHash(byte[] input)
		{
			SHA1Managed sha = new SHA1Managed();
			
			byte[] output = sha.ComputeHash( input );
			
			sha.Clear();
			
			return output;
		}
		#endregion security

		private void _onUpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth)
		{
			byte[] hash = _sec_SHAHash (machineAuth.Data);

			Directory.CreateDirectory(Path.Combine(Application.StartupPath, "sentryfiles"));

			File.WriteAllBytes(Path.Combine("sentryfiles", string.Format(
				"{0}.sentryfile", logOnDetails.Username)), machineAuth.Data);
			
			var authResponse = new SteamUser.MachineAuthDetails
			{
				BytesWritten = machineAuth.BytesToWrite,
				FileName = machineAuth.FileName,
				FileSize = machineAuth.BytesToWrite,
				Offset = machineAuth.Offset,
				
				SentryFileHash = hash, // should be the sha1 hash of the sentry file we just wrote
				
				OneTimePassword = machineAuth.OneTimePassword, // not sure on this one yet, since we've had no examples of steam using OTPs
				
				LastError = 0, // result from win32 GetLastError
				Result = EResult.OK, // if everything went okay, otherwise ~who knows~
				JobID = machineAuth.JobID, // so we respond to the correct server job
			};
			
			// send off our response
			SteamUser.SendMachineAuthResponse(authResponse);
			Log.Debug("Sent Machine AUTH response.");
		}

		private void _fireOnSteamGuardRequired(SteamGuardRequiredEventArgs e)
		{
			// Set to null in case this is another attempt
			this.AuthCode = null;

			EventHandler<SteamGuardRequiredEventArgs> handler = OnSteamGuardRequired;
			if (handler != null)
				handler(this, e);
			else
			{
				while (true)
				{
					if (this.AuthCode != null)
					{
						e.SteamGuard = this.AuthCode;
						break;
					}

					Thread.Sleep(5);
				}
			}
		}

		/// <summary>
		/// Gets the bot's inventory and stores it in MyInventory.
		/// </summary>
		/// <example> This sample shows how to find items in the bot's inventory from a user handler.
		/// <code>
		/// Bot.GetInventory(); // Get the inventory first
		/// foreach (var item in Bot.MyInventory.Items)
		/// {
		///	 if (item.Defindex == 5021)
		///	 {
		///		 // Bot has a key in its inventory
		///	 }
		/// }
		/// </code>
		/// </example>
		public void GetInventory()
		{
			_myInventoryTask = Task.Factory.StartNew(_fetchBotsInventory);
		}

		#region subscription methods
		public void TradeOfferRouter(TradeOffer offer)
		{
			if (offer.OfferState == TradeOfferState.TradeOfferStateActive)
			{
				_getUserHandler(offer.PartnerSteamId).OnNewTradeOffer(offer);
			}
		}
		public void SubscribeTradeOffer(TradeOfferManager tradeOfferManager)
		{
			tradeOfferManager.OnNewTradeOffer += TradeOfferRouter;
		}

		//todo: should unsubscribe eventually...
		public void UnsubscribeTradeOffer(TradeOfferManager tradeOfferManager)
		{
			tradeOfferManager.OnNewTradeOffer -= TradeOfferRouter;
		}

		/// <summary>
		/// Subscribes all listeners of this to the trade.
		/// </summary>
		public void SubscribeTrade(Trade trade, UserHandler handler)
		{
			trade.OnSuccess += handler.OnTradeSuccess;
			trade.OnAwaitingEmailConfirmation += handler.OnTradeAwaitingEmailConfirmation;
			trade.OnClose += handler.OnTradeClose;
			trade.OnError += handler.OnTradeError;
			trade.OnStatusError += handler.OnStatusError;
			//trade.OnTimeout += OnTradeTimeout;
			trade.OnAfterInit += handler.OnTradeInit;
			trade.OnUserAddItem += handler.OnTradeAddItem;
			trade.OnUserRemoveItem += handler.OnTradeRemoveItem;
			trade.OnMessage += handler.OnTradeMessageHandler;
			trade.OnUserSetReady += handler.OnTradeReadyHandler;
			trade.OnUserAccept += handler.OnTradeAcceptHandler;
		}
		
		/// <summary>
		/// Unsubscribes all listeners of this from the current trade.
		/// </summary>
		public void UnsubscribeTrade (UserHandler handler, Trade trade)
		{
			trade.OnSuccess -= handler.OnTradeSuccess;
			trade.OnAwaitingEmailConfirmation -= handler.OnTradeAwaitingEmailConfirmation;
			trade.OnClose -= handler.OnTradeClose;
			trade.OnError -= handler.OnTradeError;
			trade.OnStatusError -= handler.OnStatusError;
			//Trade.OnTimeout -= OnTradeTimeout;
			trade.OnAfterInit -= handler.OnTradeInit;
			trade.OnUserAddItem -= handler.OnTradeAddItem;
			trade.OnUserRemoveItem -= handler.OnTradeRemoveItem;
			trade.OnMessage -= handler.OnTradeMessageHandler;
			trade.OnUserSetReady -= handler.OnTradeReadyHandler;
			trade.OnUserAccept -= handler.OnTradeAcceptHandler;
		}
		#endregion subscription methods

		/// <summary>
		/// Fetch the Bot's inventory and log a warning if it's private
		/// </summary>
		private Inventory _fetchBotsInventory()
		{
			try
			{
				var inventory = Inventory.FetchInventory(SteamUser.SteamID, ApiKey, SteamWeb, _sendChatToEveryone);
				if (inventory.IsPrivate)
				{
					Log.Warn("The bot's backpack is private! If your bot adds any items it will fail! Your bot's backpack should be Public.");
				}
				return inventory;
			}
			catch (Exception e)
			{
				Log.Error("FAILED TO FETCH BOT INVENTORY! Exception: " + e.ToString());
				return null;
			}
		}

		private void _sendChatToEveryone(string msg)
		{
			foreach (UserHandler h in userHandlers.Values)
			{
				if (SteamFriends.GetFriendRelationship(h.OtherSID) != EFriendRelationship.Friend)
					continue;

				h.SendChatMessage(msg);
			}
		}

		#region Background Worker Methods

		private void _backgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
		{
			if (runWorkerCompletedEventArgs.Error != null)
			{
				Exception ex = runWorkerCompletedEventArgs.Error;

				Log.Error("Unhandled exceptions in bot {0} callback thread: {1} {2}",
					  DisplayName,
					  Environment.NewLine,
					  ex);

				Log.Info("This bot died. Stopping it..");
				//backgroundWorker.RunWorkerAsync();
				//Thread.Sleep(10000);
				StopBot();
				//StartBot();
			}
		}

		private void _backgroundWorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			while (!botSteamThread.CancellationPending)
			{
				try
				{
					//msg = SteamClient.WaitForCallback(true);
					//HandleSteamMessage(msg);
					CallbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(2));
				}
				catch (WebException e)
				{
					Log.Error("URI: {0} >> {1}", (e.Response != null && 
						e.Response.ResponseUri != null ? e.Response.ResponseUri.ToString() : "UNKNOWN"), e.ToString());
					Thread.Sleep(45000); //Steam is down, retry in 45 seconds.
				}
				catch (Exception e)
				{
					Log.Error("Exception in bot thread:");
					Log.Error(e.ToString());
					Log.Warn("Restarting bot...");
				}
			}
		}

		public void HeartbeatLoop()
		{
			DateTime lastHeartbeat = DateTime.Now.Subtract(TimeSpan.FromMinutes(4.5));

			while (!IsRunning)
			{
				Thread.Sleep(1000);
			}

			while (IsRunning)
			{
				try
				{
					if (DateTime.Now.Subtract(lastHeartbeat).TotalMinutes > 5.0)
					{
						bool success = SendBpTfHeartbeat();
						if (success)
							Log.Info("bp.tf Heartbeat sent.");
						else
							Log.Error("bp.tf Heartbeat failed.");

						lastHeartbeat = DateTime.Now;
					}

					if (Thread.CurrentThread.ThreadState == ThreadState.Running)
					{
						Thread.Sleep(1000);
					}
				}
				catch (Exception e)
				{
					Log.Error("backpack.tf heartbeat failed: " + e.ToString());
				}
			}
		}

		public bool SendBpTfHeartbeat()
		{
			const string BPTF_HEARTBEAT_URL = "http://backpack.tf/api/IAutomatic/IHeartBeat";

			using (WebClient client = new WebClient())
			{
				NameValueCollection data = new NameValueCollection();
				data.Add("method", "alive");
				data.Add("version", logOnDetails.Username + "-v0.1.0");
				data.Add("steamid", SteamUser.SteamID.ConvertToUInt64().ToString());
				data.Add("token", BPTF_TOKEN_SEALEDBOT);

				byte[] response = client.UploadValues(BPTF_HEARTBEAT_URL, data);

				string result = Encoding.UTF8.GetString(response);
				dynamic json = JValue.Parse(result);
				int success = json.success;

				if (success == 0)
				{
					Log.Error("backpack.tf heartbeat failed: " + json.message);
				}

				return success != 0;
			}
		}

		public void CrafterLoop()
		{
			Thread.Sleep(5000);

			DateTime lastCrafterLoop = DateTime.Now.Subtract(TimeSpan.FromMinutes(4.9));

			while (IsRunning)
			{
				if (DateTime.Now.Subtract(lastCrafterLoop).TotalMinutes > 5.0)
				{
					try
					{
						DoCraftingIfNeeded();
						lastCrafterLoop = DateTime.Now;
					}
					catch (Exception e)
					{
						Log.Error("Exception in crafting loop: " + e.Message);
					}

				}

				Thread.Sleep(5000);
			}
		}

		public void DoCraftingIfNeeded()
		{
			GetInventory();
			SetGamePlaying(440);

			while (MyInventory == null)
			{
				Log.Debug("MyInventory is null for some reason.");
				Thread.Sleep(2000);
			}

			bool didSomething = false;
			do
			{
				int totalScrap = 0, totalRec = 0;
				foreach (Inventory.Item item in MyInventory.Items)
				{
					if (item.Defindex == TF2Value.SCRAP_DEFINDEX)
						totalScrap++;
					else if (item.Defindex == TF2Value.RECLAIMED_DEFINDEX)
						totalRec++;
				}

				if (totalScrap > 4)
				{
					List<ulong> assets = _getAssetIDsInBackpack(TF2Value.SCRAP_DEFINDEX, 3);
					Crafting.CraftItems(this, ECraftingRecipe.CombineScrap, assets.ToArray());
					Log.Info("Crafted scrap into reclaimed.");
					didSomething = true;
				}

				if (totalScrap < 2)
				{
					List<ulong> assets = _getAssetIDsInBackpack(TF2Value.RECLAIMED_DEFINDEX, 1);
					Crafting.CraftItems(this, ECraftingRecipe.SmeltReclaimed, assets.ToArray());
					Log.Info("Smelted reclaimed into scrap.");
					didSomething = true;
				}

				if (totalRec > 4)
				{
					List<ulong> assets = _getAssetIDsInBackpack(TF2Value.RECLAIMED_DEFINDEX, 3);
					Crafting.CraftItems(this, ECraftingRecipe.CombineReclaimed, assets.ToArray());
					Log.Info("Crafted reclaimed into refined.");
					didSomething = true;
				}

				if (totalRec < 2)
				{
					List<ulong> assets = _getAssetIDsInBackpack(TF2Value.REFINED_DEFINDEX, 1);
					Crafting.CraftItems(this, ECraftingRecipe.SmeltRefined, assets.ToArray());
					Log.Info("Smelted refined into reclaimed.");
					didSomething = true;
				}

				Thread.Sleep(1000);
				SetGamePlaying(0);
				GetInventory();
			} while (didSomething);
		}

		private List<ulong> _getAssetIDsInBackpack(ushort defindex, int maxCount = 0)
		{
			List<ulong> res = new List<ulong>();
			foreach (Inventory.Item i in MyInventory.Items)
			{
				if (i.Defindex == defindex)
				{
					res.Add(i.Id);
				}

				if (res.Count == maxCount && maxCount > 0)
				{
					break;
				}
			}

			return res;
		}

		public void DoDelayedStuff(TimeSpan delay, Action thing)
		{
			Thread delayedThingThread = new Thread(() =>
			{
				Thread.Sleep(delay);
				thing();
			});
			delayedThingThread.Name = "Delayed Action Thread";
			delayedThingThread.IsBackground = true;
			delayedThingThread.Start();
		}

		#endregion Background Worker Methods

		#region Group Methods

		/// <summary>
		/// Accepts the invite to a Steam Group
		/// </summary>
		/// <param name="group">SteamID of the group to accept the invite from.</param>
		private void AcceptGroupInvite(SteamID group)
		{
			var acceptMsg = new ClientMsg<CMsgGroupInviteAction>((int)EMsg.ClientAcknowledgeClanInvite);

			acceptMsg.Body.GroupID = group.ConvertToUInt64();
			acceptMsg.Body.AcceptInvite = true;

			SteamClient.Send(acceptMsg);
			Log.Success("Accepted group invite to {0}.", group.ToString());
		}

		/// <summary>
		/// Declines the invite to a Steam Group
		/// </summary>
		/// <param name="group">SteamID of the group to decline the invite from.</param>
		private void DeclineGroupInvite(SteamID group)
		{
			var declineMsg = new ClientMsg<CMsgGroupInviteAction>((int)EMsg.ClientAcknowledgeClanInvite);

			declineMsg.Body.GroupID = group.ConvertToUInt64();
			declineMsg.Body.AcceptInvite = false;

			SteamClient.Send(declineMsg);
			Log.Info("Declined group invite to {0}.", group.ToString());
		}

		/// <summary>
		/// Invites a use to the specified Steam Group
		/// </summary>
		/// <param name="user">SteamID of the user to invite.</param>
		/// <param name="groupId">SteamID of the group to invite the user to.</param>
		public void InviteUserToGroup(SteamID user, SteamID groupId)
		{
			var InviteUser = new ClientMsg<CMsgInviteUserToGroup>((int)EMsg.ClientInviteUserToClan);

			InviteUser.Body.GroupID = groupId.ConvertToUInt64();
			InviteUser.Body.Invitee = user.ConvertToUInt64();
			InviteUser.Body.UnknownInfo = true;

			this.SteamClient.Send(InviteUser);
		}

		#endregion

		public void Dispose()
		{
			_dispose(true);
			GC.SuppressFinalize(this);
		}

		private void _dispose(bool recursive)
		{
			if (_disposed)
				return;
			StopBot();
			if (recursive)
			{
				Log.Dispose();
				EventMgr.Dispose();
			}
			_disposed = true;
		}
	}
}
