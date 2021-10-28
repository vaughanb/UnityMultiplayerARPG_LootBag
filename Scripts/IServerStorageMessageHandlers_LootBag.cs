using Cysharp.Threading.Tasks;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public partial interface IServerStorageMessageHandlers
    {
        /// <summary>
        /// Handles requests to move all items from storage to player.
        /// </summary>
        /// <param name="requestHandler">request handler data</param>
        /// <param name="request">request</param>
        /// <param name="result">result</param>
        UniTaskVoid HandleRequestMoveAllItemsFromStorage(
            RequestHandlerData requestHandler, RequestMoveAllItemsFromStorageMessage request,
            RequestProceedResultDelegate<ResponseMoveAllItemsFromStorageMessage> result);
    }
}