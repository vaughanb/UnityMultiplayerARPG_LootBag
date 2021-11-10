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
        private bool ownerSet = false;
        private DateTime setupTime;

        [Category("Sync Fields")]
        protected SyncFieldUInt ownerObjectID = new SyncFieldUInt();
        protected SyncFieldBool hasItems = new SyncFieldBool();

        protected uint OwnerObjectID
        {
            get { return ownerObjectID.Value; }
            set { ownerObjectID.Value = value; }
        }

        protected bool HasItems
        {
            get 
            { 
                return hasItems.Value; 
            }
            set 
            {
                if (value != hasItems.Value)
                    hasItems.Value = value; 
            }
        }

        public override void OnSetup()
        {
            base.OnSetup();

            IsImmune = immuneToDamage;

            if (IsClient)
            {
                if (showSparkleEffect && lootBagSparkleEffect != null)
                {
                    if (sparkleOnlyWhenItemsInBag)
                        lootBagSparkleEffect.SetActive(HasItems);
                    else
                        lootBagSparkleEffect.SetActive(true);
                }

                setupTime = DateTime.Now;
            }
        }

        protected override void SetupNetElements()
        {
            base.SetupNetElements();
            hasItems.deliveryMethod = DeliveryMethod.ReliableOrdered;
            hasItems.syncMode = LiteNetLibSyncField.SyncMode.ServerToClients;
            ownerObjectID.deliveryMethod = DeliveryMethod.ReliableOrdered;
            ownerObjectID.syncMode = LiteNetLibSyncField.SyncMode.ServerToClients;
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

                if (lifeTime > 0f)
                {
                    RemainsLifeTime -= Time.deltaTime;
                    if (RemainsLifeTime < 0)
                    {
                        RemainsLifeTime = 0f;
                        Destroy();
                    }
                }

                if (initialized && destroyLootBagWhenEmpty && !HasItems)
                    Destroy();
            }

            if (IsClient)
            {
                if (HasItems || DateTime.Now > setupTime.AddSeconds(3))
                    initialized = true;

                if (initialized)
                {
                    if (showSparkleEffect && lootBagSparkleEffect != null)
                    {
                        if (sparkleOnlyWhenItemsInBag)
                            lootBagSparkleEffect.SetActive(HasItems);
                        else
                            lootBagSparkleEffect.SetActive(true);
                    }

                    if (destroyLootBagWithBody)
                    {
                        if (!ownerSet && ownerEntity == null)
                            FindOwnerEntity();

                        if (ownerEntity == null)
                        {
                            lootBagSparkleEffect.SetActive(false);
                            Destroy();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the BaseCharacterEntity with the same ID as the storage's creator and sets it as the ownerEntity.
        /// Called by CLIENT only.
        /// </summary>
        protected void FindOwnerEntity()
        {
            BaseCharacterEntity[] characters = FindObjectsOfType<BaseCharacterEntity>();
            foreach (BaseCharacterEntity bce in characters)
            {
                if (bce.ObjectId == OwnerObjectID)
                {
                    ownerEntity = bce;
                    ownerSet = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Sets the loot bag's owner entity. 
        /// Called by SERVER only.
        /// </summary>
        /// <param name="entity">entity that dropped the loot bag</param>
        public void SetOwnerEntity(BaseCharacterEntity entity)
        {
            ownerEntity = entity;
            if (ownerEntity == null)
                return;

            OwnerObjectID = entity.ObjectId;

            if (entity is BasePlayerCharacterEntity)
                Title = entity.CharacterName;
            else
                Title = entity.GetDatabase().DefaultTitle;

            SetLootBagName();
        }

        /// <summary>
        /// Sets the name of the loot bag.
        /// </summary>
        public void SetLootBagName()
        {
            if (Title != "")
            {
                if (nameLootAfterOwner)
                    Title += appendToLootName;
            }
            else
                Title = defaultLootName;
        }

        /// <summary>
        /// Adds the provided character items to the loot bag storage.
        /// </summary>
        /// <param name="lootItems">items to add to loot bag</param>
        public async UniTaskVoid AddItems(List<CharacterItem> lootItems)
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

            bool itemsAdded = await AddItemsToStorage(lootItems);
            if (itemsAdded)
                initialized = true;
        }

        /// <summary>
        /// Adds the specified items to the loot bag and returns true once done.
        /// </summary>
        /// <param name="lootItems">items to add to loot bag</param>
        /// <returns>true</returns>
        protected async UniTask<bool> AddItemsToStorage(List<CharacterItem> lootItems)
        {
            List<BaseItem> items = new List<BaseItem>();
            foreach (CharacterItem item in lootItems)
            {
                items.Add(item.GetItem());
            }
            GameInstance.AddItems(items);

            StorageId storageId = new StorageId(StorageType.Building, Id);
            await GameInstance.ServerStorageHandlers.AddLootBagItems(storageId, lootItems);
            return true;
        }

        /// <summary>
        /// Sets the destroy delay for the loot bag entity.
        /// </summary>
        /// <param name="delay">number of seconds before destroying</param>
        public void SetDestroyDelay(float delay)
        {
            lifeTime = delay;
            RemainsLifeTime = delay;
        }
    }
}