using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SteamBot.ChatCommands;
using SteamKit2;

namespace SteamBot
{
	public static class ChatHandler
	{
		public static List<IChatCommand> ChatCommands
		{ get; private set; }

		static ChatHandler()
		{
			ChatCommands = new List<IChatCommand>();

			List<Type> allCommandTypes = ChatCommandAttribute.GetAllUsingTypes(
				Assembly.GetAssembly(typeof(ChatHandler)));

			foreach (Type t in allCommandTypes)
			{
				IChatCommand cmd = Activator.CreateInstance(t) as IChatCommand;

				if (cmd != null)
				{
					ChatCommands.Add(cmd);
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("[ERROR]: Chat command type does not inherit from IChatCommand: " + t.Name);
				}
			}
		}

		public static bool RunCommand(string cmdName, List<string> args, UserHandler handler)
		{
			foreach (IChatCommand cmd in ChatCommands)
			{
				if (cmd.CommandName == cmdName.ToLower())
				{
					if (!handler.IsAdmin && cmd.IsAdminOnly)
					{
						sendChatMessage(handler, "Sorry, but that command is reserved for admins.");
						handler.Log.Warn("User {0} attempted to use admin command '{1}'.", 
							handler.OtherSID.ToString(), cmd.CommandName);
						return false;
					}

					handler.Log.Info("User {0} started command '{1}'.",
						handler.OtherSID.ToString(), cmd.CommandName);
					return cmd.RunCommand(args, handler, (msg) => sendChatMessage(handler, msg));
				}
			}

			handler.Log.Warn("User {0} attempted to use nonexistent command '{1}'.",
				handler.OtherSID.ToString(), cmdName);
			sendChatMessage(handler, "I'm sorry, but that command is not recognized. " + 
				"Type 'help' for a full list of commands.");
			return false;
		}

		static void sendChatMessage(UserHandler handler, string message)
		{
			handler.Bot.SteamFriends.SendChatMessage(handler.OtherSID, EChatEntryType.ChatMsg, message);
			handler.Log.Debug("Sent chat message to user {0}: {1}.", handler.OtherSID, message);
		}
	}
}
