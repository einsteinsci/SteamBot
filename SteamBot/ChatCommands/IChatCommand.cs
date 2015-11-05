using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot.ChatCommands
{
	public interface IChatCommand
	{
		/// <summary>
		/// Must be lowercase
		/// </summary>
		string CommandName
		{ get; }

		string Purpose
		{ get; }

		string Syntax
		{ get; }

		bool IsAdminOnly
		{ get; }

		bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMessage);
	}
}
