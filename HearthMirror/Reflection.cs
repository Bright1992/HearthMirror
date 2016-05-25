using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
				yield return GetDeck(val);
			}
		}

		public static int GetGameType() => TryGetInternal(InternalGetGameType);
		private static int InternalGetGameType() => (int) Mirror.Root["GameMgr"]["s_instance"]["m_gameType"];

		public static bool IsSpectating() => TryGetInternal(() => (bool)Mirror.Root["GameMgr"]["s_instance"]["m_spectator"]);

		public static MatchInfo GetMatchInfo => TryGetInternal(GetMatchInfoInternal);
		private static MatchInfo GetMatchInfoInternal()
		{
			var matchInfo = new MatchInfo();
			var gameState = Mirror.Root["GameState"]["s_instance"];
			var players = gameState["m_playerMap"]["valueSlots"];
			foreach(var player in players)
			{
				if(player?.Class.Name != "Player")
					continue;
				var medalInfo = player["m_medalInfo"];
				var sMedalInfo = medalInfo?["m_currMedalInfo"];
				var wMedalInfo = medalInfo?["m_currWildMedalInfo"];
				var name = player["m_name"];
				var sRank = sMedalInfo?["rank"];
				var sLegendRank = sMedalInfo?["legendIndex"];
				var wRank = wMedalInfo?["rank"];
				var wLegendRank = wMedalInfo?["legendIndex"];
				if((bool)player["m_local"])
				{
					dynamic netCacheMedalInfo = null;
					foreach(var fo in Mirror.Root["NetCache"]["s_instance"]["m_netCache"]["valueSlots"])
					{
						if(fo?.Class.Name != "NetCacheMedalInfo")
							continue;
						netCacheMedalInfo = fo;
						break;
					}
					var sStars = netCacheMedalInfo?["<Standard>k__BackingField"]["<Stars>k__BackingField"];
					var wStars = netCacheMedalInfo?["<Wild>k__BackingField"]["<Stars>k__BackingField"];
					matchInfo.LocalPlayer = new MatchInfo.Player(name, sRank, sLegendRank, sStars, wRank, wLegendRank, wStars);
				}
				else
					matchInfo.OpposingPlayer = new MatchInfo.Player(name, sRank, sLegendRank, 0, wRank, wLegendRank, 0);
			}
			return matchInfo;
		}

		public static ArenaInfo GetArenaDeck() => TryGetInternal(GetArenaDeckInternal);

		private static ArenaInfo GetArenaDeckInternal()
		{
			var draftManager = Mirror.Root["DraftManager"]["s_instance"];
			return new ArenaInfo {
				Wins = draftManager["m_wins"],
				Losses = draftManager["m_losses"],
				Deck = GetDeck(draftManager["m_draftDeck"])
			};
		}

		private static Deck GetDeck(dynamic deckObj)
		{
			var deck = new Deck
			{
				Id = deckObj["ID"],
				Name = deckObj["m_name"],
				Hero = deckObj["HeroCardID"],
				IsWild = deckObj["m_isWild"],
				Type = deckObj["Type"],
				SeasonId = deckObj["SeasonId"],
				CardBackId = deckObj["CardBackID"],
				HeroPremium = deckObj["HeroPremium"],
			};
			var cardList = deckObj["m_slots"];
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
			return deck;
		}
	}
}