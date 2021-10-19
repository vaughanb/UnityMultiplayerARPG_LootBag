using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace MultiplayerARPG
{
    public partial class UICharacterItem : UIDataForCharacter<UICharacterItemData>
    {
        /// <summary>
        /// Moves the specified item from storage to the player's inventory.
        /// </summary>
        public void MoveItemFromStorage(InventoryType inventoryType, byte equipSlotIndex, short inventoryItemIndex, int amount)
        {
            OnClickMoveFromStorageConfirmed(inventoryType, equipSlotIndex, inventoryItemIndex, amount);
        }
    }
}