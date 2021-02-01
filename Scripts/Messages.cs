using LiteNetLib.Utils;

namespace MultiplayerARPG
{
    public struct RequestPickupLootBagItemMessage : INetSerializable
    {
        public int dataId;
        public short fromIndex;
        public short toIndex;

        public void Deserialize(NetDataReader reader)
        {
            dataId = reader.GetPackedInt();
            fromIndex = reader.GetPackedShort();
            toIndex = reader.GetPackedShort();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedInt(dataId);
            writer.PutPackedShort(fromIndex);
            writer.PutPackedShort(toIndex);
        }
    }

    public struct RequestPickupAllLootBagItemsMessage : INetSerializable
    {
        public int dataId;

        public void Deserialize(NetDataReader reader)
        {
            dataId = reader.GetPackedInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutPackedInt(dataId);
        }
    }

    public struct ResponsePickupLootBagItemMessage : INetSerializable
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

    public struct ResponsePickupAllLootBagItemsMessage : INetSerializable
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
