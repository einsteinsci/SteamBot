using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamBot
{
	public static class SimpleWeapons
	{
		public static bool IsSimpleWeapon(int defIndex)
		{
			return _weapons.Exists((w) => w.DefIndex == defIndex);
		}

		public static Weapon Get(int defIndex)
		{
			return _weapons.First((w) => w.DefIndex == defIndex);
		}

		public static bool AreWeaponsSameClass(int def1, int def2)
		{
			Weapon one = Get(def1);
			Weapon two = Get(def2);

			foreach (string c in one.ValidClasses)
			{
				if (two.ValidClasses.Contains(c))
				{
					return true;
				}
			}

			return false;
		}

		public static bool HasClass(int defindex, string c)
		{
			return Get(defindex).ValidClasses.Contains(c.ToUpper());
		}

		private static List<Weapon> _weapons = new List<Weapon>()
		{
			#region WeaponsList
			new Weapon(35, "The Kritzkrieg", new string[] { "MEDIC" }),
			new Weapon(36, "The Blutsauger", new string[] { "MEDIC" }),
			new Weapon(37, "The Ubersaw", new string[] { "MEDIC" }),
			new Weapon(38, "The Axtinguisher", new string[] { "PYRO" }),
			new Weapon(39, "The Flare Gun", new string[] { "PYRO" }),
			new Weapon(40, "The Backburner", new string[] { "PYRO" }),
			new Weapon(41, "Natascha", new string[] { "HEAVY" }),
			new Weapon(42, "The Sandvich", new string[] { "HEAVY" }),
			new Weapon(43, "The Killing Gloves of Boxing", new string[] { "HEAVY" }),
			new Weapon(44, "The Sandman", new string[] { "SCOUT" }),
			new Weapon(45, "The Force-A-Nature", new string[] { "SCOUT" }),
			new Weapon(46, "Bonk! Atomic Punch", new string[] { "SCOUT" }),
			new Weapon(56, "The Huntsman", new string[] { "SNIPER" }),
			new Weapon(57, "The Razorback", new string[] { "SNIPER" }),
			new Weapon(58, "Jarate", new string[] { "SNIPER" }),
			new Weapon(59, "The Dead Ringer", new string[] { "SPY" }),
			new Weapon(60, "The Cloak and Dagger", new string[] { "SPY" }),
			new Weapon(61, "The Ambassador", new string[] { "SPY" }),
			new Weapon(127, "The Direct Hit", new string[] { "SOLDIER" }),
			new Weapon(128, "The Equalizer", new string[] { "SOLDIER" }),
			new Weapon(129, "The Buff Banner", new string[] { "SOLDIER" }),
			new Weapon(130, "The Scottish Resistance", new string[] { "DEMOMAN" }),
			new Weapon(131, "The Chargin' Targe", new string[] { "DEMOMAN" }),
			new Weapon(132, "The Eyelander", new string[] { "DEMOMAN" }),
			new Weapon(133, "The Gunboats", new string[] { "SOLDIER" }),
			new Weapon(140, "The Wrangler", new string[] { "ENGINEER" }),
			new Weapon(141, "Frontier Justice", new string[] { "ENGINEER" }),
			new Weapon(142, "The Gunslinger", new string[] { "ENGINEER" }),
			new Weapon(153, "The Homewrecker", new string[] { "PYRO" }),
			new Weapon(154, "The Pain Train", new string[] { "SOLDIER", "DEMOMAN" }),
			new Weapon(155, "The Southern Hospitality", new string[] { "ENGINEER" }),
			new Weapon(159, "The Dalokohs Bar", new string[] { "HEAVY" }),
			new Weapon(163, "Crit-a-Cola", new string[] { "SCOUT" }),
			new Weapon(171, "The Tribalman's Shiv", new string[] { "SNIPER" }),
			new Weapon(172, "The Scotsman's Skullcutter", new string[] { "DEMOMAN" }),
			new Weapon(173, "The Vita-Saw", new string[] { "MEDIC" }),
			new Weapon(214, "The Powerjack", new string[] { "PYRO" }),
			new Weapon(215, "The Degreaser", new string[] { "PYRO" }),
			new Weapon(220, "The Shortstop", new string[] { "SCOUT" }),
			new Weapon(221, "The Holy Mackerel", new string[] { "SCOUT" }),
			new Weapon(222, "Mad Milk", new string[] { "SCOUT" }),
			new Weapon(224, "L'Etranger", new string[] { "SPY" }),
			new Weapon(225, "Your Eternal Reward", new string[] { "SPY" }),
			new Weapon(226, "The Battalion's Backup", new string[] { "SOLDIER" }),
			new Weapon(228, "The Black Box", new string[] { "SOLDIER" }),
			new Weapon(230, "The Sydney Sleeper", new string[] { "SNIPER" }),
			new Weapon(231, "Darwin's Danger Shield", new string[] { "SNIPER" }),
			new Weapon(232, "The Bushwacka", new string[] { "SNIPER" }),
			new Weapon(239, "Gloves of Running Urgently", new string[] { "HEAVY" }),
			new Weapon(304, "The Amputator", new string[] { "MEDIC" }),
			new Weapon(305, "Crusader's Crossbow", new string[] { "MEDIC" }),
			new Weapon(307, "The Ullapool Caber", new string[] { "DEMOMAN" }),
			new Weapon(308, "The Loch-n-Load", new string[] { "DEMOMAN" }),
			new Weapon(310, "The Warrior's Spirit", new string[] { "HEAVY" }),
			new Weapon(311, "The Buffalo Steak Sandvich", new string[] { "HEAVY" }),
			new Weapon(312, "The Brass Beast", new string[] { "HEAVY" }),
			new Weapon(317, "The Candy Cane", new string[] { "SCOUT" }),
			new Weapon(325, "The Boston Basher", new string[] { "SCOUT" }),
			new Weapon(326, "The Back Scratcher", new string[] { "PYRO" }),
			new Weapon(327, "The Claidheamh Mor", new string[] { "DEMOMAN" }),
			new Weapon(329, "The Jag", new string[] { "ENGINEER" }),
			new Weapon(331, "The Fists of Steel", new string[] { "HEAVY" }),
			new Weapon(348, "Sharpened Volcano Fragment", new string[] { "PYRO" }),
			new Weapon(349, "The Sun-on-a-Stick", new string[] { "SCOUT" }),
			new Weapon(351, "Detonator", new string[] { "PYRO" }),
			new Weapon(354, "The Concheror", new string[] { "SOLDIER" }),
			new Weapon(355, "The Fan O'War", new string[] { "SCOUT" }),
			new Weapon(356, "Conniver's Kunai", new string[] { "SPY" }),
			new Weapon(357, "The Half-Zatoichi", new string[] { "SOLDIER", "DEMOMAN" }),
			new Weapon(401, "The Shahanshah", new string[] { "SNIPER" }),
			new Weapon(402, "The Bazaar Bargain", new string[] { "SNIPER" }),
			new Weapon(404, "The Persian Persuader", new string[] { "DEMOMAN" }),
			new Weapon(405, "Ali Baba's Wee Booties", new string[] { "DEMOMAN" }),
			new Weapon(406, "The Splendid Screen", new string[] { "DEMOMAN" }),
			new Weapon(411, "The Quick-Fix", new string[] { "MEDIC" }),
			new Weapon(412, "The Overdose", new string[] { "MEDIC" }),
			new Weapon(413, "The Solemn Vow", new string[] { "MEDIC" }),
			new Weapon(414, "The Liberty Launcher", new string[] { "SOLDIER" }),
			new Weapon(415, "The Reserve Shooter", new string[] { "SOLDIER", "PYRO" }),
			new Weapon(416, "The Market Gardener", new string[] { "SOLDIER" }),
			new Weapon(424, "Tomislav", new string[] { "HEAVY" }),
			new Weapon(425, "The Family Business", new string[] { "HEAVY" }),
			new Weapon(426, "The Eviction Notice", new string[] { "HEAVY" }),
			new Weapon(441, "The Cow Mangler 5000", new string[] { "SOLDIER" }),
			new Weapon(442, "The Righteous Bison", new string[] { "SOLDIER" }),
			new Weapon(444, "The Mantreads", new string[] { "SOLDIER" }),
			new Weapon(447, "The Disciplinary Action", new string[] { "SOLDIER" }),
			new Weapon(448, "The Soda Popper", new string[] { "SCOUT" }),
			new Weapon(449, "The Winger", new string[] { "SCOUT" }),
			new Weapon(450, "The Atomizer", new string[] { "SCOUT" }),
			new Weapon(457, "The Postal Pummeler", new string[] { "PYRO" }),
			new Weapon(460, "The Enforcer", new string[] { "SPY" }),
			new Weapon(461, "The Big Earner", new string[] { "SPY" }),
			new Weapon(482, "Nessie's Nine Iron", new string[] { "DEMOMAN" }),
			new Weapon(513, "The Original", new string[] { "SOLDIER" }),
			new Weapon(525, "The Diamondback", new string[] { "SPY" }),
			new Weapon(526, "The Machina", new string[] { "SNIPER" }),
			new Weapon(527, "The Widowmaker", new string[] { "ENGINEER" }),
			new Weapon(528, "The Short Circuit", new string[] { "ENGINEER" }),
			new Weapon(588, "The Pomson 6000", new string[] { "ENGINEER" }),
			new Weapon(589, "The Eureka Effect", new string[] { "ENGINEER" }),
			new Weapon(593, "The Third Degree", new string[] { "PYRO" }),
			new Weapon(594, "The Phlogistinator", new string[] { "PYRO" }),
			new Weapon(595, "The Manmelter", new string[] { "PYRO" }),
			new Weapon(608, "The Bootlegger", new string[] { "DEMOMAN" }),
			new Weapon(609, "The Scottish Handshake", new string[] { "DEMOMAN" }),
			new Weapon(642, "The Cozy Camper", new string[] { "SNIPER" }),
			new Weapon(648, "The Wrap Assassin", new string[] { "SCOUT" }),
			new Weapon(649, "The Spy-cicle", new string[] { "SPY" }),
			new Weapon(656, "The Holiday Punch", new string[] { "HEAVY" }),
			new Weapon(730, "The Beggar's Bazooka", new string[] { "SOLDIER" }),
			new Weapon(739, "The Lollichop", new string[] { "PYRO" }),
			new Weapon(740, "The Scorch Shot", new string[] { "PYRO" }),
			new Weapon(741, "The Rainblower", new string[] { "PYRO" }),
			new Weapon(751, "The Cleaner's Carbine", new string[] { "SNIPER" }),
			new Weapon(752, "The Hitman's Heatmaker", new string[] { "SNIPER" }),
			new Weapon(772, "Baby Face's Blaster", new string[] { "SCOUT" }),
			new Weapon(773, "Pretty Boy's Pocket Pistol", new string[] { "SCOUT" }),
			new Weapon(775, "The Escape Plan", new string[] { "SOLDIER" }),
			new Weapon(810, "The Red-Tape Recorder", new string[] { "SPY" }),
			new Weapon(811, "The Huo-Long Heater", new string[] { "HEAVY" }),
			new Weapon(812, "The Flying Guillotine", new string[] { "SCOUT" }),
			new Weapon(813, "The Neon Annihilator", new string[] { "PYRO" }),
			new Weapon(831, "The Red-Tape Recorder", new string[] { "SPY" }),
			new Weapon(832, "The Huo-Long Heater", new string[] { "HEAVY" }),
			new Weapon(833, "The Flying Guillotine", new string[] { "SCOUT" }),
			new Weapon(834, "The Neon Annihilator", new string[] { "PYRO" }),
			new Weapon(996, "The Loose Cannon", new string[] { "DEMOMAN" }),
			new Weapon(997, "The Rescue Ranger", new string[] { "ENGINEER" }),
			new Weapon(998, "The Vaccinator", new string[] { "MEDIC" }),
			new Weapon(1092, "The Fortified Compound", new string[] { "SNIPER" }),
			new Weapon(1098, "The Classic", new string[] { "SNIPER" }),
			new Weapon(1099, "The Tide Turner", new string[] { "DEMOMAN" }),
			new Weapon(1101, "The B.A.S.E. Jumper", new string[] { "SOLDIER", "DEMOMAN" }),
			new Weapon(1103, "The Back Scatter", new string[] { "SCOUT" }),
			new Weapon(1104, "The Air Strike", new string[] { "SOLDIER" }),
			new Weapon(1151, "The Iron Bomber", new string[] { "DEMOMAN" }),
			#endregion WeaponsList
		};

		public class Weapon
		{
			public int DefIndex
			{ get; private set; }

			public string Name
			{ get; private set; }

			public List<string> ValidClasses
			{ get; private set; }

			public Weapon(int def, string name, string[] cl)
			{
				DefIndex = def;
				Name = name;
				ValidClasses = cl.ToList();
			}
		}
	}
}
