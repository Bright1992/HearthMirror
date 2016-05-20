namespace HearthMirror.Objects
{
	public class MatchInfo
	{
		public Player LocalPlayer { get; set; }

		public Player OpposingPlayer { get; set; }

		public class Player
		{
			public Player(string name, int standardRank, int standardLegendRank, int standardStars, int wildRank, int wildLegendRank, int wildStars)
			{
				Name = name;
				StandardRank = standardRank;
				StandardLegendRank = standardLegendRank;
				StandardStars = standardStars;
				WildRank = wildRank;
				WildLegendRank = wildLegendRank;
				WildStars = wildStars;
			}

			public string Name { get; set; }
			public int StandardRank { get; set; }
			public int StandardLegendRank { get; set; }
			public int StandardStars { get; set; }
			public int WildRank { get; set; }
			public int WildLegendRank { get; set; }
			public int WildStars { get; set; }
		}
	}
}