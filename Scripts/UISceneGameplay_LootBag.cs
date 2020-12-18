using UnityEngine;

namespace MultiplayerARPG
{
    public partial class UISceneGameplay : BaseUISceneGameplay
    {
        [Header("Loot Bag Addon")]
        public UILootItems uiLootItems;

        /// <summary>
        /// Shows loot bag and updates loot data for target character entity.
        /// </summary>
        /// <param name="characterEntity">entity to show loot bag for</param>
        public void OnShowLootBag(BaseCharacterEntity characterEntity = null)
        {
            if (uiLootItems == null)
                return;

            if (!uiLootItems.IsVisible())
            {
                uiLootItems.Show();
                uiLootItems.UpdateData(characterEntity);
            }
        }
    }
}
