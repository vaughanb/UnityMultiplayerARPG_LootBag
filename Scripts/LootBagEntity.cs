using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public class LootBagEntity : StorageEntity
    {
        [Category("Loot Bag Entity Behavior")]
        public string defaultLootName = "Loot";
        public bool nameLootAfterOwner = true;
        public string appendToLootName = "";
        public bool destroyLootBagWhenEmpty = true;
        public bool immuneToDamage = true;

        [Category("Loot Bag Effect Settings")]
        public GameObject lootBagSparkleEffect;
        public bool showSparkleEffect = true;
        public bool sparkleOnlyWhenItemsInBag = true;

        protected StorageId storageId;
        private LiteNetLibSyncField<string> ownerName = new LiteNetLibSyncField<string>();
        private bool initialized = false;
        private bool hasItems = false;

        public override void OnSetup()
        {
            base.OnSetup();

            storageId = new StorageId(StorageType.Building, Id);
            IsImmune = immuneToDamage;

            if (showSparkleEffect && lootBagSparkleEffect != null && sparkleOnlyWhenItemsInBag)
                lootBagSparkleEffect.SetActive(hasItems);
        }

        protected override void EntityUpdate()
        {
            base.EntityUpdate();

            // if (IsServer && !IsBuildMode && RemainsLifeTime == 0)
            //     Destroy();

            bool hasItems = GameInstance.ServerStorageHandlers.GetStorageItems(storageId).Count > 0;
            if (hasItems)
                initialized = true;

            if (initialized && showSparkleEffect && lootBagSparkleEffect != null && sparkleOnlyWhenItemsInBag)
                lootBagSparkleEffect.SetActive(hasItems);

            if (initialized && destroyLootBagWhenEmpty && !hasItems)
                Destroy();
        }

        /// <summary>
        /// Sets the name of the loot bag entity's owner.
        /// </summary>
        /// <param name="name">monster or character name dropping loot bag</param>
        public void SetOwnerName(string name)
        {
            ownerName.SetValue(name);

            string lootName = "";
            if (nameLootAfterOwner)
                lootName += ownerName;
            
            if (lootName == "")
                lootName = defaultLootName;
            else
                lootName += appendToLootName;
            
            entityTitle = lootName;
        }

        /// <summary>
        /// Adds the provided character items to the loot bag storage.
        /// </summary>
        /// <param name="lootItems">items to add to loot bag</param>
        public void AddItems(List<CharacterItem> lootItems)
        {
            foreach (CharacterItem item in lootItems)
            {
                CharacterItem tempItem = item.Clone();
                GameInstance.ServerStorageHandlers.AddLootBagItems(storageId, tempItem).Forget();
            }
        }
    }
}