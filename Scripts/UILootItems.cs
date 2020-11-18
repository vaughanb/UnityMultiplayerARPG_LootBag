using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public class UILootItems : UIBase
    {
        [Header("UI Elements")]
        public UICharacterItem uiItemDialog;
        public UICharacterItem uiCharacterItemPrefab;
        public Transform uiCharacterItemContainer;

        private BasePlayerCharacterEntity playerCharacterEntity;
        private BaseCharacterEntity characterEntity;

        private UIList cacheCharacterItemList;
        public UIList CacheCharacterItemList
        {
            get
            {
                if (cacheCharacterItemList == null)
                {
                    cacheCharacterItemList = gameObject.AddComponent<UIList>();
                    cacheCharacterItemList.uiPrefab = uiCharacterItemPrefab.gameObject;
                    cacheCharacterItemList.uiContainer = uiCharacterItemContainer;
                }
                return cacheCharacterItemList;
            }
        }

        private UICharacterItemSelectionManager cacheCharacterItemSelectionManager;
        public UICharacterItemSelectionManager CacheCharacterItemSelectionManager
        {
            get
            {
                if (cacheCharacterItemSelectionManager == null)
                    cacheCharacterItemSelectionManager = GetComponent<UICharacterItemSelectionManager>();
                if (cacheCharacterItemSelectionManager == null)
                    cacheCharacterItemSelectionManager = gameObject.AddComponent<UICharacterItemSelectionManager>();
                cacheCharacterItemSelectionManager.selectionMode = UISelectionMode.SelectSingle;
                return cacheCharacterItemSelectionManager;
            }
        }

        private bool closeBag;

        public override void Show()
        {
            closeBag = false;
            root.SetActive(true);

            CacheCharacterItemSelectionManager.eventOnSelected.RemoveListener(OnSelectCharacterItem);
            CacheCharacterItemSelectionManager.eventOnSelected.AddListener(OnSelectCharacterItem);
            CacheCharacterItemSelectionManager.eventOnDeselected.RemoveListener(OnDeselectCharacterItem);
            CacheCharacterItemSelectionManager.eventOnDeselected.AddListener(OnDeselectCharacterItem);
            base.Show();
        }

        public override void Hide()
        {
            // Hide
            CacheCharacterItemSelectionManager.DeselectSelectedUI();
            base.Hide();
        }

        protected void OnSelectCharacterItem(UICharacterItem ui)
        {
            if (uiItemDialog == null)
                return;

            if (ui.Data.characterItem.IsEmptySlot())
            {
                CacheCharacterItemSelectionManager.DeselectSelectedUI();
                return;
            }
            else
            {
                uiItemDialog.Show();
                uiItemDialog.Data = new UICharacterItemData(CharacterItem.Create(ui.Data.characterItem.GetItem()), 1, InventoryType.NonEquipItems);
            }
        }

        protected void OnDeselectCharacterItem(UICharacterItem ui)
        {
            if (uiItemDialog == null)
                return;

            uiItemDialog.Hide();
        }

        private void Update()
        {
            UpdateLootItems();

            if (closeBag && characterEntity != null && characterEntity.LootBag.Count == 0)
            {
                closeBag = false;
                Hide();
            }
        }

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
            int selectedIdx = CacheCharacterItemSelectionManager.SelectedUI != null ? CacheCharacterItemSelectionManager.IndexOf(CacheCharacterItemSelectionManager.SelectedUI) : -1;
            CacheCharacterItemSelectionManager.DeselectSelectedUI();
            CacheCharacterItemSelectionManager.Clear();

            if (characterEntity == null || characterEntity.LootBag == null)
                return;

            UICharacterItem tempUiCharacterItem;
            CacheCharacterItemList.Generate(characterEntity.LootBag, (index, characterItem, ui) =>
            {
                tempUiCharacterItem = ui.GetComponent<UICharacterItem>();
                tempUiCharacterItem.Setup(new UICharacterItemData(characterItem, characterItem.level, InventoryType.LootItems), BasePlayerCharacterController.OwningCharacter, index);
                tempUiCharacterItem.Show();

                UICharacterItemDragHandler dragHandler = tempUiCharacterItem.GetComponentInChildren<UICharacterItemDragHandler>();
                if (dragHandler != null)
                    dragHandler.SetupForLootItems(tempUiCharacterItem, characterEntity.ObjectId);

                CacheCharacterItemSelectionManager.Add(tempUiCharacterItem);
                if (selectedIdx == index)
                    tempUiCharacterItem.OnClickSelect();
            });
        }

        /// <summary>
        /// Takes all items in the loot bag.
        /// </summary>
        public void OnClickLootAll()
        {
            playerCharacterEntity.CallServerPickupAllLootBagItems(characterEntity.ObjectId);
            closeBag = true;
        }
    }
}
