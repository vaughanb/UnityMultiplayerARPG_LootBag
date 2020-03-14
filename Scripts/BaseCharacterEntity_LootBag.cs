using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public abstract partial class BaseCharacterEntity : DamageableEntity, ICharacterData
    {
        [Header("Loot Settings")]
        public bool useLootBag = true;
        public GameObject lootSparkleEffect;

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
        public bool RequestPickupLootBagItem(uint objectId, short sourceItemIndex, short nonEquipIndex)
        {
            if (IsDead())
                return false;
            CallNetFunction(NetFuncPickupLootBagItem, FunctionReceivers.Server, objectId, sourceItemIndex, nonEquipIndex);
            return true;
        }

        /// <summary>
        /// Picks up all loot bag items from the target monster and moves them to the player's inventory.
        /// </summary>
        /// <param name="objectId">ID of the source object to loot from</param>
        /// <returns></returns>
        public bool RequestPickupAllLootBagItems(uint objectId)
        {
            if (IsDead())
                return false;
            CallNetFunction(NetFuncPickupAllLootBagItems, FunctionReceivers.Server, objectId);
            return true;
        }

        /// <summary>
        /// Moves an item from the specified monster's loot bag to the player's inventory.
        /// </summary>
        /// <param name="objectId">ID of the source object to loot from</param>
        /// <param name="lootBagIndex">index of the item to look in the loot bag</param>
        /// <param name="nonEquipIndex">index of the inventory slot to place the item</param>
        protected void NetFuncPickupLootBagItem(uint objectId, short lootBagIndex, short nonEquipIndex)
        {
            BaseMonsterCharacterEntity monsterCharacterEntity = GetTargetEntity() as BaseMonsterCharacterEntity;
            if (monsterCharacterEntity == null || monsterCharacterEntity.ObjectId != objectId)
            {
                var monsterCharacters = FindObjectsOfType(typeof(BaseMonsterCharacterEntity));
                foreach (BaseMonsterCharacterEntity monsterCharacter in monsterCharacters)
                {
                    if (monsterCharacter.ObjectId == objectId)
                    {
                        monsterCharacterEntity = monsterCharacter;
                        break;
                    }
                }
            }

            if (monsterCharacterEntity == null || monsterCharacterEntity.LootBag.Count == 0)
                return;

            if (lootBagIndex > monsterCharacterEntity.LootBag.Count - 1)
                return;

            CharacterItem lootItem = monsterCharacterEntity.LootBag[lootBagIndex].Clone();

            int destIndex = -1;
            if (nonEquipIndex < 0 || nonEquipIndex > NonEquipItems.Count - 1 || !NonEquipItems[nonEquipIndex].IsEmptySlot())
            {
                if (nonEquipIndex > 0 && nonEquipIndex < NonEquipItems.Count &&
                    NonEquipItems[nonEquipIndex].dataId == lootItem.dataId &&
                    NonEquipItems[nonEquipIndex].amount + lootItem.amount <= lootItem.GetMaxStack())
                {
                    destIndex = nonEquipIndex;
                    lootItem.amount += NonEquipItems[nonEquipIndex].amount;
                }
                else
                {
                    int firstEmptySlot = -1;
                    for (int i = 0; i < NonEquipItems.Count; i++)
                    {
                        if (firstEmptySlot < 0 && NonEquipItems[i].IsEmptySlot())
                            firstEmptySlot = i;

                        if (NonEquipItems[i].dataId == lootItem.dataId && NonEquipItems[i].amount + lootItem.amount <= lootItem.GetMaxStack())
                        {
                            destIndex = i;
                            lootItem.amount += NonEquipItems[i].amount;
                            break;
                        }
                    }

                    if (destIndex < 0)
                        destIndex = firstEmptySlot;
                }
            }
            else
                destIndex = nonEquipIndex;

            if (destIndex >= 0)
            {
                monsterCharacterEntity.RemoveLootItemAt(lootBagIndex);
                nonEquipItems[destIndex] = lootItem;
            }
        }

        /// <summary>
        /// Removes all items from the target monster's loot bag and places them in the character's inventory.
        /// <param name="objectId">ID of the source object to loot from</param>
        /// </summary>
        protected virtual void NetFuncPickupAllLootBagItems(uint objectId)
        {
            BaseMonsterCharacterEntity monsterCharacterEntity = GetTargetEntity() as BaseMonsterCharacterEntity;
            if (monsterCharacterEntity == null || monsterCharacterEntity.ObjectId != objectId)
            {
                var monsterCharacters = FindObjectsOfType(typeof(BaseMonsterCharacterEntity));
                foreach (BaseMonsterCharacterEntity monsterCharacter in monsterCharacters)
                {
                    if (monsterCharacter.ObjectId == objectId)
                    {
                        monsterCharacterEntity = monsterCharacter;
                        break;
                    }
                }
            }

            if (monsterCharacterEntity == null || monsterCharacterEntity.LootBag.Count == 0)
                return;

            List<CharacterItem> itemsToRemove = new List<CharacterItem>();

            foreach (CharacterItem lootItem in monsterCharacterEntity.LootBag)
            {
                CharacterItem lootItemClone = lootItem.Clone();

                int destIndex = -1;
                int firstEmptySlot = -1;
                for (int i = 0; i < NonEquipItems.Count; i++)
                {
                    if (firstEmptySlot < 0 && NonEquipItems[i].IsEmptySlot())
                        firstEmptySlot = i;

                    if (NonEquipItems[i].dataId == lootItem.dataId && NonEquipItems[i].amount + lootItem.amount <= lootItem.GetMaxStack())
                    {
                        destIndex = (short)i;
                        lootItemClone.amount += NonEquipItems[i].amount;
                        break;
                    }
                }

                if (destIndex < 0)
                    destIndex = firstEmptySlot;

                if (destIndex >= 0)
                {
                    nonEquipItems[destIndex] = lootItemClone;
                    itemsToRemove.Add(lootItem);
                }
                else
                    break;
            }

            monsterCharacterEntity.RemoveLootItems(itemsToRemove);
        }
    }
}
