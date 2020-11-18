using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;
using System;

namespace MultiplayerARPG
{
    public abstract partial class BaseCharacterEntity : DamageableEntity, ICharacterData
    {
        [Header("Loot Settings")]
        public bool useLootBag = true;
        public GameObject lootSparkleEffect;

        [NonSerialized]
        public DateTime deathTime;

        private Transform lootSparkleTransform;

        private SyncListCharacterItem lootBag = new SyncListCharacterItem();
        public IList<CharacterItem> LootBag
        {
            get { return lootBag; }
            set
            {
                lootBag.Clear();
                foreach (CharacterItem entry in value)
                    lootBag.Add(entry);
            }
        }

        private bool enableSparkleEffect;

        protected override void EntityLateUpdate()
        {
            base.EntityLateUpdate();

            // Toggles the loot sparkle effect on and off depending on whether or not it is empty.
            if (lootSparkleEffect != null)
            {
                if (!enableSparkleEffect && lootBag.Count > 0)
                {
                    enableSparkleEffect = true;

                    GameObject ps = Instantiate(lootSparkleEffect, transform.position, Quaternion.identity);
                    ps.transform.parent = transform;
                    lootSparkleTransform = ps.transform;
                    lootSparkleTransform.gameObject.SetActive(true);
                }
                else if (enableSparkleEffect && lootBag.Count == 0)
                {
                    enableSparkleEffect = false;

                    if (lootSparkleTransform != null)
                        Destroy(lootSparkleTransform.gameObject);
                }
            }
        }

        /// <summary>
        /// Clears the loot bag of all contents.
        /// </summary>
        public void ClearLootBag()
        {
            LootBag.Clear();
        }

        /// <summary>
        /// Removes the specified item from the loot bag. If amountToRemove is specified, only
        /// that amount is removed from the stack.
        /// </summary>
        /// <param name="index">index of item to remove</param>
        /// <param name="amountToRemove">amount of the item stack to remove</param>
        public void RemoveLootItemAt(int index, short amountToRemove = 0)
        {
            if (lootBag.Count < index + 1)
                return;

            lootBag.RemoveAt(index);
        }

        /// <summary>
        /// Removes the specified character items from the loot bag.
        /// </summary>
        /// <param name="characterItems">character items to remove from loot bag</param>
        public void RemoveLootItems(List<CharacterItem> characterItems)
        {
            if (lootBag.Count == 0)
                return;

            foreach (CharacterItem characterItem in characterItems)
                lootBag.Remove(characterItem);
        }

        /// <summary>
        /// Calls NetFuncPickupLootBagItem to pick up the item and move it from the monster loot bag to
        /// the player's inventory.
        /// </summary>
        /// <param name="objectId">ID of the source object to loot from</param>
        /// <param name="sourceItemIndex">index of the item in the loot bag</param>
        /// <param name="nonEquipIndex">index of the inventory slot to move the item to</param>
        /// <returns>true if successful</returns>
        public bool CallServerPickupLootBagItem(uint objectId, short sourceItemIndex, short nonEquipIndex)
        {
            if (this.IsDead())
                return false;
            RPC(NetFuncPickupLootBagItem, FunctionReceivers.Server, objectId, sourceItemIndex, nonEquipIndex);
            return true;
        }

        /// <summary>
        /// Picks up all loot bag items from the target monster and moves them to the player's inventory.
        /// </summary>
        /// <param name="objectId">ID of the source object to loot from</param>
        /// <returns></returns>
        public bool CallServerPickupAllLootBagItems(uint objectId)
        {
            if (this.IsDead())
                return false;
            RPC(NetFuncPickupAllLootBagItems, FunctionReceivers.Server, objectId);
            return true;
        }

        [ServerRpc]
        /// <summary>
        /// Moves an item from the specified monster's loot bag to a slot in the player's 
        /// inventory. If no specific destination slot is specified, the first available slot is
        /// chosen.
        /// </summary>
        /// <param name="objectId">ID of the source object to loot from</param>
        /// <param name="lootBagIndex">index of the item to look in the loot bag</param>
        /// <param name="nonEquipIndex">index of the inventory slot to place the item</param>
        protected virtual void NetFuncPickupLootBagItem(uint objectId, short lootBagIndex, short nonEquipIndex)
        {
            BaseCharacterEntity characterEntity = GetCharacterEntity(objectId);

            if (characterEntity == null || characterEntity.LootBag.Count == 0)
                return;

            if (lootBagIndex > characterEntity.LootBag.Count - 1)
                return;

            CharacterItem lootItem = characterEntity.LootBag[lootBagIndex].Clone();

            if (nonEquipIndex < 0)
            {
                if (lootItem.IsEmptySlot())
                    characterEntity.RemoveLootItemAt(lootBagIndex);
                if (!this.IncreasingItemsWillOverwhelming(lootItem.dataId, lootItem.amount) && this.IncreaseItems(lootItem))
                {
                    this.FillEmptySlots();
                    characterEntity.RemoveLootItemAt(lootBagIndex);
                }
            }
            else
            {
                CharacterItem toItem = NonEquipItems[nonEquipIndex];

                if (lootItem.dataId == NonEquipItems[nonEquipIndex].dataId && !lootItem.IsFull() && !toItem.IsFull())
                {
                    short maxStack = toItem.GetMaxStack();
                    if (toItem.amount + lootItem.amount <= maxStack)
                    {
                        toItem.amount += lootItem.amount;
                        characterEntity.RemoveLootItemAt(lootBagIndex);
                        NonEquipItems[nonEquipIndex] = toItem;
                        this.FillEmptySlots();
                    }
                    else
                    {
                        short remains = (short)(toItem.amount + lootItem.amount - maxStack);
                        toItem.amount = maxStack;
                        lootItem.amount = remains;
                        characterEntity.LootBag[lootBagIndex] = lootItem;
                        NonEquipItems[nonEquipIndex] = toItem;
                    }
                }
                else
                {
                    if (toItem.IsEmptySlot())
                        characterEntity.RemoveLootItemAt(lootBagIndex);
                    else
                        characterEntity.LootBag[lootBagIndex] = toItem;

                    NonEquipItems[nonEquipIndex] = lootItem;
                }
            }
        }

        [ServerRpc]
        /// <summary>
        /// Removes all items from the target monster's loot bag and places them in the character's inventory.
        /// <param name="objectId">ID of the source object to loot from</param>
        /// </summary>
        protected virtual void NetFuncPickupAllLootBagItems(uint objectId)
        {
            BaseCharacterEntity characterEntity = GetCharacterEntity(objectId);

            if (characterEntity == null || characterEntity.LootBag.Count == 0)
                return;

            Stack<int> itemsToRemove = new Stack<int>();

            for (int i = 0; i < characterEntity.LootBag.Count; i++)
            {
                CharacterItem lootItem = characterEntity.LootBag[i].Clone();

                if (lootItem.IsEmptySlot())
                {
                    itemsToRemove.Push(i);
                    continue;
                }
                if (!this.IncreasingItemsWillOverwhelming(lootItem.dataId, lootItem.amount) && this.IncreaseItems(lootItem))
                {
                    this.FillEmptySlots();
                    itemsToRemove.Push(i);
                }
            }

            foreach (int itemIndex in itemsToRemove)
                characterEntity.RemoveLootItemAt(itemIndex);
        }

        /// <summary>
        /// Returns the BaseCharacterEntity for the provided object ID.
        /// </summary>
        /// <param name="objectId">Object ID of the character entity to return</param>
        /// <returns>BaseCharacterEntity</returns>
        private BaseCharacterEntity GetCharacterEntity(uint objectId)
        {
            BaseCharacterEntity characterEntity = GetTargetEntity() as BaseCharacterEntity;
            if (characterEntity == null || characterEntity.ObjectId != objectId)
            {
                var characterEntities = FindObjectsOfType(typeof(BaseCharacterEntity));
                foreach (BaseCharacterEntity ce in characterEntities)
                {
                    if (ce.ObjectId == objectId)
                    {
                        characterEntity = ce;
                        break;
                    }
                }
            }
            return characterEntity;
        }
    }
}
