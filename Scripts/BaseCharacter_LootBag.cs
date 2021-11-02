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
        public bool useLootBag = true;
        public bool dropEmptyBag = true;
        public LootBagEntitySelection lootBagEntity = LootBagEntitySelection.Invisible;
        public LootBagEntity lootBagEntityOverride;
        public ItemDrop[] randomLootBagItems;
        public ItemDropTable[] lootBagItemDropTables;
        public int maxLootItems = 5;
        public float lootBagDestroyDelay = 30;

        [System.NonSerialized]
        private List<ItemDrop> certainLootItems = new List<ItemDrop>();
        [System.NonSerialized]
        private List<ItemDrop> uncertainLootItems = new List<ItemDrop>();

        [System.NonSerialized]
        private List<ItemDrop> cacheRandomLootItems = null;
        public List<ItemDrop> CacheRandomLootItems
        {
            get
            {
                if (cacheRandomLootItems == null)
                {
                    int i;
                    cacheRandomLootItems = new List<ItemDrop>();
                    if (randomLootBagItems != null &&
                        randomLootBagItems.Length > 0)
                    {
                        for (i = 0; i < randomLootBagItems.Length; ++i)
                        {
                            if (randomLootBagItems[i].item == null ||
                                randomLootBagItems[i].maxAmount <= 0 ||
                                randomLootBagItems[i].dropRate <= 0)
                                continue;
                            cacheRandomLootItems.Add(randomLootBagItems[i]);
                        }
                    }
                    if (lootBagItemDropTables != null &&
                        lootBagItemDropTables.Length > 0)
                    {
                        foreach (ItemDropTable itemDropTable in lootBagItemDropTables)
                        {
                            if (itemDropTable != null &&
                                itemDropTable.randomItems != null &&
                                itemDropTable.randomItems.Length > 0)
                            {
                                for (i = 0; i < itemDropTable.randomItems.Length; ++i)
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
                    for (i = 0; i < cacheRandomLootItems.Count; ++i)
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
            int i;

            // Add certain loot rate items
            certainLootItems.Shuffle();
            for (i = 0; i < certainLootItems.Count && randomDropCount < maxLootItems; ++i)
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
            for (i = 0; i < uncertainLootItems.Count && randomDropCount < maxLootItems; ++i)
            {
                if (Random.value >= uncertainLootItems[i].dropRate)
                    continue;

                short amount = (short)Random.Range(uncertainLootItems[i].minAmount <= 0 ? 1 : uncertainLootItems[i].minAmount, uncertainLootItems[i].maxAmount);
                items.Add(CharacterItem.Create(uncertainLootItems[i].item, 1, amount));
                randomDropCount++;
            }

            return items;
        }
    }
}