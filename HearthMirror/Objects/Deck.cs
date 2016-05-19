using System.Collections.Generic;

namespace HearthMirror.Objects
{
	public class Deck
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public string Hero { get; set; }
		public bool IsWild { get; set; }
		public List<Card> Cards { get; set; } = new List<Card>();
	}
}