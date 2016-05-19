namespace HearthMirror.Objects
{
	public class Card
	{
		public string Id { get; }
		public int Count { get; set; }
		public bool Premium { get; }

		public Card(string id, int count, bool premium)
		{
			Id = id;
			Count = count;
			Premium = premium;
		}
	}
}