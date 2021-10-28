using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using LiteNetLibManager;
using LiteNetLib;
using System;

namespace MultiplayerARPG
{
    public class LootBagEntity : StorageEntity
    {
        [Category("Loot Bag Entity Behavior")]
        public string defaultLootName = "Loot";
        public bool nameLootAfterOwner = true;
        public string appendToLootName = "";
        public bool destroyLootBagWhenEmpty = true;
        public bool destroyLootBagWithBody = true;
        public bool immuneToDamage = true;

        [Category("Loot Bag Effect Settings")]
        public GameObject lootBagSparkleEffect;
        public bool showSparkleEffect = true;
        public bool sparkleOnlyWhenItemsInBag = true;

        private BaseCharacterEntity ownerEntity;
        private bool initialized = false;
        private bool _dirtyIsOpen;
        private DateTime setupTime;

        [Category("Sync Fields")]
        protected SyncFieldString ownerName = new SyncFieldString();
        protected SyncFieldBool hasItems = new SyncFieldBool();

        protected string OwnerName
        {
            get { return ownerName.Value; }
            set { ownerName.Value = value; }
        }

        protected bool HasItems
        {
            get { return hasItems.Value; }
            set { hasItems.Value = value; }
        }

        public override void OnSetup()
        {
            base.OnSetup();

            // storageId = new StorageId(StorageType.Building, Id);
            IsImmune = immuneToDamage;

            if (showSparkleEffect && lootBagSparkleEffect != null && sparkleOnlyWhenItemsInBag)
                lootBagSparkleEffect.SetActive(HasItems);

            if (RemainsLifeTime == 0)
                RemainsLifeTime = LifeTime;

            SetLootBagName();

            setupTime = DateTime.Now;
        }

        protected override void SetupNetElements()
        {
            base.SetupNetElements();
            ownerName.deliveryMethod = DeliveryMethod.ReliableOrdered;
            ownerName.syncMode = LiteNetLibSyncField.SyncMode.ServerToClients;
            hasItems.deliveryMethod = DeliveryMethod.ReliableOrdered;
            hasItems.syncMode = LiteNetLibSyncField.SyncMode.ServerToClients;
        }

        protected override void EntityUpdate()
        {
            if (IsServer)
            {
                HasItems = GameInstance.ServerStorageHandlers.GetStorageEntityItems(this).Count > 0;

                bool updatingIsOpen = GameInstance.ServerStorageHandlers.IsStorageEntityOpen(this);
                if (updatingIsOpen != _dirtyIsOpen)
                {
                    _dirtyIsOpen = updatingIsOpen;
                    isOpen.Value = updatingIsOpen;
                }

                if (RemainsLifeTime <= 0)
                    Destroy();

                if (initialized && destroyLootBagWhenEmpty && !HasItems)
                    Destroy();

                if (destroyLootBagWithBody && ownerEntity == null)
                    Destroy();
            }

            if (HasItems || DateTime.Now > setupTime.AddSeconds(3))
                initialized = true;

            if (initialized && showSparkleEffect && lootBagSparkleEffect != null && sparkleOnlyWhenItemsInBag)
                lootBagSparkleEffect.SetActive(HasItems);
        }

        /// <summary>
        /// Sets the loot bag's owner entity.
        /// </summary>
        /// <param name="entity">entity that dropped the loot bag</param>
        public void SetOwnerEntity(BaseCharacterEntity entity)
        {
            ownerEntity = entity;
            if (ownerEntity == null)
                return;

            string entityName = ownerEntity.EntityTitle;
            if (ownerEntity is BasePlayerCharacterEntity)
                entityName = ownerEntity.CharacterName;

            if (!string.IsNullOrEmpty(entityName))
                OwnerName = entityName;

            SetLootBagName();
        }

        /// <summary>
        /// Sets the name of the loot bag.
        /// </summary>
        public void SetLootBagName()
        {
            string lootName = "";
            if (nameLootAfterOwner && !string.IsNullOrEmpty(OwnerName))
                lootName += OwnerName;
            
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
            if (!IsServer)
                return;

            List<CharacterItem> items = new List<CharacterItem>(GameInstance.ServerStorageHandlers.GetStorageEntityItems(this));
            if (items.Count > 0)
                return;

            List<CharacterItem> lootItemClones = new List<CharacterItem>();
            foreach (CharacterItem lootItem in lootItems)
            {
                lootItemClones.Add(lootItem.Clone());
            }

            AddItemsToStorage(lootItems).Forget();
        }

        protected async UniTaskVoid AddItemsToStorage(List<CharacterItem> lootItems)
        {
            List<BaseItem> items = new List<BaseItem>();
            foreach (CharacterItem item in lootItems)
            {
                items.Add(item.GetItem());
            }
            GameInstance.AddItems(items);

            StorageId storageId = new StorageId(StorageType.Building, Id);
            await GameInstance.ServerStorageHandlers.AddLootBagItems(storageId, lootItems);
        }

        /// <summary>
        /// Sets the destroy delay for the loot bag entity.
        /// </summary>
        /// <param name="delay">number of seconds before destroying</param>
        public void SetDestroyDelay(float delay)
        {
            this.lifeTime = delay;
            this.RemainsLifeTime = delay;
        }
    }
}