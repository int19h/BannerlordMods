﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Separatism
{
	public static class KingdomExtensions
	{
		public static Kingdom[] CloseKingdoms(this Clan clan)
		{
			var kingdomDistance = Kingdom.All
				.Select(k => (k, k.FactionMidPoint.Distance(clan.FactionMidPoint)))
				.ToArray();
			var average = kingdomDistance.Average(x => x.Item2);
			return kingdomDistance
				.Where(x => x.Item2 <= average)
				.OrderBy(x => x.Item2)
				.Select(x => x.k)
				.ToArray();
		}

		public static bool IsInsideKingdomTeritory(this Kingdom kingdom1, Kingdom kingdom2)
		{
			var positions1 = kingdom1.Settlements.Where(x => x.IsTown || x.IsCastle)
				.Select(x => x.Position2D).ToArray();
			var positions2 = kingdom2.Settlements.Where(x => x.IsTown || x.IsCastle)
				.Select(x => x.Position2D).ToArray();

			return positions1.Max(p => p.X) <= positions2.Max(p => p.X) &&
				positions1.Max(p => p.Y) <= positions2.Max(p => p.Y) &&
				positions1.Min(p => p.X) >= positions2.Min(p => p.X) &&
				positions1.Min(p => p.Y) >= positions2.Min(p => p.Y);
		}

		public static void SetKingdomText(this Kingdom kingdom)
		{
			if (kingdom.EncyclopediaText == null)
			{
				AccessTools.Property(typeof(Kingdom), "EncyclopediaText").SetValue(kingdom, TextObject.Empty);
			}
			if (kingdom.EncyclopediaTitle == null || kingdom.EncyclopediaRulerTitle == null)
			{
				var kingdomNameText = TextObject.Empty;
				var kingdomRulerTitleText = TextObject.Empty;
				if (kingdom.RulingClan != null)
				{
					kingdom.RulingClan.GetKingdomNameAndRulerTitle(out kingdomNameText, out kingdomRulerTitleText);
				}
				AccessTools.Property(typeof(Kingdom), "EncyclopediaTitle").SetValue(kingdom, kingdomNameText);
				AccessTools.Property(typeof(Kingdom), "EncyclopediaRulerTitle").SetValue(kingdom, kingdomRulerTitleText);
			}
		}

		public static void AddKingdom(this Campaign campaign, Kingdom kingdom)
		{
			ModifyKingdomList(campaign, kingdoms =>
			{
				if (!kingdoms.Contains(kingdom))
				{
					kingdoms.Add(kingdom);
					return kingdoms;
				}

				return null;
			});
		}

		public static void RemoveEmptyKingdoms(this Campaign campaign, Action<Kingdom> callBack = null)
		{
			ModifyKingdomList(campaign, kingdoms =>
			{
				var emptyKingdomsToRemove = kingdoms
					.Where(k =>
						k.Clans.Where(x => x.Leader.IsAlive).Count() == 0 &&
						(!SeparatismConfig.Settings.KeepEmptyKingdoms || k.RulingClan?.GetKingdomId() == k.StringId))
					.ToArray();
				if (emptyKingdomsToRemove.Length > 0)
				{
					foreach (var kingdom in emptyKingdomsToRemove)
					{
						callBack?.Invoke(kingdom);
						DestroyKingdomAction.Apply(kingdom);
					}
					return kingdoms.Except(emptyKingdomsToRemove).ToList();
				}

				return null;
			});
		}

		private static void ModifyKingdomList(this Campaign campaign, Func<List<Kingdom>, List<Kingdom>> modificator)
		{
			List<Kingdom> kingdoms = campaign.Kingdoms.ToList();
			kingdoms = modificator(kingdoms);
			if (kingdoms != null)
			{
				AccessTools.Field(Campaign.Current.GetType(), "_kingdoms").SetValue(Campaign.Current, new MBReadOnlyList<Kingdom>(kingdoms));
			}
		}
	}
}
