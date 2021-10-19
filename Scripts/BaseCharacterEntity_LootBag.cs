using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public abstract partial class BaseCharacterEntity : DamageableEntity, ICharacterData
    {
        protected BaseCharacter characterDB;

        [DevExtMethods("Awake")]
        protected void OnAwake()
        {
            onReceivedDamage += OnReceivedDamage;
        }

        /// <summary>
        /// Checks for death on damage received and calls OnDeath if dead.
        /// </summary>
        protected void OnReceivedDamage(Vector3 fromPosition, IGameEntity attacker, CombatAmountType combatAmountType, int damage, CharacterItem weapon, BaseSkill skill, short skillLevel)
        {
            if (this.IsDead())
                OnDeath();
        }

        /// <summary>
        /// Event that occurs on death. Used primarily to trigger dropping of loot bag.
        /// </summary>
        protected virtual void OnDeath()
        {
            characterDB = this.GetDatabase();
            if (characterDB == null) 
            {
                Debug.Log("character DB is null");
                return;
            }

            if (characterDB.useLootBag)
                DropLootBag();
        }

        /// <summary>
        /// Calls GenerateLootItems to creat loot and spawns the loot bag at the entity's current location.
        /// </summary>
        protected virtual void DropLootBag()
        {
            if (!IsServer)
                return;

            List<CharacterItem> lootItems = GenerateLootItems();

            if (lootItems.Count > 0 || characterDB.dropEmptyBag)
                RPC(CreateLootBag, this.gameObject.transform.position, this.gameObject.transform.rotation, lootItems);
        }

        /// <summary>
        /// Generates the loot items that will be added to the bag on death.
        /// </summary>
        protected virtual List<CharacterItem> GenerateLootItems()
        {
            List<CharacterItem> lootBagItems = new List<CharacterItem>();
            List<ItemDrop> itemDrops = characterDB.GetRandomItems();
            Debug.Log("generated " + itemDrops.Count + " loot items");
            foreach (ItemDrop itemDrop in itemDrops)
            {
                var itemAmount = (short)Random.Range(itemDrop.minAmount <= 0 ? 1 : itemDrop.minAmount, itemDrop.maxAmount);
                lootBagItems.Add(CharacterItem.Create(itemDrop.item, 1, itemAmount));
            }
            return lootBagItems;
        }

        [ServerRpc]
        /// <summary>
        /// Creates the loot bag entity building and configures it according to configuration settings.
        /// </summary>
        /// <param name="position">position in gameworld to spawn loot bag entity</param>
        /// <param name="rotation">rotation to use for lootbag entity</param>
        /// <param name="lootItems">items to place on lootbag</param>
        protected void CreateLootBag(Vector3 position, Quaternion rotation, List<CharacterItem> lootItems)
        {
            BuildingEntity buildingEntity;
            if (!GameInstance.BuildingEntities.TryGetValue(characterDB.lootBagEntity.EntityId, out buildingEntity))
                return;

            if (!(buildingEntity is LootBagEntity))
                return;

            BuildingSaveData bsd = new BuildingSaveData();
            bsd.Id = GenericUtils.GetUniqueId();
            bsd.ParentId = string.Empty;
            bsd.EntityId = buildingEntity.EntityId;
            bsd.CurrentHp = buildingEntity.MaxHp;
            bsd.RemainsLifeTime = buildingEntity.LifeTime;
            bsd.Position = position;
            bsd.Rotation = rotation;
            bsd.CreatorId = Id;
            bsd.CreatorName = CharacterName;
            LootBagEntity lbe = CurrentGameManager.CreateBuildingEntity(bsd, false) as LootBagEntity;

            string ownerName = EntityTitle;
            if (this is BasePlayerCharacterEntity)
                ownerName = CharacterName;

            lbe.SetOwnerName(ownerName);
            lbe.AddItems(lootItems);
        }
    }
}
