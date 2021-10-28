using LiteNetLibManager;

namespace MultiplayerARPG
{
    public static class LootBagStorageActions
    {
        public static System.Action<ResponseHandlerData, AckResponseCode, ResponseMoveAllItemsFromStorageMessage> onResponseMoveAllItemsFromStorage;

        /// <summary>
        /// Response callback for MoveAllItemsFromStorage
        /// </summary>
        public static void ResponseMoveAllItemsFromStorage(ResponseHandlerData requestHandler, AckResponseCode responseCode, ResponseMoveAllItemsFromStorageMessage response)
        {
            ClientGenericActions.ClientReceiveGameMessage(response.message);
            if (onResponseMoveAllItemsFromStorage != null)
                onResponseMoveAllItemsFromStorage.Invoke(requestHandler, responseCode, response);
        }
    }
}