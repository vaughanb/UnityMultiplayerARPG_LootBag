using UnityEngine;
using System;

namespace MultiplayerARPG
{
    public partial class UISceneGameplay_LootBag : UISceneGameplay
    {
        [Category("Loot Bag Addon")]
        public UILootBagItems uiLootBagItems;

        protected override void Awake()
        {
            base.Awake();
            UISetup();
        }

        /// <summary>
        /// Adds the loot bag specific UI components to UISceneGameplay and configures them to be displayed.
        /// </summary>
        public virtual void UISetup()
        {
            // Add UILootBagItemStorageDialog if it does not already exist
            if (!gameObject.GetComponentInChildren<UILootBagItems>())
            {
                GameObject uiLootBagStorageDialog = Instantiate(Resources.Load("UI/UILootBagStorageDialog") as GameObject);
                uiLootBagStorageDialog.transform.SetParent(transform);
                uiLootBagStorageDialog.transform.localPosition = Vector3.zero;
                uiLootBagStorageDialog.transform.localScale = Vector3.one;
            }

            // Assign the loot bag UI dialog to UISceneGameplay
            if (uiLootBagItems == null)
                uiLootBagItems = gameObject.GetComponentInChildren<UILootBagItems>(true);

            // Add the loot bag activate button activator and configure it (for mobile)
            if (gameObject.GetComponentInChildren<ActivateButtonActivator>() && !gameObject.GetComponentInChildren<ActivateButtonActivator_LootBag>())
            {
                var oldActivator = GetComponentInChildren<ActivateButtonActivator>();
                if (oldActivator == null)
                    return;

                var newActivator = oldActivator.gameObject.AddComponent<ActivateButtonActivator_LootBag>();
                oldActivator.enabled = false;

                MobileInputButton[] inputButtons = GetComponentsInChildren<MobileInputButton>();
                foreach (MobileInputButton ib in inputButtons)
                {
                    var text = ib.gameObject.GetComponentInChildren<UnityEngine.UI.Text>();
                    if (text != null && text.text != "ACTIVE")
                        continue;

                    newActivator.activateObjects.Add(ib.gameObject);

                    TextWrapper tw = ib.gameObject.AddComponent<TextWrapper>();
                    tw.unityText = text;

                    newActivator.buttonTextWrapper = tw;
                }
            }
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
        /// ShowStorageDialog is overridden in order to open loot bag items dialog for loot bag storage.
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

            if (uiLootBagItems != null && storageType == StorageType.Building && GameInstance.Singleton.targetLootBagEntity != null)
            {
                uiLootBagItems.Show(storageType, storageOwnerId, null, weightLimit, slotLimit);
                    AddNpcDialog(uiLootBagItems);
            } else
                base.ShowStorageDialog(storageType, storageOwnerId, objectId, weightLimit, slotLimit);
        }

        /// <summary>
        /// SetTargetEntity is overriden to show loot item target UI instead of building target UI 
        /// when entity is loot bag, and to hide the loot bag items dialog when target entity disappears.
        /// </summary>
        public override void SetTargetEntity(BaseGameEntity entity)
        {
            base.SetTargetEntity(entity);

            if (entity != null && entity is LootBagEntity)
            {
                GameInstance.Singleton.targetLootBagEntity = entity as LootBagEntity;

                if (uiTargetItemDrop == null)
                    return;

                if (uiTargetBuilding != null)
                    uiTargetBuilding.Hide();

                uiTargetItemDrop.Data = entity;
                uiTargetItemDrop.Show();
            } else {
                GameInstance.Singleton.targetLootBagEntity = null;
            }

            if (uiLootBagItems != null && uiLootBagItems.IsVisible() && !uiTargetItemDrop.IsVisible())
                uiLootBagItems.Hide();
        }
    }
}
