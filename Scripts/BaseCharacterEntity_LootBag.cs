using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public abstract partial class BaseCharacterEntity : DamageableEntity, ICharacterData
    {
        protected BaseCharacter characterDB;
        protected LootBagEntity lootBagEntity;

        private const string visibleLootBagName = "LootBagEntityVisible";
        private const string invisibleLootBagName = "LootBagEntityInvisible";

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
            if (IsServer && this.IsDead())
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

            // Determine which loot bag entity to use based on character DB.
            switch (characterDB.lootBagEntity)
            {
                case LootBagEntitySelection.Visible:
                    if (GameInstance.Singleton.LootBagEntities.ContainsKey(visibleLootBagName))
                        lootBagEntity = GameInstance.Singleton.LootBagEntities[visibleLootBagName];
                    break;
                case LootBagEntitySelection.Invisible:
                    if (GameInstance.Singleton.LootBagEntities.ContainsKey(invisibleLootBagName))
                        lootBagEntity = GameInstance.Singleton.LootBagEntities[invisibleLootBagName];
                    break;
                case LootBagEntitySelection.Override:
                    lootBagEntity = characterDB.lootBagEntityOverride;
                    break;
            }

            DropLootBag();

            // If character is a monster, set body destroy delay according to character DB settings.
            BaseMonsterCharacterEntity bmce = this as BaseMonsterCharacterEntity;
            if (bmce != null && characterDB is MonsterCharacter)
            {
                var monsterDB = characterDB as MonsterCharacter;
                if (monsterDB != null && monsterDB.syncDestroyDelayWithBody)
                    bmce.SetDestroyDelay(monsterDB.lootBagDestroyDelay);
            }
        }

        /// <summary>
        /// Calls GenerateLootItems to creat loot and spawns the loot bag at the entity's current location.
        /// </summary>
        protected virtual void DropLootBag()
        {
            List<CharacterItem> lootItems = GenerateLootItems();

            if (lootItems.Count > 0 || characterDB.dropEmptyBag)
                RPC(CreateLootBag, this.gameObject.transform.position, this.gameObject.transform.rotation, lootItems);
        }

        /// <summary>
        /// Generates the loot items that will be added to the bag on death.
        /// </summary>
        protected virtual List<CharacterItem> GenerateLootItems()
        {
            return characterDB.GetRandomItems();
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
            if (!GameInstance.BuildingEntities.TryGetValue(lootBagEntity.EntityId, out buildingEntity))
                return;

            if (!(buildingEntity is LootBagEntity))
                return;

            string creatorName = EntityTitle;
            if (this is BasePlayerCharacterEntity)
                creatorName = CharacterName;

            BuildingSaveData bsd = new BuildingSaveData();
            bsd.Id = GenericUtils.GetUniqueId();
            bsd.ParentId = string.Empty;
            bsd.EntityId = buildingEntity.EntityId;
            bsd.CurrentHp = buildingEntity.MaxHp;
            bsd.RemainsLifeTime = buildingEntity.LifeTime;
            bsd.Position = position;
            bsd.Rotation = rotation;
            bsd.CreatorId = Id;
            bsd.CreatorName = creatorName;
            LootBagEntity lbe = CurrentGameManager.CreateBuildingEntity(bsd, false) as LootBagEntity;

            lbe.SetOwnerEntity(this);
            lbe.AddItems(lootItems).Forget();
            lbe.SetDestroyDelay(characterDB.lootBagDestroyDelay);
        }
    }
}
