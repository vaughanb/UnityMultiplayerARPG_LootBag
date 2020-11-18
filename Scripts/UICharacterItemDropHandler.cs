using UnityEngine;
using UnityEngine.EventSystems;

namespace MultiplayerARPG
{
    public class UICharacterItemDropHandler : MonoBehaviour, IDropHandler
    {
        public UICharacterItem uiCharacterItem;

        private RectTransform dropRect;
        public RectTransform DropRect
        {
            get
            {
                if (dropRect == null)
                    dropRect = transform as RectTransform;
                return dropRect;
            }
        }

        private void Start()
        {
            if (uiCharacterItem == null)
                uiCharacterItem = GetComponent<UICharacterItem>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (uiCharacterItem == null)
            {
                Debug.LogWarning("[UICharacterItemDropHandler] `uicharacterItem` is empty");
                return;
            }
            // Validate drop position
            if (!RectTransformUtility.RectangleContainsScreenPoint(DropRect, Input.mousePosition))
                return;
            // Validate dragging UI
            UIDragHandler dragHandler = eventData.pointerDrag.GetComponent<UIDragHandler>();
            if (dragHandler == null || dragHandler.isDropped)
                return;
            // Get dragged item UI, if dragging item UI is UI for character item.
            // try to equip the item
            UICharacterItemDragHandler draggedItemUI = dragHandler as UICharacterItemDragHandler;
            if (draggedItemUI != null && draggedItemUI.uiCharacterItem != uiCharacterItem)
            {
                switch (draggedItemUI.sourceLocation)
                {
                    case UICharacterItemDragHandler.SourceLocation.EquipItems:
                        break;
                    case UICharacterItemDragHandler.SourceLocation.NonEquipItems:
                        OnDropNonEquipItem(draggedItemUI);
                        break;
                    case UICharacterItemDragHandler.SourceLocation.StorageItems:
                        OnDropStorageItem(draggedItemUI);
                        break;
                    case UICharacterItemDragHandler.SourceLocation.LootItems:
                        OnLootItem(draggedItemUI);
                        break;
                }
            }
        }

        private void OnDropNonEquipItem(UICharacterItemDragHandler draggedItemUI)
        {
            // Set UI drop state
            draggedItemUI.isDropped = true;

            switch (uiCharacterItem.InventoryType)
            {
                case InventoryType.NonEquipItems:
                    // Drop non equip item to non equip item
                    BasePlayerCharacterController.OwningCharacter.CallServerSwapOrMergeItem((short)draggedItemUI.uiCharacterItem.IndexOfData, (short)uiCharacterItem.IndexOfData);
                    break;
                case InventoryType.EquipItems:
                case InventoryType.EquipWeaponRight:
                case InventoryType.EquipWeaponLeft:
                    // Drop non equip item to equip item
                    EquipItem(draggedItemUI);
                    break;
                case InventoryType.StorageItems:
                    // Drop non equip item to storage item
                    BasePlayerCharacterController.OwningCharacter.CallServerMoveItemToStorage((short)draggedItemUI.uiCharacterItem.IndexOfData, draggedItemUI.uiCharacterItem.CharacterItem.amount, (short)uiCharacterItem.IndexOfData);
                    break;
            }
        }

        private void OnDropStorageItem(UICharacterItemDragHandler draggedItemUI)
        {
            // Set UI drop state
            draggedItemUI.isDropped = true;

            switch (uiCharacterItem.InventoryType)
            {
                case InventoryType.NonEquipItems:
                    // Drop storage item to non equip item
                    BasePlayerCharacterController.OwningCharacter.CallServerMoveItemFromStorage((short)draggedItemUI.uiCharacterItem.IndexOfData, draggedItemUI.uiCharacterItem.CharacterItem.amount, (short)uiCharacterItem.IndexOfData);
                    break;
                case InventoryType.StorageItems:
                    // Drop storage item to storage item
                    BasePlayerCharacterController.OwningCharacter.CallServerSwapOrMergeStorageItem((short)draggedItemUI.uiCharacterItem.IndexOfData, (short)uiCharacterItem.IndexOfData);
                    break;
            }
        }

        private void OnLootItem(UICharacterItemDragHandler draggedItemUI)
        {
            // Set UI drop state
            draggedItemUI.isDropped = true;

            if (uiCharacterItem.InventoryType == InventoryType.NonEquipItems)
            {
                BasePlayerCharacterEntity owningCharacter = BasePlayerCharacterController.OwningCharacter;
                owningCharacter.CallServerPickupLootBagItem(draggedItemUI.sourceObjectId, (short)draggedItemUI.uiCharacterItem.IndexOfData, (short)uiCharacterItem.IndexOfData);
            }
        }

        private void EquipItem(UICharacterItemDragHandler draggedItemUI)
        {
            // Don't equip the item if drop area is not setup as equip slot UI
            if (!uiCharacterItem.IsSetupAsEquipSlot)
                return;

            // Get owing character
            BasePlayerCharacterEntity owningCharacter = BasePlayerCharacterController.OwningCharacter;
            if (owningCharacter == null)
                return;

            // Detect type of equipping slot and validate
            IArmorItem armorItem = draggedItemUI.uiCharacterItem.CharacterItem.GetArmorItem();
            IWeaponItem weaponItem = draggedItemUI.uiCharacterItem.CharacterItem.GetWeaponItem();
            IShieldItem shieldItem = draggedItemUI.uiCharacterItem.CharacterItem.GetShieldItem();
            switch (uiCharacterItem.InventoryType)
            {
                case InventoryType.EquipItems:
                    if (armorItem == null ||
                        !armorItem.EquipPosition.Equals(uiCharacterItem.EquipPosition))
                    {
                        // Check if it's correct equip position or not
                        BaseGameNetworkManager.Singleton.ClientReceiveGameMessage(new GameMessage()
                        {
                            type = GameMessage.Type.CannotEquip
                        });
                        return;
                    }
                    break;
                case InventoryType.EquipWeaponRight:
                case InventoryType.EquipWeaponLeft:
                    if (weaponItem == null &&
                        shieldItem == null)
                    {
                        // Check if it's correct equip position or not
                        BaseGameNetworkManager.Singleton.ClientReceiveGameMessage(new GameMessage()
                        {
                            type = GameMessage.Type.CannotEquip
                        });
                        return;
                    }
                    break;
            }
            // Can equip the item
            // so tell the server that this client want to equip the item
            owningCharacter.CallServerEquipItem(
                (short)draggedItemUI.uiCharacterItem.IndexOfData,
                uiCharacterItem.InventoryType,
                uiCharacterItem.EquipSlotIndex);
        }
    }
}
