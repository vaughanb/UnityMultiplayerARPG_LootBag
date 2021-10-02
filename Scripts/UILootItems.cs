using UnityEngine;

namespace MultiplayerARPG
{
    public class UILootItems : UICharacterItems
    {
        public UILootItem uiLootItemDialog;

        private BasePlayerCharacterEntity playerCharacterEntity;
        private BaseCharacterEntity characterEntity;

        private bool closeBag;

        /// <summary>
        /// Configures the selected item to be view in the UILootItemDialog window.
        /// </summary>
        /// <param name="ui">UICharacterItem being selected</param>
        protected override void OnSelect(UICharacterItem ui)
        {
            if (uiLootItemDialog == null)
                return;

            if (ui.Data.characterItem.IsEmptySlot())
            {
                CacheSelectionManager.DeselectSelectedUI();
                return;
            }
            else
            {
                if (!uiLootItemDialog.isActiveAndEnabled)
                    uiLootItemDialog.gameObject.SetActive(true);

                uiLootItemDialog.selectionManager = CacheSelectionManager;
                uiLootItemDialog.Setup(ui.Data, Character, ui.IndexOfData);
                uiLootItemDialog.Show();
            }
        }

        /// <summary>
        /// Closes the item UI on item deselect.
        /// </summary>
        /// <param name="ui">Character item being deselected</param>
        protected override void OnDeselect(UICharacterItem ui)
        {
            if (uiDialog == null)
                return;

            CloseItemUI();
        }

        /// <summary>
        /// Updates the loot items and closes the bag if it has no contents.
        /// </summary>
        protected override void Update()
        {
            if (characterEntity == null)
                CloseBag();
                
            UpdateLootItems();

            if (closeBag && characterEntity != null && characterEntity.LootBag.Count == 0)
                CloseBag();
        }

        /// <summary>
        /// Updates loot items and character entity.
        /// </summary>
        /// <param name="baseCharacterEntity">BaseCharacterEntity being looted</param>
        public void UpdateData(BaseCharacterEntity baseCharacterEntity = null)
        {
            characterEntity = baseCharacterEntity;

            playerCharacterEntity = BasePlayerCharacterController.OwningCharacter;
            if (characterEntity == null)
                characterEntity = playerCharacterEntity.GetTargetEntity() as BaseMonsterCharacterEntity;

            if (characterEntity == null)
                return;

            UpdateLootItems();
        }

        /// <summary>
        /// Updates all loot items in the UI based on the loot items in the monster entity.
        /// </summary>
        public void UpdateLootItems()
        {
            int selectedIdx = CacheSelectionManager.SelectedUI != null ? CacheSelectionManager.IndexOf(CacheSelectionManager.SelectedUI) : -1;
            CacheSelectionManager.DeselectSelectedUI();
            CacheSelectionManager.Clear();

            if (characterEntity == null || characterEntity.LootBag == null)
                return;

            UICharacterItem tempUiCharacterItem;
            CacheList.Generate(characterEntity.LootBag, (index, characterItem, ui) =>
            {
                tempUiCharacterItem = ui.GetComponent<UICharacterItem>();
                InventoryType LootItem = (InventoryType)5;
                tempUiCharacterItem.Setup(new UICharacterItemData(characterItem, characterItem.level, LootItem), BasePlayerCharacterController.OwningCharacter, index);
                tempUiCharacterItem.Show();

                UILootItemDragHandler dragHandler = tempUiCharacterItem.GetComponentInChildren<UILootItemDragHandler>();
                if (dragHandler != null)
                    dragHandler.SetupForLootItems(tempUiCharacterItem, characterEntity.ObjectId);

                CacheSelectionManager.Add(tempUiCharacterItem);
                if (selectedIdx == index)
                    tempUiCharacterItem.OnClickSelect();
            });
        }

        /// <summary>
        /// Takes all items in the loot bag.
        /// </summary>
        public void OnClickLootAll()
        {
            CloseItemUI();

            GameInstance.LootBagHandlers.RequestPickupAllLootBagItems(new RequestPickupAllLootBagItemsMessage()
            {
                dataId = (int)characterEntity.ObjectId,
            }, LootBagActions.ResponsePickupAllLootBagItems);

            closeBag = true;
        }

        /// <summary>
        /// Calls close on the attached UILootItemDialog.
        /// </summary>
        public void CloseItemUI()
        {
            uiLootItemDialog.Close();
        }

        /// <summary>
        /// Closes the bag and any open item UI windows.
        /// </summary>
        public void CloseBag()
        {
            CloseItemUI();
            Hide();

            closeBag = false;
        }
    }
}