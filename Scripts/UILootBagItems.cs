using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
            int itemCount = CacheSelectionManager.Count;
            for (int i = itemCount; i > 0; i--)
            {
                UICharacterItem uici = CacheSelectionManager.Get(i-1);
                uici.MoveItemFromStorage(uici.InventoryType, uici.EquipSlotIndex, -1, uici.CharacterItem.amount);
            }

            shouldClose = true;
        }
    }
}