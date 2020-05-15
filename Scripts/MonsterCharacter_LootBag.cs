using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class MonsterCharacter : BaseCharacter
    {
        /// <summary>
        /// Returns a random collection of item drops.
        /// </summary>
        /// <returns>list of random item drops</returns>
        public List<ItemDrop> GetRandomItems()
        {
            List<ItemDrop> itemDrops = new List<ItemDrop>();
            for (int countDrops = 0; countDrops < CacheRandomItems.Count && countDrops < maxDropItems; ++countDrops)
            {
                ItemDrop randomItem = CacheRandomItems[Random.Range(0, CacheRandomItems.Count)];
                if (randomItem.item == null || randomItem.amount == 0 || Random.value > randomItem.dropRate)
                    continue;

                itemDrops.Add(randomItem);
            }

            return itemDrops;
        }
    }
}