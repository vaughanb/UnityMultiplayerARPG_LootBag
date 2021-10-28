using Cysharp.Threading.Tasks;
using LiteNetLibManager;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class LanRpgServerStorageMessageHandlers : MonoBehaviour, IServerStorageMessageHandlers
    {
        /// <summary>
        /// Request handler for moving all items from storage to player for LAN mode.
        /// </summary>
        /// <param name="requestHandler">request handler data</param>
        /// <param name="request">request</param>
        /// <param name="result">result</param>
        public async UniTaskVoid HandleRequestMoveAllItemsFromStorage(RequestHandlerData requestHandler, RequestMoveAllItemsFromStorageMessage request, RequestProceedResultDelegate<ResponseMoveAllItemsFromStorageMessage> result)
        {
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

            // Get items from storage
            List<CharacterItem> storageItems = GameInstance.ServerStorageHandlers.GetStorageItems(storageId);

            // Prepare storage data
            Storage storage = GameInstance.ServerStorageHandlers.GetStorage(storageId, out _);
            bool isLimitSlot = storage.slotLimit > 0;
            short slotLimit = storage.slotLimit;

            for (int i = storageItems.Count; i > 0; i--)
            {
                UITextKeys gameMessage;
                if (!playerCharacter.MoveItemFromStorage(isLimitSlot, slotLimit, storageItems, i-1, storageItems[i-1].amount, InventoryType.NonEquipItems, -1, 0, out gameMessage))
                {
                    result.Invoke(AckResponseCode.Error, new ResponseMoveAllItemsFromStorageMessage()
                    {
                        message = gameMessage,
                    });
                    break;
                } else
                {
                    GameInstance.ServerStorageHandlers.SetStorageItems(storageId, storageItems);
                }
            }
            GameInstance.ServerStorageHandlers.NotifyStorageItemsUpdated(request.storageType, request.storageOwnerId);

            // Success
            result.Invoke(AckResponseCode.Success, new ResponseMoveAllItemsFromStorageMessage());
            await UniTask.Yield();
        }
    }
}