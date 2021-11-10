using UnityEngine;

namespace MultiplayerARPG
{
    public partial class MonsterCharacter : BaseCharacter
    {
        [Category("Loot Bag Settings")]
        [Tooltip("If selected, monster's body will remain as long as loot bag does")]
        public bool syncDestroyDelayWithBody = true;

        public MonsterCharacter()
        {
            lootBagDestroyDelay = 30;
        }
    }
}