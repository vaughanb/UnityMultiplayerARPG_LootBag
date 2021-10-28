using LiteNetLibManager;
using LiteNetLib.Utils;

namespace MultiplayerARPG
{
    public partial interface IClientStorageHandlers
    {
        bool RequestMoveAllItemsFromStorage(RequestMoveAllItemsFromStorageMessage data, ResponseDelegate<ResponseMoveAllItemsFromStorageMessage> callback);
    }

    /// <summary>
    /// MoveAllItemsFromStorage request.
    /// </summary>
    public struct RequestMoveAllItemsFromStorageMessage : INetSerializable
    {
        public StorageType storageType;
        public string storageOwnerId;

        public void Deserialize(NetDataReader reader)
        {
            storageType = (StorageType)reader.GetByte();
            storageOwnerId = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put((byte)storageType);
            writer.Put(storageOwnerId);
        }
    }

    /// <summary>
    /// MoveAllItemsFromStorage response.
    /// </summary>
    public struct ResponseMoveAllItemsFromStorageMessage : INetSerializable
    {
        public UITextKeys message;

        public void Deserialize(NetDataReader reader)
        {
            message = (UITextKeys)reader.GetPackedUShort();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedUShort((ushort)message);
        }
    }
}