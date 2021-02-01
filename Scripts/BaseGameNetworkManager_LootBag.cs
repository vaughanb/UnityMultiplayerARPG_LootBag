using LiteNetLibManager;

namespace MultiplayerARPG
{
    public abstract partial class BaseGameNetworkManager : LiteNetLibGameManager
    {
        protected LootBagHandlers LootBagHandlers { get; set; }

        [DevExtMethods("RegisterServerMessages")]
        protected void DevExt_RegisterServerMessages()
        {
            LootBagHandlers = gameObject.GetOrAddComponent<LootBagHandlers>();
            GameInstance.LootBagHandlers = LootBagHandlers;

            if (LootBagHandlers != null)
            {
                RegisterRequestToServer<RequestPickupLootBagItemMessage, ResponsePickupLootBagItemMessage>(GameNetworkingConsts.PickUpLootBagItem, LootBagHandlers.HandleRequestPickupLootBagItem);
                RegisterRequestToServer<RequestPickupAllLootBagItemsMessage, ResponsePickupAllLootBagItemsMessage>(GameNetworkingConsts.PickUpAllLootBagItems, LootBagHandlers.HandleRequestPickupAllLootBagItems);
            }
        }

        [DevExtMethods("OnStartClient")]
        protected void DevExt_OnStartClient()
        {
            GameInstance.LootBagHandlers = LootBagHandlers;
        }
    }
}
