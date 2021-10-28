namespace MultiplayerARPG
{
    public partial class PlayerCharacter : BaseCharacter
    {
        [Category("Loot Bag Settings")]
        public bool dropAllPlayerItems = false;

        public PlayerCharacter()
        {
            useLootBag = false;
            lootBagEntity = LootBagEntitySelection.Visible;
            lootBagDestroyDelay = 600;
        }
    }
}