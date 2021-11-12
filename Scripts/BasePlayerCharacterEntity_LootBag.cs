using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public abstract partial class BasePlayerCharacterEntity : BaseCharacterEntity, IPlayerCharacterData
    {
        private PlayerCharacter playerDB = null;
        private List<CharacterItem> lootBagItems;

        /// <summary>
        /// Unequips all equipped items and returns all items in inventory.
        /// </summary>
        /// <returns>loot bag items</returns>
        protected override List<CharacterItem> GenerateLootItems()
        {
            lootBagItems = new List<CharacterItem>();

            if (playerDB == null)
            {
                PlayerCharacter playerCharacter;
                GameInstance.PlayerCharacters.TryGetValue(dataId, out playerCharacter);
                playerDB = playerCharacter;
            }

            if (playerDB == null)
                return lootBagItems;

            MapInfo mapInfo = CurrentMapInfo as MapInfo;
            if ((!playerDB.dropLootInNonPVPAreas && mapInfo.pvpMode == PvpMode.None) ||
                (!playerDB.dropLootInPVPAreas && mapInfo.pvpMode == PvpMode.Pvp) ||
                (!playerDB.dropLootInFactionPVPAreas && mapInfo.pvpMode == PvpMode.FactionPvp) ||
                (!playerDB.dropLootInGuildPVPAreas && mapInfo.pvpMode == PvpMode.GuildPvp))
                return lootBagItems;

            if (playerDB.CacheRandomLootItems.Count > 0)
                lootBagItems.AddRange(base.GenerateLootItems());

            AddPlayerItems();

            return lootBagItems;
        }

        /// <summary>
        /// Adds player items to the loot bag according to rules set.
        /// </summary>
        /// <returns>items to be added to player loot bag</returns>
        protected List<CharacterItem> AddPlayerItems()
        {
            lootBagItems = new List<CharacterItem>();

            if (playerDB.dropInventoryItems)
                DropInventoryItems();
            
            if (playerDB.dropEquippedArmorItems)
                DropArmorItems();
            
            if (playerDB.dropEquippedWeaponItems)
                DropWeaponItems();

            return lootBagItems;
        }

        /// <summary>
        /// Drops inventory items to the loot bag depending on the rules set.
        /// </summary>
        protected void DropInventoryItems()
        {
            for (int i = NonEquipItems.Count - 1; i >= 0; i--)
            {
                if (NonEquipItems[i].IsEmptySlot())
                    continue;

                if (playerDB.maxLootItems > 0 && lootBagItems.Count >= playerDB.maxLootItems)
                    return;

                if (ShouldDrop(NonEquipItems[i]))
                    DropInventoryItem(NonEquipItems[i]);
            }
        }

        /// <summary>
        /// Drops the specific item from the player's inventory and adds it to the loot bag.
        /// </summary>
        /// <param name="item">item to drop</param>
        protected void DropInventoryItem(CharacterItem item)
        {
            CharacterItem itemData = item.Clone();
            itemData.level = item.level;
            itemData.amount = item.amount;

            if (NonEquipItems.DecreaseItemsByIndex(this.IndexOfNonEquipItem(item.id), item.amount, GameInstance.Singleton.IsLimitInventorySlot))
                lootBagItems.Add(itemData);
        }

        /// <summary>
        /// Unequips armor items from the player and drops them to the loot bag according to the rules set.
        /// </summary>
        protected void DropArmorItems()
        {
            for (int i = EquipItems.Count - 1; i >= 0; i--)
            {
                if (playerDB.maxLootItems > 0 && lootBagItems.Count >= playerDB.maxLootItems)
                    return;

                CharacterItem unEquipItem = EquipItems[i];
                if (unEquipItem.IsEmptySlot())
                    continue;

                if (ShouldDrop(unEquipItem))
                {
                    EquipItems.RemoveAt(i);
                    this.AddOrSetNonEquipItems(unEquipItem);
                    this.FillEmptySlots(true);

                    DropInventoryItem(unEquipItem);
                }

                this.FillEmptySlots();
            }
        }

        /// <summary>
        /// Unequips weapon items from the character and drops them to the loot bag according to the rules set.
        /// </summary>
        protected void DropWeaponItems()
        {
            for (int i = SelectableWeaponSets.Count - 1; i >= 0; i--)
            {
                EquipWeapons set = SelectableWeaponSets[i];

                var leftHandWeapon = set.leftHand;
                var rightHandWeapon = set.rightHand;

                if (!leftHandWeapon.IsEmptySlot())
                {
                    if (playerDB.maxLootItems > 0 && lootBagItems.Count >= playerDB.maxLootItems)
                        return;

                    if (ShouldDrop(leftHandWeapon))
                    {
                        CharacterItem unEquipItem = leftHandWeapon;
                        set.leftHand = CharacterItem.Empty;
                        this.AddOrSetNonEquipItems(unEquipItem);
                        this.FillEmptySlots();

                        DropInventoryItem(unEquipItem);
                    }
                }

                if (!rightHandWeapon.IsEmptySlot())
                {
                    if (playerDB.maxLootItems > 0 && lootBagItems.Count >= playerDB.maxLootItems)
                        return;

                    if (ShouldDrop(rightHandWeapon))
                    {
                        CharacterItem unEquipItem = rightHandWeapon;
                        set.rightHand = CharacterItem.Empty;
                        this.AddOrSetNonEquipItems(unEquipItem);
                        this.FillEmptySlots();

                        DropInventoryItem(unEquipItem);
                    }
                }
            }

            this.FillEmptySlots();
        }

        /// <summary>
        /// Checks to see if the specified item should drop to the loot bag based on current rules.
        /// </summary>
        /// <param name="item">item to drop</param>
        /// <returns>true if should drop, false otherwise</returns>
        protected bool ShouldDrop(CharacterItem item)
        {
            if (playerDB.CacheFilterLootItems.Count == 0)
                return true;

            if (playerDB.lootBagItemFilterBehavior == LootBagFilterBehavior.Inclusive)
            {
                foreach (LootBagFilterItem fi in playerDB.CacheFilterLootItems)
                {
                    if (item.GetItem().DataId == fi.item.DataId)
                    {
                        if (fi.dropRate >= 1 || Random.value < fi.dropRate)
                            return true;
                    }
                }
            }
            else
            {
                bool inFilter = false;
                foreach (LootBagFilterItem fi in playerDB.CacheFilterLootItems)
                {
                    if (item.GetItem().DataId == fi.item.DataId)
                    {
                        inFilter = true;
                        break;
                    }
                }
                return !inFilter;
            }
            return false;
        }
    }
}
