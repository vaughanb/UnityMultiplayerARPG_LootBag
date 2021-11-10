using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
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
        [Tooltip("Items added to these tables will be the only items dropped by the player character on death.")]
        public LootBagItemFilterTable[] lootBagItemFilterTables;

        [Header("PVP Mode Loot Bag Settings")]
        [Tooltip("Setting this flag to true will allow dropping player loot in non-PVP areas.")]
        public bool dropLootInNonPVPAreas = true;
        [Tooltip("Setting this flag to true will allow dropping player loot in PVP areas.")]
        public bool dropLootInPVPAreas = true;
        [Tooltip("Setting this flag to true will allow dropping player loot in faction PVP areas.")]
        public bool dropLootInFactionPVPAreas = true;
        [Tooltip("Setting this flag to true will allow dropping player loot in guild PVP areas.")]
        public bool dropLootInGuildPVPAreas = true;

        [System.NonSerialized]
        private List<LootBagFilterItem> certainFilterLootItems = new List<LootBagFilterItem>();
        [System.NonSerialized]
        public List<LootBagFilterItem> uncertainFilterLootItems = new List<LootBagFilterItem>();

        public List<LootBagFilterItem> CertainFilterLootItems
        {
            get { return certainFilterLootItems; }
            private set { certainFilterLootItems = value; }
        }

        public List<LootBagFilterItem> UncertainFilterLootItems
        {
            get { return uncertainFilterLootItems; }
            private set { uncertainFilterLootItems = value; }
        }

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

                    CertainFilterLootItems.Clear();
                    UncertainFilterLootItems.Clear();
                    for (int i = 0; i < cacheFilterLootItems.Count; i++)
                    {
                        if (cacheFilterLootItems[i].dropRate >= 1f)
                            CertainFilterLootItems.Add(cacheFilterLootItems[i]);
                        else
                            UncertainFilterLootItems.Add(cacheFilterLootItems[i]);
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