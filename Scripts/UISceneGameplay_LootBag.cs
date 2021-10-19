using UnityEngine;

namespace MultiplayerARPG
{
    public partial class UISceneGameplay_LootBag : UISceneGameplay
    {
        [Category("Loot Bag Addon")]
        public UILootBagItems uiLootBagItems;
        private const short lootItemsWeightLimit = 32767; // Maximum short value used to identify loot items

        protected override void Awake()
        {
            base.Awake();

            if (uiLootBagItems == null)
                uiLootBagItems = gameObject.GetComponentInChildren<UILootBagItems>(true);
        }

        /// <summary>
        /// Checks the visibility status of all storage dialogs (including loot bag items).
        /// </summary>
        /// <returns>true if dialog is visible, false otherwise</returns>
        public override bool IsStorageDialogVisible()
        {
            return (uiPlayerStorageItems != null && uiPlayerStorageItems.IsVisible()) ||
                (uiGuildStorageItems != null && uiGuildStorageItems.IsVisible()) ||
                (uiBuildingStorageItems != null && uiBuildingStorageItems.IsVisible()) ||
                (uiBuildingCampfireItems != null && uiBuildingCampfireItems.IsVisible()) ||
                (uiLootBagItems != null && uiLootBagItems.IsVisible());
        }

        /// <summary>
        /// ShowStorageDialog is overriden in order to open loot bag items dialog for loot bag storage.
        /// Currently this is done by checking that weightLimit is the maximum short value.
        /// </summary>
        public override void ShowStorageDialog(StorageType storageType, string storageOwnerId, uint objectId, short weightLimit, short slotLimit)
        {
            // Hide all storage UIs
            if (uiPlayerStorageItems != null)
                uiPlayerStorageItems.Hide(true);
            if (uiGuildStorageItems != null)
                uiGuildStorageItems.Hide(true);
            if (uiBuildingStorageItems != null)
                uiBuildingStorageItems.Hide(true);
            if (uiBuildingCampfireItems != null)
                uiBuildingCampfireItems.Hide(true);
            if (uiLootBagItems != null)
                uiLootBagItems.Hide(true);

            if (uiLootBagItems != null && storageType == StorageType.Building && weightLimit == lootItemsWeightLimit)
            {
                uiLootBagItems.Show(storageType, storageOwnerId, null, weightLimit, slotLimit);
                    AddNpcDialog(uiLootBagItems);
            } else
                base.ShowStorageDialog(storageType, storageOwnerId, objectId, weightLimit, slotLimit);
        }

        /// <summary>
        /// SetTargetEntity is overriden in order to hide the loot bag items dialog when target entity disappears.
        /// </summary>
        public override void SetTargetEntity(BaseGameEntity entity)
        {
            base.SetTargetEntity(entity);

            if (uiLootBagItems != null && uiLootBagItems.IsVisible() && !uiTargetBuilding.IsVisible())
                uiLootBagItems.Hide();
        }
    }
}
