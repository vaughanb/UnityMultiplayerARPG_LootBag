using UnityEngine;
using LiteNetLibManager;
using Cysharp.Threading.Tasks;

namespace MultiplayerARPG
{
    public class LootBagHandlers : MonoBehaviour
    {
        public LiteNetLibManager.LiteNetLibManager Manager { get; private set; }

        private void Awake()
        {
            Manager = GetComponent<LiteNetLibManager.LiteNetLibManager>();
        }

        public bool RequestPickupLootBagItem(RequestPickupLootBagItemMessage data, ResponseDelegate<ResponsePickupLootBagItemMessage> callback)
        {
            return Manager.ClientSendRequest(GameNetworkingConsts.PickUpLootBagItem, data, responseDelegate: callback);
        }

        public bool RequestPickupAllLootBagItems(RequestPickupAllLootBagItemsMessage data, ResponseDelegate<ResponsePickupAllLootBagItemsMessage> callback)
        {
            return Manager.ClientSendRequest(GameNetworkingConsts.PickUpAllLootBagItems, data, responseDelegate: callback);
        }

        public async UniTaskVoid HandleRequestPickupLootBagItem(RequestHandlerData requestHandler, RequestPickupLootBagItemMessage request, RequestProceedResultDelegate<ResponsePickupLootBagItemMessage> result)
        {
            IPlayerCharacterData playerCharacter;
            if (!GameInstance.ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out playerCharacter))
            {
                result.Invoke(AckResponseCode.Error, new ResponsePickupLootBagItemMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            UITextKeys gameMessage;
            if (!(playerCharacter as BaseCharacterEntity).PickupLootBagItem(out gameMessage, (uint)request.dataId, request.fromIndex, request.toIndex))
            {
                result.Invoke(AckResponseCode.Error, new ResponsePickupLootBagItemMessage()
                {
                    message = gameMessage,
                });
                return;
            }
            result.Invoke(AckResponseCode.Success, new ResponsePickupLootBagItemMessage());
            await UniTask.Yield();
        }

        public async UniTaskVoid HandleRequestPickupAllLootBagItems(RequestHandlerData requestHandler, RequestPickupAllLootBagItemsMessage request, RequestProceedResultDelegate<ResponsePickupAllLootBagItemsMessage> result)
        {
            IPlayerCharacterData playerCharacter;
            if (!GameInstance.ServerUserHandlers.TryGetPlayerCharacter(requestHandler.ConnectionId, out playerCharacter))
            {
                result.Invoke(AckResponseCode.Error, new ResponsePickupAllLootBagItemsMessage()
                {
                    message = UITextKeys.UI_ERROR_NOT_LOGGED_IN,
                });
                return;
            }
            UITextKeys gameMessage;
            if (!(playerCharacter as BaseCharacterEntity).PickupAllLootBagItems(out gameMessage, (uint)request.dataId))
            {
                result.Invoke(AckResponseCode.Error, new ResponsePickupAllLootBagItemsMessage()
                {
                    message = gameMessage,
                });
                return;
            }
            result.Invoke(AckResponseCode.Success, new ResponsePickupAllLootBagItemsMessage());
            await UniTask.Yield();
        }
    }
}
