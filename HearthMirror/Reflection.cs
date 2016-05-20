using System;
using System.Collections.Generic;
using System.Linq;
using HearthMirror.Objects;

namespace HearthMirror
{
	public class Reflection
	{
		private static readonly Lazy<Mirror> LazyMirror = new Lazy<Mirror>(() => new Mirror {ImageName = "Hearthstone"});
		private static Mirror Mirror => LazyMirror.Value;

		private static T TryGetInternal<T>(Func<T> action, bool clearCache = true)
		{
			if(clearCache)
				Mirror.View?.ClearCache();
			try
			{
				return action.Invoke();
			}
			catch
			{
				Mirror.Clean();
				try
				{
					return action.Invoke();
				}
				catch
				{
					return default(T);
				}
			}
		}

		public static List<Card> GetCollection() => TryGetInternal(() => GetCollectionInternal().ToList());

		private static IEnumerable<Card> GetCollectionInternal()
		{
			var values = Mirror.Root["NetCache"]["s_instance"]["m_netCache"]["valueSlots"];
			foreach(var val in values)
			{
				if(val == null || val.Class.Name != "NetCacheCollection") continue;
				var stacks = val["<Stacks>k__BackingField"];
				var items = stacks["_items"];
				int size = stacks["_size"];
				for(var i = 0; i < size; i++)
				{
					var stack = items[i];
					int count = stack["<Count>k__BackingField"];
					var def = stack["<Def>k__BackingField"];
					string name = def["<Name>k__BackingField"];
					int premium = def["<Premium>k__BackingField"];
					yield return new Card(name, count, premium > 0);
				}
			}
		}

		public static List<Deck> GetDecks() => TryGetInternal(() => InternalGetDecks().ToList());

		private static IEnumerable<Deck> InternalGetDecks()
		{
			var values = Mirror.Root["CollectionManager"]["s_instance"]["m_decks"]["valueSlots"];
			foreach(var val in values)
			{
				if(val == null || val.Class.Name != "CollectionDeck")
					continue;
				long id = val["ID"];
				string name = val["m_name"];
				string hero = val["HeroCardID"];
				bool wild = val["m_isWild"];
				var deck = new Deck {Id = id, Name = name, Hero = hero, IsWild = wild};
				var cardList = val["m_slots"];
				var cards = cardList["_items"];
				int size = cardList["_size"];
				for(var i = 0; i < size; i++)
				{
					var card = cards[i];
					string cardId = card["m_cardId"];
					int count = card["m_count"];
					var existingCard = deck.Cards.FirstOrDefault(x => x.Id == cardId);
					if(existingCard != null)
						existingCard.Count++;
					else
						deck.Cards.Add(new Card(cardId, count, false));
				}
				yield return deck;
			}
		}
	}
}