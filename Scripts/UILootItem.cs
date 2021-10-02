using UnityEngine;

namespace MultiplayerARPG
{
    public partial class UILootItem : UICharacterItem
    {
        /// <summary>
        /// Makes sure character is set and calls base UpdateData.
        /// This is necessary to avoid null reference errors.
        /// </summary>
        protected override void UpdateData()
        {
            if (Character == null)
                Character = GameInstance.PlayingCharacter;

            base.UpdateData();
        }

        /// <summary>
        /// Loots the current item.
        /// </summary>
        public void OnClickLootItem()
        {
            (this as UICharacterItem).OnClickLootItem();
        }

        /// <summary>
        /// Deselects and hides the item.
        /// </summary>
        public void Close()
        {
            Deselect();
            Hide();
        }
    }
}
