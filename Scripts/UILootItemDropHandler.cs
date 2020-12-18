using UnityEngine;
using UnityEngine.EventSystems;

namespace MultiplayerARPG
{
    public class UILootItemDropHandler : UICharacterItemDropHandler
    {
        /// <summary>
        /// Handle drop of loot item or call base OnDrop method otherwise.
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnDrop(PointerEventData eventData)
        {
            if (uiCharacterItem == null)
            {
                Debug.LogWarning("[UICharacterItemDropHandler] `uicharacterItem` is empty");
                return;
            }
            // Validate drop position
            if (!RectTransformUtility.RectangleContainsScreenPoint(DropRect, Input.mousePosition))
                return;

            UIDragHandler dragHandler = eventData.pointerDrag.GetComponent<UIDragHandler>();
            if (dragHandler == null || dragHandler.isDropped)
                return;

            UILootItemDragHandler draggedItemUI = dragHandler as UILootItemDragHandler;
            if (draggedItemUI != null && draggedItemUI.uiCharacterItem != uiCharacterItem)
            {
                if (draggedItemUI.sourceLocation == draggedItemUI.LootItems)
                {
                    OnLootItem(draggedItemUI);
                    Destroy(dropRect);
                }
                else
                {
                    base.OnDrop(eventData);
                }
            }            
        }

        /// <summary>
        /// Loot item implementation on drop.
        /// </summary>
        /// <param name="draggedItemUI">Item being dropped.</param>
        private void OnLootItem(UILootItemDragHandler draggedItemUI)
        {
            // Set UI drop state
            draggedItemUI.isDropped = true;

            if (uiCharacterItem.InventoryType == InventoryType.NonEquipItems)
            {
                BasePlayerCharacterEntity owningCharacter = BasePlayerCharacterController.OwningCharacter;
                owningCharacter.CallServerPickupLootBagItem(draggedItemUI.sourceObjectId, (short)draggedItemUI.uiCharacterItem.IndexOfData, (short)uiCharacterItem.IndexOfData);
            }
        }
    }
}
