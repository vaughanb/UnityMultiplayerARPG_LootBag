using Cysharp.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class LanRpgServerStorageHandlers : MonoBehaviour, IServerStorageHandlers
    {
        public async UniTask<bool> AddLootBagItems(StorageId storageId, CharacterItem addingItem)
        {
            await UniTask.Yield();
            if (addingItem.IsEmptySlot())
                return false;
            List<CharacterItem> storageItems = GetStorageItems(storageId);
            // Prepare storage data
            Storage storage = GetStorage(storageId, out _);
            bool isLimitWeight = storage.weightLimit > 0;
            bool isLimitSlot = storage.slotLimit > 0;
            short weightLimit = storage.weightLimit;
            short slotLimit = storage.slotLimit;
            // Increase item to storage
            bool isOverwhelming = storageItems.IncreasingItemsWillOverwhelming(
                addingItem.dataId, addingItem.amount, isLimitWeight, weightLimit,
                storageItems.GetTotalItemWeight(), isLimitSlot, slotLimit);
            if (!isOverwhelming && storageItems.IncreaseItems(addingItem))
            {
                // Update slots
                storageItems.FillEmptySlots(isLimitSlot, slotLimit);
                SetStorageItems(storageId, storageItems);
                return true;
            }
            return false;
        }
    }
}