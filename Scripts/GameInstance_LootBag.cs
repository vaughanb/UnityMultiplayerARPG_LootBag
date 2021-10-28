using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class GameInstance : MonoBehaviour
    {
        public Dictionary<string, LootBagEntity> LootBagEntities;
        public LootBagEntity targetLootBagEntity;
    }
}