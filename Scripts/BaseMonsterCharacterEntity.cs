using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using LiteNetLibManager;
using LiteNetLib;

namespace MultiplayerARPG
{
    public abstract partial class BaseMonsterCharacterEntity : BaseCharacterEntity
    {
        public readonly Dictionary<BaseCharacterEntity, ReceivedDamageRecord> receivedDamageRecords = new Dictionary<BaseCharacterEntity, ReceivedDamageRecord>();

        [Header("Monster Character Settings")]
        [Tooltip("The title which will override `Monster Character`'s title")]
        [SerializeField]
        protected string characterTitle;
        [Tooltip("Character titles by language keys")]
        [SerializeField]
        protected LanguageData[] characterTitles;
        [SerializeField]
        protected MonsterCharacter monsterCharacter;
        [SerializeField]
        protected float destroyDelay = 2f;
        [SerializeField]
        protected float destroyRespawnDelay = 5f;

        [Header("Monster Character Sync Fields")]
        [SerializeField]
        protected SyncFieldUInt summonerObjectId = new SyncFieldUInt();
        [SerializeField]
        protected SyncFieldByte summonType = new SyncFieldByte();

        public string CharacterTitle
        {
            get { return Language.GetText(characterTitles, characterTitle); }
        }

        public override string Title
        {
            get
            {
                // Return title (Can set in prefab) if it is not empty
                if (!string.IsNullOrEmpty(CharacterTitle))
                    return CharacterTitle;
                return !MonsterDatabase || string.IsNullOrEmpty(MonsterDatabase.Title) ? LanguageManager.GetUnknowTitle() : MonsterDatabase.Title;
            }
            set { }
        }

        private BaseCharacterEntity summoner;
        public BaseCharacterEntity Summoner
        {
            get
            {
                if (summoner == null)
                {
                    LiteNetLibIdentity identity;
                    if (Manager.Assets.TryGetSpawnedObject(summonerObjectId.Value, out identity))
                        summoner = identity.GetComponent<BaseCharacterEntity>();
                }
                return summoner;
            }
            protected set
            {
                summoner = value;
                if (IsServer)
                    summonerObjectId.Value = summoner != null ? summoner.ObjectId : 0;
            }
        }

        public SummonType SummonType { get { return (SummonType)summonType.Value; } protected set { summonType.Value = (byte)value; } }
        public bool IsSummoned { get { return SummonType != SummonType.None; } }

        public MonsterSpawnArea SpawnArea { get; protected set; }
        public Vector3 SpawnPosition { get; protected set; }
        public MonsterCharacter MonsterDatabase { get { return monsterCharacter; } }
        public override int DataId { get { return MonsterDatabase.DataId; } set { } }
        public float DestroyDelay { get { return destroyDelay; } }
        public float DestroyRespawnDelay { get { return destroyRespawnDelay; } }
        public bool IsWandering { get; set; }

        private readonly HashSet<uint> looters = new HashSet<uint>();

        protected override void EntityAwake()
        {
            base.EntityAwake();
            gameObject.tag = CurrentGameInstance.monsterTag;
        }

        protected override void EntityUpdate()
        {
            Profiler.BeginSample("BaseMonsterCharacterEntity - Update");
            base.EntityUpdate();
            if (IsSummoned)
            {
                if (Summoner)
                {
                    if (Vector3.Distance(CacheTransform.position, Summoner.CacheTransform.position) > CurrentGameInstance.maxFollowSummonerDistance)
                    {
                        // Teleport to summoner if too far from summoner
                        Teleport(Summoner.GetSummonPosition());
                    }
                }
                else
                {
                    // Summoner disappear so destroy it
                    UnSummon();
                }
            }
            Profiler.EndSample();
        }

        protected void InitStats()
        {
            if (!IsServer)
                return;

            if (Level <= 0)
                Level = MonsterDatabase.defaultLevel;

            CharacterStats stats = this.GetStats();
            CurrentHp = (int)stats.hp;
            CurrentMp = (int)stats.mp;
            CurrentStamina = (int)stats.stamina;
            CurrentFood = (int)stats.food;
            CurrentWater = (int)stats.water;
        }

        public void SetSpawnArea(MonsterSpawnArea spawnArea, Vector3 spawnPosition)
        {
            SpawnArea = spawnArea;
            FindGroundedPosition(spawnPosition, 512f, out spawnPosition);
            SpawnPosition = spawnPosition;
        }

        protected override void SetupNetElements()
        {
            base.SetupNetElements();
            summonerObjectId.deliveryMethod = DeliveryMethod.ReliableOrdered;
            summonerObjectId.syncMode = LiteNetLibSyncField.SyncMode.ServerToClients;
            summonType.deliveryMethod = DeliveryMethod.ReliableOrdered;
            summonType.syncMode = LiteNetLibSyncField.SyncMode.ServerToClients;
        }

        public override void OnSetup()
        {
            // Force set `MovementSecure` to `ServerAuthoritative` for all monsters
            MovementSecure = MovementSecure.ServerAuthoritative;

            base.OnSetup();

            // Setup relates elements
            if (CurrentGameInstance.monsterCharacterMiniMapObjects != null && CurrentGameInstance.monsterCharacterMiniMapObjects.Length > 0)
            {
                foreach (GameObject obj in CurrentGameInstance.monsterCharacterMiniMapObjects)
                {
                    if (obj == null) continue;
                    Instantiate(obj, MiniMapUITransform.position, MiniMapUITransform.rotation, MiniMapUITransform);
                }
            }

            if (CurrentGameInstance.monsterCharacterUI != null)
                InstantiateUI(CurrentGameInstance.monsterCharacterUI);

            InitStats();

            if (SpawnArea == null)
                SpawnPosition = CacheTransform.position;
        }

        public void SetAttackTarget(IDamageableEntity target)
        {
            if (target == null || target.Entity == Entity ||
                target.IsDead() || !target.CanReceiveDamageFrom(this))
                return;
            // Already have target so don't set target
            IDamageableEntity oldTarget;
            if (TryGetTargetEntity(out oldTarget) && !oldTarget.IsDead())
                return;
            // Set target to attack
            SetTargetEntity(target.Entity);
        }

        public override float GetMoveSpeed()
        {
            if (IsWandering)
                return MonsterDatabase.wanderMoveSpeed;
            return base.GetMoveSpeed();
        }
        
        public override void ReceiveDamage(IGameEntity attacker, CharacterItem weapon, Dictionary<DamageElement, MinMaxFloat> damageAmounts, BaseSkill skill, short skillLevel)
        {
            if (!IsServer || IsDead() || !CanReceiveDamageFrom(attacker))
                return;

            base.ReceiveDamage(attacker, weapon, damageAmounts, skill, skillLevel);

            if (attacker != null && attacker.Entity is BaseCharacterEntity)
            {
                BaseCharacterEntity attackerCharacter = attacker.Entity as BaseCharacterEntity;

                // If character is not dead, try to attack
                if (!IsDead())
                {
                    BaseCharacterEntity targetEntity;
                    if (!TryGetTargetEntity(out targetEntity))
                    {
                        // If no target enemy, set target enemy as attacker
                        SetAttackTarget(attackerCharacter);
                    }
                    else if (attackerCharacter != targetEntity && Random.value > 0.5f)
                    {
                        // Random 50% to change target when receive damage from anyone
                        SetAttackTarget(attackerCharacter);
                    }
                }
            }
        }

        public override void GetAttackingData(
            ref bool isLeftHand,
            out AnimActionType animActionType,
            out int animationDataId,
            out CharacterItem weapon)
        {
            // Monster animation always main-hand (right-hand) animation
            isLeftHand = false;
            // Monster animation always main-hand (right-hand) animation
            animActionType = AnimActionType.AttackRightHand;
            // Monster will not have weapon type so set dataId to `0`, then random attack animation from default attack animtions
            animationDataId = 0;
            // Monster will not have weapon data
            weapon = null;
        }

        public override void GetUsingSkillData(
            BaseSkill skill,
            ref bool isLeftHand,
            out AnimActionType animActionType,
            out int animationDataId,
            out CharacterItem weapon)
        {
            // Monster animation always main-hand (right-hand) animation
            isLeftHand = false;
            // Monster animation always main-hand (right-hand) animation
            animActionType = AnimActionType.AttackRightHand;
            // Monster will not have weapon type so set dataId to `0`, then random attack animation from default attack animtions
            animationDataId = 0;
            // Monster will not have weapon data
            weapon = null;
            // Prepare skill data
            if (skill == null)
                return;
            // Get activate animation type which defined at character model
            SkillActivateAnimationType useSkillActivateAnimationType = CharacterModel.UseSkillActivateAnimationType(skill);
            // Prepare animation
            if (useSkillActivateAnimationType == SkillActivateAnimationType.UseAttackAnimation && skill.IsAttack())
            {
                // Assign data id
                animationDataId = 0;
                // Assign animation action type
                animActionType = AnimActionType.AttackRightHand;
            }
            else if (useSkillActivateAnimationType == SkillActivateAnimationType.UseActivateAnimation)
            {
                // Assign data id
                animationDataId = skill.DataId;
                // Assign animation action type
                animActionType = AnimActionType.SkillRightHand;
            }
        }

        public override float GetAttackDistance(bool isLeftHand)
        {
            return MonsterDatabase.DamageInfo.GetDistance();
        }

        public override float GetAttackFov(bool isLeftHand)
        {
            return MonsterDatabase.DamageInfo.GetFov();
        }

        public override void ReceivedDamage(IGameEntity attacker, CombatAmountType damageAmountType, int damage)
        {
            // Attacker can be null when character buff's buff applier is null, So avoid it
            if (attacker != null)
            {
                BaseCharacterEntity attackerCharacter = attacker.Entity as BaseCharacterEntity;

                // If summoned by someone, summoner is attacker
                if (attackerCharacter != null &&
                    attackerCharacter is BaseMonsterCharacterEntity &&
                    (attackerCharacter as BaseMonsterCharacterEntity).IsSummoned)
                    attackerCharacter = (attackerCharacter as BaseMonsterCharacterEntity).Summoner;

                // Add received damage entry
                if (attackerCharacter != null)
                {
                    ReceivedDamageRecord receivedDamageRecord = new ReceivedDamageRecord();
                    receivedDamageRecord.totalReceivedDamage = damage;
                    if (receivedDamageRecords.ContainsKey(attackerCharacter))
                    {
                        receivedDamageRecord = receivedDamageRecords[attackerCharacter];
                        receivedDamageRecord.totalReceivedDamage += damage;
                    }
                    receivedDamageRecord.lastReceivedDamageTime = Time.unscaledTime;
                    receivedDamageRecords[attackerCharacter] = receivedDamageRecord;
                }
            }
            base.ReceivedDamage(attacker, damageAmountType, damage);
        }

        public override sealed void Killed(IGameEntity lastAttacker)
        {
            base.Killed(lastAttacker);

            // If this summoned by someone, don't give reward to killer
            if (IsSummoned)
                return;

            Reward reward = CurrentGameplayRule.MakeMonsterReward(MonsterDatabase);
            // Temp data which will be in-use in loop
            BaseCharacterEntity tempCharacterEntity;
            BasePlayerCharacterEntity tempPlayerCharacterEntity;
            BaseMonsterCharacterEntity tempMonsterCharacterEntity;
            // Last player is last player who kill the monster
            // Whom will have permission to pickup an items before other
            BasePlayerCharacterEntity lastPlayer = null;
            if (lastAttacker != null)
            {
                if (lastAttacker.Entity is BaseMonsterCharacterEntity)
                {
                    tempMonsterCharacterEntity = lastAttacker.Entity as BaseMonsterCharacterEntity;
                    if (tempMonsterCharacterEntity.Summoner != null &&
                        tempMonsterCharacterEntity.Summoner is BasePlayerCharacterEntity)
                    {
                        // Set its summoner as main enemy
                        lastAttacker = tempMonsterCharacterEntity.Summoner;
                    }
                }
                lastPlayer = lastAttacker.Entity as BasePlayerCharacterEntity;
            }
            GuildData tempGuildData;
            PartyData tempPartyData;
            bool givenRewardExp;
            bool givenRewardCurrency;
            float shareGuildExpRate;
            if (receivedDamageRecords.Count > 0)
            {
                float tempHighRewardRate = 0f;
                foreach (BaseCharacterEntity enemy in receivedDamageRecords.Keys)
                {
                    if (enemy == null)
                        continue;

                    tempCharacterEntity = enemy;
                    givenRewardExp = false;
                    givenRewardCurrency = false;
                    shareGuildExpRate = 0f;

                    ReceivedDamageRecord receivedDamageRecord = receivedDamageRecords[tempCharacterEntity];
                    float rewardRate = (float)receivedDamageRecord.totalReceivedDamage / (float)this.GetCaches().MaxHp;
                    if (rewardRate > 1f)
                        rewardRate = 1f;

                    if (tempCharacterEntity is BaseMonsterCharacterEntity)
                    {
                        tempMonsterCharacterEntity = tempCharacterEntity as BaseMonsterCharacterEntity;
                        if (tempMonsterCharacterEntity.Summoner != null &&
                            tempMonsterCharacterEntity.Summoner is BasePlayerCharacterEntity)
                        {
                            // Set its summoner as main enemy
                            tempCharacterEntity = tempMonsterCharacterEntity.Summoner;
                        }
                    }

                    if (tempCharacterEntity is BasePlayerCharacterEntity)
                    {
                        bool makeMostDamage = false;
                        tempPlayerCharacterEntity = tempCharacterEntity as BasePlayerCharacterEntity;
                        // Clear looters list when it is found new player character who make most damages
                        if (rewardRate > tempHighRewardRate)
                        {
                            tempHighRewardRate = rewardRate;
                            looters.Clear();
                            makeMostDamage = true;
                        }
                        // Try find guild data from player character
                        if (tempPlayerCharacterEntity.GuildId > 0 && CurrentGameManager.TryGetGuild(tempPlayerCharacterEntity.GuildId, out tempGuildData))
                        {
                            // Calculation amount of Exp which will be shared to guild
                            shareGuildExpRate = (float)tempGuildData.ShareExpPercentage(tempPlayerCharacterEntity.Id) * 0.01f;
                            // Will share Exp to guild when sharing amount more than 0
                            if (shareGuildExpRate > 0)
                            {
                                // Increase guild exp
                                CurrentGameManager.IncreaseGuildExp(tempPlayerCharacterEntity, (int)(reward.exp * shareGuildExpRate * rewardRate));
                            }
                        }
                        // Try find party data from player character
                        if (tempPlayerCharacterEntity.PartyId > 0 && CurrentGameManager.TryGetParty(tempPlayerCharacterEntity.PartyId, out tempPartyData))
                        {
                            BasePlayerCharacterEntity partyPlayerCharacterEntity;
                            // Loop party member to fill looter list / increase gold / increase exp
                            foreach (SocialCharacterData member in tempPartyData.GetMembers())
                            {
                                if (CurrentGameManager.TryGetPlayerCharacterById(member.id, out partyPlayerCharacterEntity))
                                {
                                    // If share exp, every party member will receive devided exp
                                    // If not share exp, character who make damage will receive non-devided exp
                                    if (tempPartyData.shareExp)
                                        partyPlayerCharacterEntity.RewardExp(reward, (1f - shareGuildExpRate) / (float)tempPartyData.CountMember() * rewardRate, RewardGivenType.PartyShare);

                                    // If share item, every party member will receive devided gold
                                    // If not share item, character who make damage will receive non-devided gold
                                    if (tempPartyData.shareItem)
                                    {
                                        if (makeMostDamage)
                                        {
                                            // Make other member in party able to pickup items
                                            looters.Add(partyPlayerCharacterEntity.ObjectId);
                                        }
                                        partyPlayerCharacterEntity.RewardCurrencies(reward, 1f / (float)tempPartyData.CountMember() * rewardRate, RewardGivenType.PartyShare);
                                    }
                                }
                            }
                            // Shared exp has been given, so do not give it to character again
                            if (tempPartyData.shareExp)
                                givenRewardExp = true;
                            // Shared gold has been given, so do not give it to character again
                            if (tempPartyData.shareItem)
                                givenRewardCurrency = true;
                        }

                        // Add reward to current character in damage record list
                        if (!givenRewardExp)
                        {
                            // Will give reward when it was not given
                            int petIndex = tempPlayerCharacterEntity.IndexOfSummon(SummonType.Pet);
                            if (petIndex >= 0)
                            {
                                tempMonsterCharacterEntity = tempPlayerCharacterEntity.Summons[petIndex].CacheEntity;
                                if (tempMonsterCharacterEntity != null)
                                {
                                    // Share exp to pet, set multiplier to 0.5, because it will be shared to player
                                    tempMonsterCharacterEntity.RewardExp(reward, (1f - shareGuildExpRate) * 0.5f * rewardRate, RewardGivenType.KillMonster);
                                }
                                // Set multiplier to 0.5, because it was shared to monster
                                tempPlayerCharacterEntity.RewardExp(reward, (1f - shareGuildExpRate) * 0.5f * rewardRate, RewardGivenType.KillMonster);
                            }
                            else
                            {
                                // No pet, no share, so rate is 1f
                                tempPlayerCharacterEntity.RewardExp(reward, (1f - shareGuildExpRate) * rewardRate, RewardGivenType.KillMonster);
                            }
                        }

                        if (!givenRewardCurrency)
                        {
                            // Will give reward when it was not given
                            tempPlayerCharacterEntity.RewardCurrencies(reward, rewardRate, RewardGivenType.KillMonster);
                        }

                        if (makeMostDamage)
                        {
                            // Make current character able to pick up item because it made most damage
                            looters.Add(tempPlayerCharacterEntity.ObjectId);
                        }
                    }   // End is `BasePlayerCharacterEntity` condition
                }   // End for-loop
            }   // End count recived damage record count
            receivedDamageRecords.Clear();

            // Generate loot to drop on ground or fill loot bag
            List<ItemDrop> itemDrops = MonsterDatabase.GetRandomItems();
            List<CharacterItem> lootBagItems = new List<CharacterItem>();
            foreach (ItemDrop itemDrop in itemDrops)
            {
                if (useLootBag)
                    lootBagItems.Add(CharacterItem.Create(itemDrop.item, 1, itemDrop.amount));
                if (!useLootBag)
                    OnRandomDropItem(itemDrop.item, itemDrop.amount);
            }
            LootBag = lootBagItems;

            // Clear looters because they are already set to dropped items
            looters.Clear();

            if (lastPlayer != null)
            {
                // Increase kill progress
                lastPlayer.OnKillMonster(this);
            }

            if (!IsSummoned)
            {
                // If not summoned by someone, destroy and respawn it
                DestroyAndRespawn();
            }
        }

        private void OnRandomDropItem(BaseItem item, short amount)
        {
            // Drop item to the ground
            if (amount > item.MaxStack)
                amount = item.MaxStack;
            ItemDropEntity.DropItem(this, CharacterItem.Create(item, 1, amount), looters);
        }

        public override void Respawn()
        {
            if (!IsServer || !IsDead())
                return;

            base.Respawn();
            StopMove();
            Teleport(SpawnPosition);
        }

        public void DestroyAndRespawn()
        {
            if (!IsServer)
                return;

            if (SpawnArea != null)
                SpawnArea.Spawn(DestroyDelay + DestroyRespawnDelay);
            else
                Manager.StartCoroutine(RespawnRoutine());

            NetworkDestroy(DestroyDelay);
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSecondsRealtime(DestroyDelay + DestroyRespawnDelay);
            ClearLootBag();
            InitStats();
            Manager.Assets.NetworkSpawnScene(
                Identity.ObjectId,
                SpawnPosition,
                CurrentGameInstance.DimensionType == DimensionType.Dimension3D ? Quaternion.Euler(Vector3.up * Random.Range(0, 360)) : Quaternion.identity);
        }

        public void Summon(BaseCharacterEntity summoner, SummonType summonType, short level)
        {
            Summoner = summoner;
            SummonType = summonType;
            Level = level;
            InitStats();
        }

        public void UnSummon()
        {
            // TODO: May play teleport effects
            NetworkDestroy();
        }

        protected override void NotifyEnemySpottedToAllies(BaseCharacterEntity enemy)
        {
            if (MonsterDatabase.characteristic != MonsterCharacteristic.Assist)
                return;
            // Warn that this character received damage to nearby characters
            List<BaseCharacterEntity> foundCharacters = FindAliveCharacters<BaseCharacterEntity>(MonsterDatabase.visualRange, true, false, false);
            if (foundCharacters == null || foundCharacters.Count == 0) return;
            foreach (BaseCharacterEntity foundCharacter in foundCharacters)
            {
                foundCharacter.NotifyEnemySpotted(this, enemy);
            }
        }

        public override void NotifyEnemySpotted(BaseCharacterEntity ally, BaseCharacterEntity attacker)
        {
            if ((Summoner && Summoner == ally) ||
                MonsterDatabase.characteristic == MonsterCharacteristic.Assist)
                SetAttackTarget(attacker);
        }

        public override float GetMoveSpeedRateWhileAttackOrUseSkill(AnimActionType animActionType, BaseSkill skill)
        {
            switch (animActionType)
            {
                case AnimActionType.AttackRightHand:
                case AnimActionType.AttackLeftHand:
                    return MonsterDatabase.MoveSpeedRateWhileAttacking;
                case AnimActionType.SkillRightHand:
                case AnimActionType.SkillLeftHand:
                    // Calculate move speed rate while doing action at clients and server
                    if (skill != null)
                        return skill.moveSpeedRateWhileUsingSkill;
                    break;
            }
            return 1f;
        }
    }

    public struct ReceivedDamageRecord
    {
        public float lastReceivedDamageTime;
        public int totalReceivedDamage;
    }
}
