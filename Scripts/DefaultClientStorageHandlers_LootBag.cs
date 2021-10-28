using LiteNetLibManager;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class DefaultClientStorageHandlers : MonoBehaviour, IClientStorageHandlers
    {
        /// <summary>
        /// Requests to move all items from storage to player.
        /// </summary>
        /// <param name="data">request data</param>
        /// <param name="callback">response callback</param>
        /// <returns>true if success, false otherwise</returns>
        public bool RequestMoveAllItemsFromStorage(RequestMoveAllItemsFromStorageMessage data, ResponseDelegate<ResponseMoveAllItemsFromStorageMessage> callback)
        {
            return Manager.ClientSendRequest(GameNetworkingConsts.MoveAllItemsFromStorage, data, responseDelegate: callback);
        }
    }
}