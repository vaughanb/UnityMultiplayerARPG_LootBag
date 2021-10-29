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
        public int maxRandomLootItems = 5;
        public float lootBagDestroyDelay = 30;

        [System.NonSerialized]
        private List<ItemDrop> certainDropItems = new List<ItemDrop>();
        [System.NonSerialized]
        private List<ItemDrop> uncertainDropItems = new List<ItemDrop>();
        [System.NonSerialized]
        protected List<ItemDrop> cacheRandomLootBagItems;

        /// <summary>
        /// Returns a random collection of item drops.
        /// </summary>
        /// <returns>list of random item drops</returns>
        public List<ItemDrop> GetRandomItems()
        {
            List<ItemDrop> itemDrops = new List<ItemDrop>();
            for (int countDrops = 0; countDrops < CacheLootBagRandomItems.Count && countDrops < maxRandomLootItems; ++countDrops)
            {
                ItemDrop randomItem = CacheLootBagRandomItems[Random.Range(0, CacheLootBagRandomItems.Count)];
                if (randomItem.item == null || randomItem.maxAmount == 0 || Random.value > randomItem.dropRate)
                    continue;

                itemDrops.Add(randomItem);
            }

            return itemDrops;
        }

        /// <summary>
        /// Returns a list of random item drops.
        /// </summary>
        public List<ItemDrop> CacheLootBagRandomItems
        {
            get
            {
                if (cacheRandomLootBagItems == null)
                {
                    int i;
                    cacheRandomLootBagItems = new List<ItemDrop>();
                    if (randomLootBagItems != null &&
                        randomLootBagItems.Length > 0)
                    {
                        for (i = 0; i < randomLootBagItems.Length; ++i)
                        {
                            if (randomLootBagItems[i].item == null ||
                                randomLootBagItems[i].maxAmount <= 0 ||
                                randomLootBagItems[i].dropRate <= 0)
                                continue;
                            cacheRandomLootBagItems.Add(randomLootBagItems[i]);
                        }
                    }

                    if (lootBagItemDropTables != null && lootBagItemDropTables.Length > 0)
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
                                    cacheRandomLootBagItems.Add(itemDropTable.randomItems[i]);
                                }
                            }
                        }
                    }
                    cacheRandomLootBagItems.Sort((a, b) => b.dropRate.CompareTo(a.dropRate));
                    certainDropItems.Clear();
                    uncertainDropItems.Clear();
                    for (i = 0; i < cacheRandomLootBagItems.Count; ++i)
                    {
                        if (cacheRandomLootBagItems[i].dropRate >= 1f)
                            certainDropItems.Add(cacheRandomLootBagItems[i]);
                        else
                            uncertainDropItems.Add(cacheRandomLootBagItems[i]);
                    }
                }
                return cacheRandomLootBagItems;
            }
        }
    }
}