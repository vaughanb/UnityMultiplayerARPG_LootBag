using UnityEngine;

namespace MultiplayerARPG
{
    [CreateAssetMenu(fileName = "Loot Bag Item Filter Table", menuName = "Create GameData/Loot Bag Item Filter Table", order = -4993)]
    public class LootBagItemFilterTable : ScriptableObject
    {
        [ArrayElementTitle("item")]
        public LootBagFilterItem[] randomItems;
    }
}
