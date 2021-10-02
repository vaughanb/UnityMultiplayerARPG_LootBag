using UnityEngine;

namespace MultiplayerARPG
{
    public partial class UICharacterItem
    {
        protected int owningCharacterId;

        /// <summary>
        /// Sets the character data ID of the entity that owns the item.
        /// </summary>
        public void SetOwningCharacter(int dataId) 
        {
            owningCharacterId = dataId;
        }

        /// <summary>
        /// Determines if the item is equipped. Also returns useful information about the item.
        /// </summary>
        /// <param name="pc">player character entity</param>
        /// <param name="dataID">ID of item to use</param>
        /// <param name="inventoryType">type of inventory item</param>
        /// <param name="itemIndex">index of item</param>
        /// <param name="equipWeaponSet">equip weapon set</param>
        /// <returns>true if equipped, false otherwise</returns>
        public bool IsEquipped(BasePlayerCharacterEntity pc, int dataID, out InventoryType inventoryType, out int itemIndex, out byte equipWeaponSet)
        {
            inventoryType = InventoryType.NonEquipItems;
            itemIndex = -1;
            equipWeaponSet = 0;

            itemIndex = pc.IndexOfEquipItem(dataID);
            if (itemIndex >= 0)
            {
                inventoryType = InventoryType.EquipItems;
                return true;
            }

            string id = CharacterItem.id;

            EquipWeapons tempEquipWeapons;
            for (byte i = 0; i < pc.SelectableWeaponSets.Count; ++i)
            {
                tempEquipWeapons = pc.SelectableWeaponSets[i];
                if (!string.IsNullOrEmpty(tempEquipWeapons.rightHand.id) &&
                    tempEquipWeapons.rightHand.id.Equals(id))
                {
                    equipWeaponSet = i;
                    inventoryType = InventoryType.EquipWeaponRight;
                    return true;
                }

                if (!string.IsNullOrEmpty(tempEquipWeapons.leftHand.id) &&
                    tempEquipWeapons.leftHand.id.Equals(id))
                {
                    equipWeaponSet = i;
                    inventoryType = InventoryType.EquipWeaponLeft;
                    return true;
                }
            }

            itemIndex = pc.IndexOfNonEquipItem(id);
            if (itemIndex >= 0)
            {
                inventoryType = InventoryType.NonEquipItems;
                return false;
            }

            return false;
        }

        /// <summary>
        /// Calls RequestPickupLootBagItem to remove the item from the lootbag and move it to 
        /// the player's inventory.
        /// </summary>
        public void OnClickLootItem(short toIndex = -1)
        {
            GameInstance.LootBagHandlers.RequestPickupLootBagItem(new RequestPickupLootBagItemMessage()
            {
                dataId = owningCharacterId,
                fromIndex = (short)IndexOfData,
                toIndex = toIndex,
            }, LootBagActions.ResponsePickupLootBagItem);
        }
    }
}