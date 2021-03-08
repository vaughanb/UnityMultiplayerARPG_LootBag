using Cysharp.Threading.Tasks;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public static class LootBagActions
    {
        public static System.Action<ResponseHandlerData, AckResponseCode, ResponsePickupLootBagItemMessage> onResponsePickupLootBagItem;
        public static System.Action<ResponseHandlerData, AckResponseCode, ResponsePickupAllLootBagItemsMessage> onResponsePickupAllLootBagItems;

        public static void ResponsePickupLootBagItem(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponsePickupLootBagItemMessage response)
        {
            ClientGenericActions.ClientReceiveGameMessage(response.message);
            if (onResponsePickupLootBagItem != null)
                onResponsePickupLootBagItem.Invoke(requestHandler, responseCode, response);
        }

        public static void ResponsePickupAllLootBagItems(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponsePickupAllLootBagItemsMessage response)
        {
            ClientGenericActions.ClientReceiveGameMessage(response.message);
            if (onResponsePickupAllLootBagItems != null)
                onResponsePickupAllLootBagItems.Invoke(requestHandler, responseCode, response);
        }
    }
}
