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

                bool remove = false;
                if (playerDB.CacheFilterLootItems.Count > 0)
                {
                    foreach (LootBagFilterItem certainItem in playerDB.CertainFilterLootItems)
                    {
                        if (NonEquipItems[i].GetItem().DataId == certainItem.item.DataId)
                        {
                            remove = true;
                            goto Remove;
                        }
                    }
                    foreach (LootBagFilterItem uncertainItem in playerDB.UncertainFilterLootItems)
                    {
                        if (NonEquipItems[i].GetItem().DataId == uncertainItem.item.DataId && Random.value <= uncertainItem.dropRate)
                        {
                            remove = true;
                            break;
                        }
                    }
                } 
                else
                    remove = true;

                Remove:
                if (remove)
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

                bool remove = false;
                if (playerDB.CacheFilterLootItems.Count > 0)
                {
                    foreach (LootBagFilterItem filterItem in playerDB.CertainFilterLootItems)
                    {
                        if (unEquipItem.GetItem().DataId == filterItem.item.DataId)
                        {
                            remove = true;
                            goto Remove;
                        }
                    }
                    foreach (LootBagFilterItem filterItem in playerDB.UncertainFilterLootItems)
                    {
                        if (unEquipItem.GetItem().DataId == filterItem.item.DataId && Random.value <= filterItem.dropRate)
                        {
                            remove = true;
                            break;
                        }
                    }
                }
                else
                    remove = true;

                Remove:
                if (remove)
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

                    bool remove = false;
                    if (playerDB.CacheFilterLootItems.Count > 0)
                    {
                        foreach (LootBagFilterItem certainItem in playerDB.CertainFilterLootItems)
                        {
                            if (leftHandWeapon.GetItem().DataId == certainItem.item.DataId)
                            {
                                remove = true;
                                goto RemoveLeft;
                            }
                        }
                        foreach (LootBagFilterItem uncertainItem in playerDB.UncertainFilterLootItems)
                        {
                            if (leftHandWeapon.GetItem().DataId == uncertainItem.item.DataId && Random.value <= uncertainItem.dropRate)
                            {
                                remove = true;
                                break;
                            }
                        }
                    }
                    else
                        remove = true;

                    RemoveLeft:
                    if (remove)
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

                    bool remove = false;
                    if (playerDB.CacheFilterLootItems.Count > 0)
                    {
                        foreach (LootBagFilterItem certainItem in playerDB.CertainFilterLootItems)
                        {
                            if (rightHandWeapon.GetItem().DataId == certainItem.item.DataId)
                            {
                                remove = true;
                                goto RemoveRight;
                            }
                        }
                        foreach (LootBagFilterItem uncertainItem in playerDB.UncertainFilterLootItems)
                        {
                            if (rightHandWeapon.GetItem().DataId == uncertainItem.item.DataId && Random.value <= uncertainItem.dropRate)
                            {
                                remove = true;
                                break;
                            }
                        }
                    }
                    else
                        remove = true;

                    RemoveRight:
                    if (remove)
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
    }
}
