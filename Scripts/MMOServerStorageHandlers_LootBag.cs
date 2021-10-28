using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG.MMO
{
    public partial class MMOServerStorageHandlers : MonoBehaviour, IServerStorageHandlers
    {
        /// <summary>
        /// Adds the loot bag items to the specified loot bag storage.
        /// </summary>
        /// <param name="storageId">loot bag storage ID</param>
        /// <param name="addingItems">items to add to loot bag</param>
        /// <returns>true if successful, false otherwise</returns>
        public async UniTask<bool> AddLootBagItems(StorageId storageId, List<CharacterItem> lootItems)
        {
#if UNITY_STANDALONE && !CLIENT_BUILD
            // Attempt to read from storage first. This registers it and adds it to cache.
            ReadStorageItemsReq rsir = new ReadStorageItemsReq();
            rsir.StorageType = storageId.storageType;
            rsir.StorageOwnerId = storageId.storageOwnerId;
            ReadStorageItemsResp rsiresp = await DbServiceClient.ReadStorageItemsAsync(rsir);

            foreach (CharacterItem lootItem in lootItems)
            {
                Storage storge = GetStorage(storageId, out _);
                IncreaseStorageItemsReq req = new IncreaseStorageItemsReq();
                req.StorageType = storageId.storageType;
                req.StorageOwnerId = storageId.storageOwnerId;
                req.WeightLimit = storge.weightLimit;
                req.SlotLimit = storge.slotLimit;
                req.Item = lootItem;
                IncreaseStorageItemsResp resp = await DbServiceClient.IncreaseStorageItemsAsync(req);
                if (UITextKeys.NONE != resp.Error)
                {
                    return false;
                }
                SetStorageItems(storageId, resp.StorageCharacterItems);
            }
            return true;
#else
            return false;
#endif
        }
    }
}