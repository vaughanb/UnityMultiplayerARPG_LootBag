using UnityEngine;

namespace MultiplayerARPG
{
    [System.Serializable]
    public struct LootBagFilterItem
    {
        public BaseItem item;
        [Range(0f, 1f)]
        public float dropRate;
    }
}
