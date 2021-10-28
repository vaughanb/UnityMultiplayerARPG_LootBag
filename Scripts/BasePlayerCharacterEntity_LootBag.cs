using System.Collections.Generic;

namespace MultiplayerARPG
{
    public abstract partial class BasePlayerCharacterEntity : BaseCharacterEntity, IPlayerCharacterData
    {
        private List<CharacterItem> lootBagItems;

        /// <summary>
        /// Unequips all equipped items and returns all items in inventory.
        /// </summary>
        /// <returns>loot bag items</returns>
        protected override List<CharacterItem> GenerateLootItems()
        {
            if (characterDB == null)
            {
                PlayerCharacter playerCharacter;
                GameInstance.PlayerCharacters.TryGetValue(dataId, out playerCharacter);
                characterDB = playerCharacter;
            }

            if ((characterDB as PlayerCharacter).dropAllPlayerItems)
                return GetAllPlayerItems();
            else
                return base.GenerateLootItems();
        }

        /// <summary>
        /// Unequips and removes all items from inventory and returns them as loot bag items.
        /// </summary>
        /// <returns>items to be added to player loot bag</returns>
        protected List<CharacterItem> GetAllPlayerItems()
        {
            lootBagItems = new List<CharacterItem>();

            if (!characterDB.useLootBag)
                return lootBagItems;

            // Remove everything from inventory to make room for equipped items.
            RemoveAllInventoryItems();

            UnEquipAll();

            // Call RemoveAllinventoryItems again to remove armor/weapons that were just unequipped.
            RemoveAllInventoryItems();

            return lootBagItems;
        }

        /// <summary>
        /// Removes all items from the inventory and adds them to the lootbag.
        /// </summary>
        protected void RemoveAllInventoryItems() 
        {
            for (int i = NonEquipItems.Count - 1; i >= 0; i--)
            {
                if (NonEquipItems[i].IsEmptySlot())
                    continue;

                CharacterItem item = NonEquipItems[i];
                CharacterItem itemData = item.Clone();
                itemData.level = item.level;
                itemData.amount = item.amount;

                if (NonEquipItems.DecreaseItemsByIndex(this.IndexOfNonEquipItem(item.id), item.amount, GameInstance.Singleton.IsLimitInventorySlot))
                    lootBagItems.Add(itemData);
            }

            NonEquipItems.FillEmptySlots(CurrentGameInstance.IsLimitInventorySlot, CurrentGameInstance.baseSlotLimit);
        }

        /// <summary>
        /// Unequips all equipped armor and weapon items.
        /// </summary>
        protected void UnEquipAll()
        {
            // Make sure all slots are empty
            for (int i = 0; i < NonEquipItems.Count; i++)
            {
                NonEquipItems[i] = CharacterItem.CreateEmptySlot();
            }

            // Unequp armor items
            for (int i = EquipItems.Count - 1; i >= 0; i--)
            {
                CharacterItem unEquipItem = EquipItems[i];
                EquipItems.RemoveAt(i);

                if (unEquipItem.NotEmptySlot())
                {
                    this.AddOrSetNonEquipItems(unEquipItem);
                    this.FillEmptySlots(true);
                }
            }

             // Unequip selectable weapons
            for (int i = SelectableWeaponSets.Count -1; i >= 0; i--)
            {
                EquipWeapons set = SelectableWeaponSets[i];

                var leftHandWeapon = set.leftHand;
                var rightHandWeapon = set.rightHand;

                if (!leftHandWeapon.IsEmptySlot())
                {
                    CharacterItem unEquipItem = leftHandWeapon;
                    set.leftHand = CharacterItem.Empty;
                    this.AddOrSetNonEquipItems(unEquipItem);            
                }

                if (!rightHandWeapon.IsEmptySlot())
                {
                    CharacterItem unEquipItem = rightHandWeapon;
                    set.rightHand = CharacterItem.Empty;
                    this.AddOrSetNonEquipItems(unEquipItem);
                }   
            }

            this.FillEmptySlots();
        }
    }
}
