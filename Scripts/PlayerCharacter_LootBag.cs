using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public enum LootBagFilterBehavior
    {
        Inclusive,
        Exclusive,
    };

    public partial class PlayerCharacter : BaseCharacter
    {
        [Category("Loot Bag Settings")]
        [Header("Player-specific Loot Bag Settings")]
        [Tooltip("If selected, player will drop inventory items to loot bag on death.")]
        public bool dropInventoryItems = false;
        [Tooltip("If selected, player will drop armor items to loot bag on death.")]
        public bool dropEquippedArmorItems = false;
        [Tooltip("If selected, player will drop weapon items to loot bag on death.")]
        public bool dropEquippedWeaponItems = false;
        [Tooltip("If set, player will drop items according to the items in these tables and the Inclusive/Exclusive behavior specified.")]
        public LootBagItemFilterTable[] lootBagItemFilterTables;
        [Tooltip("Inclusive means only the items in the table will drop. Exlusive means all items except those in the table will drop.")]
        public LootBagFilterBehavior lootBagItemFilterBehavior = LootBagFilterBehavior.Inclusive;

        [Header("PVP Mode Loot Bag Settings")]
        [Tooltip("Setting this flag to true will allow dropping player loot in non-PVP areas.")]
        public bool dropLootInNonPVPAreas = true;
        [Tooltip("Setting this flag to true will allow dropping player loot in PVP areas.")]
        public bool dropLootInPVPAreas = true;
        [Tooltip("Setting this flag to true will allow dropping player loot in faction PVP areas.")]
        public bool dropLootInFactionPVPAreas = true;
        [Tooltip("Setting this flag to true will allow dropping player loot in guild PVP areas.")]
        public bool dropLootInGuildPVPAreas = true;

        private List<LootBagFilterItem> cacheFilterLootItems = null;
        public List<LootBagFilterItem> CacheFilterLootItems
        {
            get
            {
                if (cacheFilterLootItems == null)
                {
                    cacheFilterLootItems = new List<LootBagFilterItem>();

                    foreach (LootBagItemFilterTable filterTable in lootBagItemFilterTables)
                    {
                        if (filterTable != null && filterTable.randomItems != null && filterTable.randomItems.Length > 0)
                        {
                            foreach (LootBagFilterItem item in filterTable.randomItems)
                            {
                                if (item.dropRate <= 0)
                                    continue;

                                cacheFilterLootItems.Add(item);
                            }
                        }
                    }
                }
                return cacheFilterLootItems;
            }
        }

        public PlayerCharacter()
        {
            dropEmptyBag = false;
            maxLootItems = 0;
            lootBagEntity = LootBagEntitySelection.Visible;
            lootBagDestroyDelay = 600;
        }

        /// <summary>
        /// ResetCaches is overridden in order to clear out filter loot items cache.
        /// </summary>
        public override void ResetCaches()
        {
            base.ResetCaches();
            cacheFilterLootItems = null;
        }
    }
}