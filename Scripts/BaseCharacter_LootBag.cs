using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public enum LootBagEntitySelection
    {
        Invisible,
        Visible,
        Override,
    };

    public abstract partial class BaseCharacter : BaseGameData
    {
        [Category("Loot Bag Settings")]
        [Tooltip("If selected, character will drop a loot bag on death, whether it has items or not.")]
        public bool dropEmptyBag = true;
        [Tooltip("Which loot bag entity should be used? Invisible simulated loot from body. Visible drops an actual bag. Override to use your own.")]
        public LootBagEntitySelection lootBagEntity = LootBagEntitySelection.Invisible;
        [Tooltip("This loot bag entity will be used instead of the provided ones if selected.")]
        public LootBagEntity lootBagEntityOverride;
        [Tooltip("Random items that will be generated for loot bag on death.")]
        public ItemDrop[] randomLootBagItems;
        [Tooltip("Item tables containing multiple random items that can ben generated for loot bag on death.")]
        public LootBagItemTable[] lootBagItemTables;
        [Tooltip("Maximum loot items to drop. If set to 0, maximum is ignored.")]
        public int maxLootItems = 5;
        [Tooltip("How long in seconds after dropping should loot bag be destroyed?")]
        public float lootBagDestroyDelay = 30;

        [System.NonSerialized]
        private List<ItemDrop> certainLootItems = new List<ItemDrop>();
        [System.NonSerialized]
        private List<ItemDrop> uncertainLootItems = new List<ItemDrop>();

        [System.NonSerialized]
        protected List<ItemDrop> cacheRandomLootItems = null;
        public List<ItemDrop> CacheRandomLootItems
        {
            get
            {
                if (cacheRandomLootItems == null)
                {
                    cacheRandomLootItems = new List<ItemDrop>();
                    if (randomLootBagItems != null &&
                        randomLootBagItems.Length > 0)
                    {
                        for (int i = 0; i < randomLootBagItems.Length; ++i)
                        {
                            if (randomLootBagItems[i].item == null ||
                                randomLootBagItems[i].maxAmount <= 0 ||
                                randomLootBagItems[i].dropRate <= 0)
                                continue;
                            cacheRandomLootItems.Add(randomLootBagItems[i]);
                        }
                    }
                    if (lootBagItemTables != null &&
                        lootBagItemTables.Length > 0)
                    {
                        foreach (LootBagItemTable itemDropTable in lootBagItemTables)
                        {
                            if (itemDropTable != null &&
                                itemDropTable.randomItems != null &&
                                itemDropTable.randomItems.Length > 0)
                            {
                                for (int i = 0; i < itemDropTable.randomItems.Length; ++i)
                                {
                                    if (itemDropTable.randomItems[i].item == null ||
                                        itemDropTable.randomItems[i].maxAmount <= 0 ||
                                        itemDropTable.randomItems[i].dropRate <= 0)
                                        continue;
                                    cacheRandomLootItems.Add(itemDropTable.randomItems[i]);
                                }
                            }
                        }
                    }
                    cacheRandomLootItems.Sort((a, b) => b.dropRate.CompareTo(a.dropRate));
                    certainLootItems.Clear();
                    uncertainLootItems.Clear();
                    for (int i = 0; i < cacheRandomLootItems.Count; ++i)
                    {
                        if (cacheRandomLootItems[i].dropRate >= 1f)
                            certainLootItems.Add(cacheRandomLootItems[i]);
                        else
                            uncertainLootItems.Add(cacheRandomLootItems[i]);
                    }
                }
                return cacheRandomLootItems;
            }
        }

        /// <summary>
        /// Returns a random collection of item drops.
        /// </summary>
        /// <returns>list of random item drops</returns>
        public List<CharacterItem> GetRandomItems()
        {
            List<CharacterItem> items = new List<CharacterItem>();

            if (CacheRandomLootItems.Count == 0)
                return items;

            int randomDropCount = 0;

            // Add certain loot rate items
            certainLootItems.Shuffle();
            for (int i = 0; i < certainLootItems.Count && (maxLootItems == 0 || randomDropCount < maxLootItems); ++i)
            {
                short amount = (short)Random.Range(certainLootItems[i].minAmount <= 0 ? 1 : certainLootItems[i].minAmount, certainLootItems[i].maxAmount);
                items.Add(CharacterItem.Create(certainLootItems[i].item, 1, amount));
                randomDropCount++;
            }

            // Reached max loot items?
            if (randomDropCount >= maxLootItems)
                return items;
            
            // Add uncertain loot rate items
            uncertainLootItems.Shuffle();
            for (int i = 0; i < uncertainLootItems.Count && (maxLootItems == 0 || randomDropCount < maxLootItems); ++i)
            {
                if (Random.value >= uncertainLootItems[i].dropRate)
                    continue;

                short amount = (short)Random.Range(uncertainLootItems[i].minAmount <= 0 ? 1 : uncertainLootItems[i].minAmount, uncertainLootItems[i].maxAmount);
                items.Add(CharacterItem.Create(uncertainLootItems[i].item, 1, amount));
                randomDropCount++;
            }

            return items;
        }

        /// <summary>
        /// Resets data caches. Should be called on startup to refresh changes in game data.
        /// </summary>
        public virtual void ResetCaches()
        {
            cacheRandomLootItems = null;
        }
    }
}