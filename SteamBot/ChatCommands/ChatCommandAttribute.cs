using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot.ChatCommands
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class ChatCommandAttribute : Attribute
	{
		public ChatCommandAttribute()
		{ }

		public static List<Type> GetAllUsingTypes(Assembly assembly)
		{
			Type[] allTypes = assembly.GetTypes();

			List<Type> result = new List<Type>();
			foreach (Type t in allTypes)
			{
				if (!typeof(IChatCommand).IsAssignableFrom(t))
				{
					continue;
				}

				var atts = t.GetCustomAttributes<ChatCommandAttribute>();
				if (atts != null && atts.Count() != 0)
				{
					result.Add(t);
				}
			}

			return result;
		}
	}
}
