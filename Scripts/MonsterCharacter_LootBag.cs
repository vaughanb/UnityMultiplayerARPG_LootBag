using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class MonsterCharacter : BaseCharacter
    {
        [Header("Loot Bag Rewards")]
        [SerializeField]
        private ItemDrop[] randomLootBagItems;
        [SerializeField]
        private ItemDropTable lootBagitemDropTable;

        [System.NonSerialized]
        private List<ItemDrop> cacheRandomLootBagItems;

        /// <summary>
        /// Returns a random collection of item drops.
        /// </summary>
        /// <returns>list of random item drops</returns>
        public List<ItemDrop> GetRandomItems()
        {
            List<ItemDrop> itemDrops = new List<ItemDrop>();
            for (int countDrops = 0; countDrops < CacheLootBagRandomItems.Count && countDrops < maxDropItems; ++countDrops)
            {
                ItemDrop randomItem = CacheLootBagRandomItems[Random.Range(0, CacheLootBagRandomItems.Count)];
                if (randomItem.item == null || randomItem.amount == 0 || Random.value > randomItem.dropRate)
                    continue;

                itemDrops.Add(randomItem);
            }

            return itemDrops;
        }

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
                                randomLootBagItems[i].amount <= 0 ||
                                randomLootBagItems[i].dropRate <= 0)
                                continue;
                            cacheRandomLootBagItems.Add(randomLootBagItems[i]);
                        }
                    }
                    if (lootBagitemDropTable != null &&
                        lootBagitemDropTable.randomItems != null &&
                        lootBagitemDropTable.randomItems.Length > 0)
                    {
                        for (i = 0; i < itemDropTable.randomItems.Length; ++i)
                        {
                            if (lootBagitemDropTable.randomItems[i].item == null ||
                                lootBagitemDropTable.randomItems[i].amount <= 0 ||
                                lootBagitemDropTable.randomItems[i].dropRate <= 0)
                                continue;
                            cacheRandomLootBagItems.Add(lootBagitemDropTable.randomItems[i]);
                        }
                    }
                }
                return cacheRandomLootBagItems;
            }
        }
    }
}