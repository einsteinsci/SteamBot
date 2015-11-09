using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SteamBot.ChatCommands
{
	[ChatCommand]
	public class CmdFeedback : IChatCommand
	{
		public string CommandName => "feedback";

		public bool IsAdminOnly => false;

		public string Purpose => "Provides the owner with feedback about the bot's functionality.";

		public string Syntax => "feedback {message}";

		public static readonly string FEEDBACK_FILE_PATH =
			Environment.GetEnvironmentVariable("appdata") + "\\SteamBot\\feedback.json";

		public bool RunCommand(List<string> args, UserHandler handler, Action<string> sendChatMessage)
		{
			try
			{
				string fileContents;
				if (File.Exists(FEEDBACK_FILE_PATH))
				{
					fileContents = File.ReadAllText(FEEDBACK_FILE_PATH);
				}
				else
				{
					fileContents = null;
				}

				Dictionary<string, string> feedbackData;
				if (fileContents == null)
				{
					feedbackData = new Dictionary<string, string>();
				}
				else
				{
					feedbackData = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents);
				}

				string provided = string.Join(" ", args);

				if (feedbackData.ContainsKey(handler.OtherSID.ToString()))
				{
					feedbackData[handler.OtherSID.ToString()] = provided;
				}
				else
				{
					feedbackData.Add(handler.OtherSID.ToString(), provided);
				}

				fileContents = JsonConvert.SerializeObject(feedbackData);
				File.WriteAllText(FEEDBACK_FILE_PATH, fileContents);

				sendChatMessage("Thank you for your input.");
				return true;
			}
			catch (Exception e)
			{
				sendChatMessage("I seem to have encountered an error while sending the feedback.");
				handler.Log.Error("Error in command '{0}': {1}", CommandName, e.ToString());
			}

			return false;
		}
	}
}
