using UnityEngine;

namespace MultiplayerARPG
{
    [System.Serializable]
    public struct LootBagFilterItem
    {
        public BaseItem item;
        [Tooltip("Drop rate applies only to inclusive loot bag filter behavior.")]
        [Range(0f, 1f)]
        public float dropRate;
    }
}
