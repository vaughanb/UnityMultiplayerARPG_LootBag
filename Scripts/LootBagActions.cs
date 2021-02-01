using Cysharp.Threading.Tasks;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public static class LootBagActions
    {
        public static System.Action<ResponseHandlerData, AckResponseCode, ResponsePickupLootBagItemMessage> onResponsePickupLootBagItem;
        public static System.Action<ResponseHandlerData, AckResponseCode, ResponsePickupAllLootBagItemsMessage> onResponsePickupAllLootBagItems;

        public static async UniTaskVoid ResponsePickupLootBagItem(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponsePickupLootBagItemMessage response)
        {
            await UniTask.Yield();
            ClientGenericActions.ClientReceiveGameMessage(response.message);
            if (onResponsePickupLootBagItem != null)
                onResponsePickupLootBagItem.Invoke(requestHandler, responseCode, response);
        }

        public static async UniTaskVoid ResponsePickupAllLootBagItems(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponsePickupAllLootBagItemsMessage response)
        {
            await UniTask.Yield();
            ClientGenericActions.ClientReceiveGameMessage(response.message);
            if (onResponsePickupAllLootBagItems != null)
                onResponsePickupAllLootBagItems.Invoke(requestHandler, responseCode, response);
        }
    }
}
