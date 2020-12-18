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
                Character = OwningCharacter;

            base.UpdateData();
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
