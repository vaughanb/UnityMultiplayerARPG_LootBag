namespace MultiplayerARPG
{
    public partial class MonsterCharacter : BaseCharacter
    {
        [Category("Loot Bag Settings")]
        public bool syncDestroyDelayWithBody = true;

        public MonsterCharacter()
        {
            lootBagDestroyDelay = 30;
        }
    }
}