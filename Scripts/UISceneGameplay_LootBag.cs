using UnityEngine;

namespace MultiplayerARPG
{
    public partial class UISceneGameplay : BaseUISceneGameplay
    {
        [Header("Loot Bag Addon")]
        public UILootItems uiLootItems;

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
