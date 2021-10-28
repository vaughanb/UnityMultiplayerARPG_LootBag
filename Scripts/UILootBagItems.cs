namespace MultiplayerARPG
{
    public class UILootBagItems : UIStorageItems
    {
        bool shouldClose;

        protected override void Update()
        {
            base.Update();    

            if (IsVisible() && shouldClose && CacheSelectionManager.Count == 0)
            {
                Hide();
                shouldClose = false;
            } else if (!IsVisible() && shouldClose)
                shouldClose = false;
        }

        /// <summary>
        /// Loots all items in the bag without prompting per item.
        /// </summary>
        public void OnClickLootAll()
        {
            if (CacheSelectionManager != null)
                CacheSelectionManager.DeselectAll();

            GameInstance.ClientStorageHandlers.RequestMoveAllItemsFromStorage(new RequestMoveAllItemsFromStorageMessage()
            {
                storageType = GameInstance.OpenedStorageType,
                storageOwnerId = GameInstance.OpenedStorageOwnerId,
            }, LootBagStorageActions.ResponseMoveAllItemsFromStorage);

            shouldClose = true;
        }
    }
}