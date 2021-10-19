using Cysharp.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG.MMO
{
    public partial class MMOServerStorageHandlers : MonoBehaviour, IServerStorageHandlers
    {
        /// <summary>
        /// Adds the loot bag item to the specified storage.
        /// </summary>
        /// <param name="storageId">loot bag storage ID</param>
        /// <param name="addingItem">item to add</param>
        /// <returns>true if successful, false otherwise</returns>
        public async UniTask<bool> AddLootBagItems(StorageId storageId, CharacterItem addingItem) 
        {
#if UNITY_STANDALONE && !CLIENT_BUILD
            Storage storge = GetStorage(storageId, out _);
            IncreaseStorageItemsReq req = new IncreaseStorageItemsReq();
            req.StorageType = storageId.storageType;
            req.StorageOwnerId = storageId.storageOwnerId;
            req.WeightLimit = storge.weightLimit;
            req.SlotLimit = storge.slotLimit;
            req.Item = addingItem;
            IncreaseStorageItemsResp resp = await DbServiceClient.IncreaseStorageItemsAsync(req);
            if (UITextKeys.NONE != (UITextKeys)resp.Error)
            {
                return false;
            }
            SetStorageItems(storageId, resp.StorageCharacterItems);
            return true;
#else
            return false;
#endif
        }
    }
}