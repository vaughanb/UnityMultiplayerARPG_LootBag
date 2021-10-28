using Cysharp.Threading.Tasks;
using LiteNetLibManager;
using UnityEngine;
using System.Collections.Generic;

namespace MultiplayerARPG.MMO
{
    public partial class MMOServerStorageMessageHandlers : MonoBehaviour, IServerStorageMessageHandlers
    {
        /// <summary>
        /// Handles requests for moving all storage items to character inventory for MMO mode.
        /// </summary>
        /// <param name="requestHandler">request handler data</param>
        /// <param name="request">request</param>
        /// <param name="result">result</param>
        public async UniTaskVoid HandleRequestMoveAllItemsFromStorage(RequestHandlerData requestHandler, RequestMoveAllItemsFromStorageMessage request, RequestProceedResultDelegate<ResponseMoveAllItemsFromStorageMessage> result)
        {
#if UNITY_STANDALONE && !CLIENT_BUILD
            StorageId storageId = new StorageId(request.storageType, request.storageOwnerId);
            IPlayerCharacterData playerCharacter;
            if (!GameInstance.ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out playerCharacter))
            {
                result.Invoke(AckResponseCode.Error, new ResponseMoveAllItemsFromStorageMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            if (!GameInstance.ServerStorageHandlers.CanAccessStorage(playerCharacter, storageId))
            {
                result.Invoke(AckResponseCode.Error, new ResponseMoveAllItemsFromStorageMessage()
                {
                    message = UITextKeys.UI_ERROR_CANNOT_ACCESS_STORAGE,
                });
                return;
            }
            Storage storage = GameInstance.ServerStorageHandlers.GetStorage(storageId, out _);
            List<CharacterItem> storageItems = GameInstance.ServerStorageHandlers.GetStorageItems(storageId);

            for (int i = storageItems.Count; i > 0; i--)
            {
                MoveItemFromStorageReq req = new MoveItemFromStorageReq();
                req.StorageType = request.storageType;
                req.StorageOwnerId = request.storageOwnerId;
                req.CharacterId = playerCharacter.Id;
                req.WeightLimit = storage.weightLimit;
                req.SlotLimit = storage.slotLimit;
                req.StorageItemIndex = i - 1;
                req.StorageItemAmount = storageItems[i - 1].amount;
                req.InventoryItemIndex = -1;
                req.Inventory = new List<CharacterItem>(playerCharacter.NonEquipItems);

                MoveItemFromStorageResp resp = await DbServiceClient.MoveItemFromStorageAsync(req);
                UITextKeys message = resp.Error;
                if (message != UITextKeys.NONE)
                {
                    result.Invoke(AckResponseCode.Error, new ResponseMoveAllItemsFromStorageMessage()
                    {
                        message = message,
                    });
                    break;
                }
                else
                {
                    playerCharacter.NonEquipItems = resp.InventoryItemItems;
                    GameInstance.ServerStorageHandlers.SetStorageItems(storageId, resp.StorageCharacterItems);
                }
                playerCharacter.FillEmptySlots();
                GameInstance.ServerStorageHandlers.NotifyStorageItemsUpdated(request.storageType, request.storageOwnerId);
            }

            // Success
            result.Invoke(AckResponseCode.Success, new ResponseMoveAllItemsFromStorageMessage());
#endif
        }
    }
}