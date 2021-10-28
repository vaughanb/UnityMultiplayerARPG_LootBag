using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class LanRpgServerStorageHandlers : MonoBehaviour, IServerStorageHandlers
    {
        /// <summary>
        /// Adds the loot bag items to the specified loot bag storage.
        /// </summary>
        /// <param name="storageId">loot bag storage ID</param>
        /// <param name="addingItems">items to add to loot bag</param>
        /// <returns>true if successful, false otherwise</returns>
        public async UniTask<bool> AddLootBagItems(StorageId storageId, List<CharacterItem> lootItems)
        {
            await UniTask.Yield();

            foreach (CharacterItem lootItem in lootItems)
            {
                if (lootItem.IsEmptySlot())
                    continue;

                List<CharacterItem> storageItems = GetStorageItems(storageId);
                // Prepare storage data
                Storage storage = GetStorage(storageId, out _);
                bool isLimitWeight = storage.weightLimit > 0;
                bool isLimitSlot = storage.slotLimit > 0;
                short weightLimit = storage.weightLimit;
                short slotLimit = storage.slotLimit;
                // Increase item to storage
                bool isOverwhelming = storageItems.IncreasingItemsWillOverwhelming(
                    lootItem.dataId, lootItem.amount, isLimitWeight, weightLimit,
                    storageItems.GetTotalItemWeight(), isLimitSlot, slotLimit);
                if (!isOverwhelming && storageItems.IncreaseItems(lootItem))
                {
                    // Update slots
                    storageItems.FillEmptySlots(isLimitSlot, slotLimit);
                    SetStorageItems(storageId, storageItems);
                }
            }

            return true;
        }
    }
}