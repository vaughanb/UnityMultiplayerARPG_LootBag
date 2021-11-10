using UnityEngine;

namespace MultiplayerARPG
{
    [CreateAssetMenu(fileName = "Loot Bag Item Table", menuName = "Create GameData/Loot Bag Item Table", order = -4993)]
    public class LootBagItemTable : ScriptableObject
    {
        [ArrayElementTitle("item")]
        public ItemDrop[] randomItems;
    }
}
