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
        public ItemDropTable lootBagItemDropTable;
        public int maxRandomLootItems = 5;
        public float lootBagDestroyDelay = 30;

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
                    if (lootBagItemDropTable != null &&
                        lootBagItemDropTable.randomItems != null &&
                        lootBagItemDropTable.randomItems.Length > 0)
                    {
                        for (i = 0; i < lootBagItemDropTable.randomItems.Length; ++i)
                        {
                            if (lootBagItemDropTable.randomItems[i].item == null ||
                                lootBagItemDropTable.randomItems[i].maxAmount <= 0 ||
                                lootBagItemDropTable.randomItems[i].dropRate <= 0)
                                continue;
                            cacheRandomLootBagItems.Add(lootBagItemDropTable.randomItems[i]);
                        }
                    }
                }
                return cacheRandomLootBagItems;
            }
        }
    }
}