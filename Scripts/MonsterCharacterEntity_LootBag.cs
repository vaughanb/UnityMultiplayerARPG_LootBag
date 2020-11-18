using LiteNetLibManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace MultiplayerARPG
{
    public partial class MonsterCharacterEntity : BaseMonsterCharacterEntity
    {
        [DevExtMethods("Awake")]
        protected void OnAwake()
        {
            onReceivedDamage += OnReceivedDamage;
        }

        protected void OnReceivedDamage(Vector3 fromPosition, IGameEntity attacker, CombatAmountType combatAmountType, int damage, CharacterItem weapon, BaseSkill skill, short skillLevel)
        {
            if (this.IsDead())
            {
                Debug.Log("Target is dead.");

                BaseCharacterEntity bce = this as BaseCharacterEntity;
                bce.deathTime = DateTime.Now;

                SpawnLootBag();
            }  
        }

        /// <summary>
        /// Spawns the loot bag items and adds them to the bag.
        /// </summary>
        protected void SpawnLootBag()
        {
            // Generate loot to drop on ground or fill loot bag
            List<ItemDrop> itemDrops = CharacterDatabase.GetRandomItems();
            List<CharacterItem> lootBagItems = new List<CharacterItem>();
            foreach (ItemDrop itemDrop in itemDrops)
            {
                if (useLootBag)
                    lootBagItems.Add(CharacterItem.Create(itemDrop.item, 1, itemDrop.amount));
            }
            LootBag = lootBagItems;

            Debug.Log("LootBag items spawned.");
        }
    }
}
