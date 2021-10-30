namespace MultiplayerARPG
{
    public partial class PlayerCharacter : BaseCharacter
    {
        [Category("Loot Bag Settings")]
        public bool dropAllPlayerItems = false;
        public bool dropLootInNonPVPAreas = true;
        public bool dropLootInPVPAreas = true;
        public bool dropLootInFactionPVPAreas = true;
        public bool dropLootInGuildPVPAreas = true;

        public PlayerCharacter()
        {
            useLootBag = false;
            lootBagEntity = LootBagEntitySelection.Visible;
            lootBagDestroyDelay = 600;
        }
    }
}