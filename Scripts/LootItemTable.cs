using UnityEngine;

namespace MultiplayerARPG
{
    [CreateAssetMenu(fileName = "Loot Item Table", menuName = "Create GameData/Loot Item Table", order = -4993)]
    public class LootItemTable : ScriptableObject
    {
        [ArrayElementTitle("item")]
        public ItemDrop[] randomItems;
    }
}
