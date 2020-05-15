using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class PlayerCharacterController
    {
        public virtual void UpdateInput()
        {
            bool isFocusInputField = GenericUtils.IsFocusInputField();
            bool isPointerOverUIObject = CacheUISceneGameplay.IsPointerOverUIObject();
            if (CacheGameplayCameraControls != null)
            {
                CacheGameplayCameraControls.updateRotationX = false;
                CacheGameplayCameraControls.updateRotationY = false;
                CacheGameplayCameraControls.updateRotation = !isFocusInputField && !isPointerOverUIObject && InputManager.GetButton("CameraRotate");
                CacheGameplayCameraControls.updateZoom = !isFocusInputField && !isPointerOverUIObject;
            }

            if (isFocusInputField)
                return;

            if (PlayerCharacterEntity.IsDead())
                return;

            // If it's building something, don't allow to activate NPC/Warp/Pickup Item
            if (ConstructingBuildingEntity == null)
            {
                // Activate nearby npcs / players / activable buildings
                if (InputManager.GetButtonDown("Activate"))
                {
                    targetPlayer = null;
                    if (ActivatableEntityDetector.players.Count > 0)
                        targetPlayer = ActivatableEntityDetector.players[0];
                    targetNpc = null;
                    if (ActivatableEntityDetector.npcs.Count > 0)
                        targetNpc = ActivatableEntityDetector.npcs[0];
                    targetBuilding = null;
                    if (ActivatableEntityDetector.buildings.Count > 0)
                        targetBuilding = ActivatableEntityDetector.buildings[0];
                    targetVehicle = null;
                    if (ActivatableEntityDetector.vehicles.Count > 0)
                        targetVehicle = ActivatableEntityDetector.vehicles[0];
                    // Priority Player -> Npc -> Buildings
                    if (targetPlayer != null && CacheUISceneGameplay != null)
                    {
                        // Show dealing, invitation menu
                        SelectedEntity = targetPlayer;
                        CacheUISceneGameplay.SetActivePlayerCharacter(targetPlayer);
                    }
                    else if (targetNpc != null)
                    {
                        // Talk to NPC
                        SelectedEntity = targetNpc;
                        PlayerCharacterEntity.RequestNpcActivate(targetNpc.ObjectId);
                    }
                    else if (targetBuilding != null)
                    {
                        // Use building
                        SelectedEntity = targetBuilding;
                        ActivateBuilding(targetBuilding);
                    }
                    else if (targetVehicle != null)
                    {
                        // Enter vehicle
                        PlayerCharacterEntity.RequestEnterVehicle(targetVehicle.ObjectId);
                    }
                    else
                    {
                        // Enter warp, For some warp portals that `warpImmediatelyWhenEnter` is FALSE
                        PlayerCharacterEntity.RequestEnterWarp();
                    }
                }
                // Pick up nearby items
                if (InputManager.GetButtonDown("PickUpItem"))
                {
                    targetItemDrop = null;
                    if (ItemDropEntityDetector.itemDrops.Count > 0)
                        targetItemDrop = ItemDropEntityDetector.itemDrops[0];
                    if (targetItemDrop != null)
                        PlayerCharacterEntity.RequestPickupItem(targetItemDrop.ObjectId);
                }
                // Loot items from nearest lootable enemy
                if (InputManager.GetButtonDown("Loot"))
                {
                    if (EnemyEntityDetector.characters.Count > 0)
                    {
                        foreach (BaseCharacterEntity characterEntity in EnemyEntityDetector.characters)
                        {
                            if (characterEntity.IsDead() && characterEntity.useLootBag && characterEntity.LootBag.Count > 0)
                            {
                                SetTarget(characterEntity, TargetActionType.Loot);
                                (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(characterEntity);
                                break;
                            }
                        }
                    }
                }
                // Reload
                if (InputManager.GetButtonDown("Reload"))
                {
                    // Reload ammo when press the button
                    ReloadAmmo();
                }
                // Find target to attack
                if (InputManager.GetButtonDown("FindEnemy"))
                {
                    ++findingEnemyIndex;
                    if (findingEnemyIndex < 0 || findingEnemyIndex >= EnemyEntityDetector.characters.Count)
                        findingEnemyIndex = 0;
                    if (EnemyEntityDetector.characters.Count > 0)
                    {
                        SetTarget(null, TargetActionType.Attack);
                        if (!EnemyEntityDetector.characters[findingEnemyIndex].IsHideOrDead)
                        {
                            SetTarget(EnemyEntityDetector.characters[findingEnemyIndex], TargetActionType.Attack);
                            if (SelectedEntity != null)
                            {
                                // Turn character to enemy but does not move or attack yet.
                                TurnCharacterToEntity(SelectedEntity);
                            }
                        }
                    }
                }
                if (InputManager.GetButtonDown("ExitVehicle"))
                {
                    // Exit vehicle
                    PlayerCharacterEntity.RequestExitVehicle();
                }
                if (InputManager.GetButtonDown("SwitchEquipWeaponSet"))
                {
                    // Switch equip weapon set
                    PlayerCharacterEntity.RequestSwitchEquipWeaponSet((byte)(PlayerCharacterEntity.EquipWeaponSet + 1));
                }
                if (InputManager.GetButtonDown("Sprint"))
                {
                    // Toggles sprint state
                    isSprinting = !isSprinting;
                }
                // Auto reload
                if (PlayerCharacterEntity.EquipWeapons.rightHand.IsAmmoEmpty() ||
                    PlayerCharacterEntity.EquipWeapons.leftHand.IsAmmoEmpty())
                {
                    // Reload ammo when empty and not press any keys
                    ReloadAmmo();
                }
            }
            // Update enemy detecting radius to attack distance
            EnemyEntityDetector.detectingRadius = Mathf.Max(PlayerCharacterEntity.GetAttackDistance(false), lockAttackTargetDistance);
            // Update inputs
            UpdateQueuedSkill();
            UpdatePointClickInput();
            UpdateWASDInput();
            // Set sprinting state
            PlayerCharacterEntity.SetExtraMovement(isSprinting ? ExtraMovementState.IsSprinting : ExtraMovementState.None);
        }

        protected void ReloadAmmo()
        {
            // Reload ammo at server
            if (!PlayerCharacterEntity.EquipWeapons.rightHand.IsAmmoFull())
                PlayerCharacterEntity.RequestReload(false);
            else if (!PlayerCharacterEntity.EquipWeapons.leftHand.IsAmmoFull())
                PlayerCharacterEntity.RequestReload(true);
        }

        public virtual void UpdatePointClickInput()
        {
            if (controllerMode == PlayerCharacterControllerMode.WASD)
                return;

            // If it's building something, not allow point click movement
            if (ConstructingBuildingEntity != null)
                return;

            // If it's aiming skills, not allow point click movement
            if (UICharacterHotkeys.UsingHotkey != null)
                return;

            getMouseDown = Input.GetMouseButtonDown(0);
            getMouseUp = Input.GetMouseButtonUp(0);
            getMouse = Input.GetMouseButton(0);

            if (getMouseDown)
            {
                isMouseDragOrHoldOrOverUI = false;
                mouseDownTime = Time.unscaledTime;
                mouseDownPosition = Input.mousePosition;
            }
            // Read inputs
            isPointerOverUI = CacheUISceneGameplay != null && CacheUISceneGameplay.IsPointerOverUIObject();
            isMouseDragDetected = (Input.mousePosition - mouseDownPosition).sqrMagnitude > DETECT_MOUSE_DRAG_DISTANCE_SQUARED;
            isMouseHoldDetected = Time.unscaledTime - mouseDownTime > DETECT_MOUSE_HOLD_DURATION;
            isMouseHoldAndNotDrag = !isMouseDragDetected && isMouseHoldDetected;
            if (!isMouseDragOrHoldOrOverUI && (isMouseDragDetected || isMouseHoldDetected || isPointerOverUI))
            {
                // Detected mouse dragging or hold on an UIs
                isMouseDragOrHoldOrOverUI = true;
            }
            // Will set move target when pointer isn't point on an UIs 
            if (!isPointerOverUI && (getMouse || getMouseUp))
            {
                // Clear target
                ClearTarget(true);
                // Prepare temp variables
                Transform tempTransform;
                Vector3 tempVector3;
                bool tempHasMapPosition = false;
                Vector3 tempMapPosition = Vector3.zero;
                float tempHighestY = float.MinValue;
                BuildingMaterial tempBuildingMaterial;
                // If mouse up while cursor point to target (character, item, npc and so on)
                bool mouseUpOnTarget = getMouseUp && !isMouseDragOrHoldOrOverUI;
                int tempCount = FindClickObjects(out tempVector3);
                for (int tempCounter = tempCount - 1; tempCounter >= 0; --tempCounter)
                {
                    tempTransform = GetRaycastTransform(tempCounter);
                    // When holding on target, or already enter edit building mode
                    if (isMouseHoldAndNotDrag)
                    {
                        targetBuilding = null;
                        tempBuildingMaterial = tempTransform.GetComponent<BuildingMaterial>();
                        if (tempBuildingMaterial != null && tempBuildingMaterial.TargetEntity != null)
                            targetBuilding = tempBuildingMaterial.TargetEntity;
                        if (targetBuilding && !targetBuilding.IsDead())
                        {
                            SetTarget(targetBuilding, TargetActionType.ViewOptions);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                    }
                    else if (mouseUpOnTarget)
                    {
                        targetCharacterEntity = tempTransform.GetComponent <BaseCharacterEntity>();
                        targetPlayer = tempTransform.GetComponent<BasePlayerCharacterEntity>();
                        targetMonster = tempTransform.GetComponent<BaseMonsterCharacterEntity>();
                        targetNpc = tempTransform.GetComponent<NpcEntity>();
                        targetItemDrop = tempTransform.GetComponent<ItemDropEntity>();
                        targetHarvestable = tempTransform.GetComponent<HarvestableEntity>();
                        targetBuilding = null;
                        tempBuildingMaterial = tempTransform.GetComponent<BuildingMaterial>();
                        if (tempBuildingMaterial != null && tempBuildingMaterial.TargetEntity != null)
                            targetBuilding = tempBuildingMaterial.TargetEntity;
                        targetVehicle = tempTransform.GetComponent<VehicleEntity>();
                        lastNpcObjectId = 0;
                        if (targetCharacterEntity && targetCharacterEntity.IsDead() && targetCharacterEntity.useLootBag)
                        {
                            // Found activating entity as lootable character entity
                            SetTarget(targetCharacterEntity, TargetActionType.Loot);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        if (targetPlayer && !targetPlayer.IsHideOrDead)
                        {
                            // Found activating entity as player character entity
                            SetTarget(targetPlayer, TargetActionType.Attack);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetMonster && !targetMonster.IsHideOrDead)
                        {
                            // Found activating entity as monster character entity
                            SetTarget(targetMonster, TargetActionType.Attack);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetNpc)
                        {
                            // Found activating entity as npc entity
                            SetTarget(targetNpc, TargetActionType.Activate);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetItemDrop)
                        {
                            // Found activating entity as item drop entity
                            SetTarget(targetItemDrop, TargetActionType.Activate);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetHarvestable && !targetHarvestable.IsDead())
                        {
                            // Found activating entity as harvestable entity
                            SetTarget(targetHarvestable, TargetActionType.Attack);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetBuilding && !targetBuilding.IsDead() && targetBuilding.Activatable)
                        {
                            // Found activating entity as building entity
                            SetTarget(targetBuilding, TargetActionType.Activate);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetVehicle)
                        {
                            // Found activating entity as vehicle entity
                            SetTarget(targetVehicle, TargetActionType.Activate);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (!GetRaycastIsTrigger(tempCounter))
                        {
                            // Set clicked map position, it will be used if no activating entity found
                            tempHasMapPosition = true;
                            tempMapPosition = GetRaycastPoint(tempCounter);
                            if (tempMapPosition.y > tempHighestY)
                                tempHighestY = tempMapPosition.y;
                        }
                    } // End mouseUpOnTarget
                }
                // When clicked on map (Not touch any game entity)
                // - Clear selected target to hide selected entity UIs
                // - Set target position to position where mouse clicked
                if (tempHasMapPosition)
                {
                    SelectedEntity = null;
                    targetPosition = tempMapPosition;
                }
                // When clicked on map (any non-collider position)
                // tempVector3 is come from FindClickObjects()
                // - Clear character target to make character stop doing actions
                // - Clear selected target to hide selected entity UIs
                // - Set target position to position where mouse clicked
                if (CurrentGameInstance.DimensionType == DimensionType.Dimension2D && mouseUpOnTarget && tempCount == 0)
                {
                    ClearTarget();
                    tempVector3.z = 0;
                    targetPosition = tempVector3;
                }

                // Found ground position
                if (targetPosition.HasValue)
                {
                    // Close NPC dialog, when target changes
                    HideNpcDialog();
                    ClearQueueUsingSkill();
                    isFollowingTarget = false;
                    if (!PlayerCharacterEntity.IsPlayingActionAnimation())
                    {
                        destination = targetPosition.Value;
                        PlayerCharacterEntity.PointClickMovement(targetPosition.Value);
                    }
                }
            }
        }

        protected virtual void SetTarget(BaseGameEntity entity, TargetActionType targetActionType, bool checkControllerMode = true)
        {
            targetPosition = null;
            if (checkControllerMode && controllerMode == PlayerCharacterControllerMode.WASD)
            {
                this.targetActionType = targetActionType;
                destination = null;
                SelectedEntity = entity;
                return;
            }
            if (pointClickSetTargetImmediately ||
                (entity != null && SelectedEntity == entity) ||
                (entity != null && entity is ItemDropEntity))
            {
                this.targetActionType = targetActionType;
                destination = null;
                TargetEntity = entity;
                PlayerCharacterEntity.SetTargetEntity(entity);
            }
            SelectedEntity = entity;
        }

        protected virtual void ClearTarget(bool exceptSelectedTarget = false)
        {
            if (!exceptSelectedTarget)
                SelectedEntity = null;
            TargetEntity = null;
            PlayerCharacterEntity.SetTargetEntity(null);
            targetPosition = null;
            targetActionType = TargetActionType.Activate;
        }

        public override void DeselectBuilding()
        {
            base.DeselectBuilding();
            ClearTarget();
        }

        public virtual void UpdateWASDInput()
        {
            if (controllerMode == PlayerCharacterControllerMode.PointClick)
                return;

            // If mobile platforms, don't receive input raw to make it smooth
            bool raw = !InputManager.useMobileInputOnNonMobile && !Application.isMobilePlatform;
            Vector3 moveDirection = GetMoveDirection(InputManager.GetAxis("Horizontal", raw), InputManager.GetAxis("Vertical", raw));
            moveDirection.Normalize();

            // Move
            if (moveDirection.sqrMagnitude > 0f)
            {
                HideNpcDialog();
                ClearQueueUsingSkill();
                destination = null;
                isFollowingTarget = false;
                if (TargetEntity != null && Vector3.Distance(MovementTransform.position, TargetEntity.CacheTransform.position) >= wasdClearTargetDistance)
                {
                    // Clear target when character moved far from target
                    ClearTarget();
                }
                if (!PlayerCharacterEntity.IsPlayingActionAnimation())
                    PlayerCharacterEntity.SetLookRotation(Quaternion.LookRotation(moveDirection));
            }

            // Attack when player pressed attack button
            if (InputManager.GetButton("Attack"))
                UpdateWASDAttack();

            // Always forward
            MovementState movementState = MovementState.Forward;
            if (InputManager.GetButtonDown("Jump"))
                movementState |= MovementState.IsJump;
            PlayerCharacterEntity.KeyMovement(moveDirection, movementState);
        }

        protected void UpdateWASDAttack()
        {
            destination = null;
            BaseCharacterEntity targetEntity;

            if (TryGetSelectedTargetAsAttackingEntity(out targetEntity))
                SetTarget(targetEntity, TargetActionType.Attack, false);

            if (wasdLockAttackTarget)
            {
                if (!TryGetAttackingEntity(out targetEntity) || targetEntity.IsHideOrDead)
                {
                    // Find nearest target and move to the target
                    targetEntity = PlayerCharacterEntity
                        .FindNearestAliveCharacter<BaseCharacterEntity>(
                        Mathf.Max(PlayerCharacterEntity.GetAttackDistance(isLeftHandAttacking), lockAttackTargetDistance),
                        false,
                        true,
                        false);
                }
                if (targetEntity != null && !targetEntity.IsHideOrDead)
                {
                    // Set target, then attack later when moved nearby target
                    SelectedEntity = targetEntity;
                    SetTarget(targetEntity, TargetActionType.Attack, false);
                    isFollowingTarget = true;
                }
                else
                {
                    // No nearby target, so attack immediately
                    if (PlayerCharacterEntity.RequestAttack(isLeftHandAttacking))
                        isLeftHandAttacking = !isLeftHandAttacking;
                    isFollowingTarget = false;
                }
            }
            else if (!wasdLockAttackTarget)
            {
                // Find nearest target and set selected target to show character hp/mp UIs
                SelectedEntity = PlayerCharacterEntity
                    .FindNearestAliveCharacter<BaseCharacterEntity>(
                    PlayerCharacterEntity.GetAttackDistance(isLeftHandAttacking),
                    false,
                    true,
                    false);
                if (SelectedEntity != null)
                {
                    // Look at target and attack
                    TurnCharacterToEntity(SelectedEntity);
                }
                // Not lock target, so not finding target and attack immediately
                if (PlayerCharacterEntity.RequestAttack(isLeftHandAttacking))
                    isLeftHandAttacking = !isLeftHandAttacking;
                isFollowingTarget = false;
            }
        }

        protected void UpdateQueuedSkill()
        {
            if (PlayerCharacterEntity.IsHideOrDead)
            {
                ClearQueueUsingSkill();
                return;
            }
            if (queueUsingSkill.skill == null || queueUsingSkill.level <= 0)
                return;
            if (PlayerCharacterEntity.IsPlayingActionAnimation())
                return;
            destination = null;
            BaseSkill skill = queueUsingSkill.skill;
            short skillLevel = queueUsingSkill.level;
            Vector3? aimPosition = queueUsingSkill.aimPosition;
            BaseCharacterEntity targetEntity;
            bool wasdLockAttackTarget = this.wasdLockAttackTarget || controllerMode == PlayerCharacterControllerMode.PointClick;

            if (skill.HasCustomAimControls())
            {
                // Target not required, use skill immediately
                TurnCharacterToPosition(aimPosition.Value);
                RequestUsePendingSkill();
                isFollowingTarget = false;
                return;
            }

            if (skill.IsAttack())
            {
                if (TryGetSelectedTargetAsAttackingEntity(out targetEntity))
                    SetTarget(targetEntity, TargetActionType.UseSkill, false);

                if (wasdLockAttackTarget)
                {
                    if (!TryGetAttackingEntity(out targetEntity) || targetEntity.IsHideOrDead)
                    {
                        targetEntity = PlayerCharacterEntity
                            .FindNearestAliveCharacter<BaseCharacterEntity>(
                            Mathf.Max(skill.GetCastDistance(PlayerCharacterEntity, skillLevel, isLeftHandAttacking), lockAttackTargetDistance),
                            false,
                            true,
                            false);
                    }
                    if (targetEntity != null && !targetEntity.IsHideOrDead)
                    {
                        // Set target, then use skill later when moved nearby target
                        SelectedEntity = targetEntity;
                        SetTarget(targetEntity, TargetActionType.UseSkill, false);
                        isFollowingTarget = true;
                    }
                    else
                    {
                        // No target, so use skill immediately
                        RequestUsePendingSkill();
                        isFollowingTarget = false;
                    }
                }
                else if (!wasdLockAttackTarget)
                {
                    // Find nearest target and set selected target to show character hp/mp UIs
                    SelectedEntity = PlayerCharacterEntity
                        .FindNearestAliveCharacter<BaseCharacterEntity>(
                        skill.GetCastDistance(PlayerCharacterEntity, skillLevel, isLeftHandAttacking),
                        false,
                        true,
                        false);
                    if (SelectedEntity != null)
                    {
                        // Look at target and attack
                        TurnCharacterToEntity(SelectedEntity);
                    }
                    // Not lock target, so not finding target and use skill immediately
                    RequestUsePendingSkill();
                    isFollowingTarget = false;
                }
            }
            else
            {
                // Not attack skill, so use skill immediately
                if (skill.RequiredTarget())
                {
                    if (wasdLockAttackTarget)
                    {
                        // Set target, then use skill later when moved nearby target
                        if (SelectedEntity != null && SelectedEntity is BaseCharacterEntity)
                        {
                            SetTarget(SelectedEntity, TargetActionType.UseSkill, false);
                            isFollowingTarget = true;
                        }
                        else
                        {
                            ClearQueueUsingSkill();
                            isFollowingTarget = false;
                        }
                    }
                    else
                    {
                        // Try apply skill to selected entity immediately, it will fail if selected entity is far from the character
                        if (SelectedEntity != null && SelectedEntity is BaseCharacterEntity)
                        {
                            if (SelectedEntity != PlayerCharacterEntity)
                            {
                                // Look at target and use skill
                                TurnCharacterToEntity(SelectedEntity);
                            }
                            RequestUsePendingSkill();
                            isFollowingTarget = false;
                        }
                        else
                        {
                            ClearQueueUsingSkill();
                            isFollowingTarget = false;
                        }
                    }
                }
                else
                {
                    // Target not required, use skill immediately
                    RequestUsePendingSkill();
                    isFollowingTarget = false;
                }
            }
        }

        public void UpdateFollowTarget()
        {
            if (!isFollowingTarget)
                return;

            if (TryGetLootableCharacter(out targetCharacterEntity))
            {
                DoActionOrMoveToEntity(targetCharacterEntity, CurrentGameInstance.conversationDistance, () =>
                {
                    if (!InputManager.useMobileInputOnNonMobile && !Application.isMobilePlatform)
                        (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(targetCharacterEntity);
                });
            }
            else if (TryGetAttackingEntity(out targetDamageable))
            {
                if (EntityIsHideOrDead(targetDamageable))
                {
                    ClearQueueUsingSkill();
                    PlayerCharacterEntity.StopMove();
                    ClearTarget();
                    return;
                }
                float attackDistance = 0f;
                float attackFov = 0f;
                GetAttackDistanceAndFov(isLeftHandAttacking, out attackDistance, out attackFov);
                AttackOrMoveToEntity(targetDamageable, attackDistance, CurrentGameInstance.characterLayer.Mask);
            }
            else if (TryGetUsingSkillEntity(out targetDamageable))
            {
                if (queueUsingSkill.skill.IsAttack() && EntityIsHideOrDead(targetDamageable))
                {
                    ClearQueueUsingSkill();
                    PlayerCharacterEntity.StopMove();
                    ClearTarget();
                    return;
                }
                float castDistance = 0f;
                float castFov = 0f;
                GetUseSkillDistanceAndFov(out castDistance, out castFov);
                UseSkillOrMoveToEntity(targetDamageable, castDistance);
            }
            else if (TryGetDoActionEntity(out targetPlayer))
            {
                DoActionOrMoveToEntity(targetPlayer, CurrentGameInstance.conversationDistance, () =>
                {
                    // TODO: Do something
                });
            }
            else if (TryGetDoActionEntity(out targetNpc))
            {
                DoActionOrMoveToEntity(targetNpc, CurrentGameInstance.conversationDistance, () =>
                {
                    if (lastNpcObjectId != targetNpc.ObjectId)
                    {
                        PlayerCharacterEntity.RequestNpcActivate(targetNpc.ObjectId);
                        lastNpcObjectId = targetNpc.ObjectId;
                    }
                });
            }
            else if (TryGetDoActionEntity(out targetItemDrop))
            {
                DoActionOrMoveToEntity(targetItemDrop, CurrentGameInstance.pickUpItemDistance, () =>
                {
                    PlayerCharacterEntity.RequestPickupItem(targetItemDrop.ObjectId);
                    ClearTarget();
                });
            }
            else if (TryGetDoActionEntity(out targetBuilding, TargetActionType.Activate))
            {
                DoActionOrMoveToEntity(targetBuilding, CurrentGameInstance.conversationDistance, () =>
                {
                    ActivateBuilding(targetBuilding);
                });
            }
            else if (TryGetDoActionEntity(out targetBuilding, TargetActionType.ViewOptions))
            {
                DoActionOrMoveToEntity(targetBuilding, CurrentGameInstance.conversationDistance, () =>
                {
                    ShowCurrentBuildingDialog();
                });
            }
            else if (TryGetDoActionEntity(out targetVehicle))
            {
                DoActionOrMoveToEntity(targetVehicle, CurrentGameInstance.conversationDistance, () =>
                {
                    PlayerCharacterEntity.RequestEnterVehicle(targetVehicle.ObjectId);
                    ClearTarget();
                });
            }
        }

        protected void DoActionOrMoveToEntity(BaseGameEntity entity, float distance, System.Action action)
        {
            Vector3 measuringPosition = MovementTransform.position;
            Vector3 targetPosition = entity.CacheTransform.position;
            if (Vector3.Distance(measuringPosition, targetPosition) <= distance)
            {
                // Stop movement to do action
                PlayerCharacterEntity.StopMove();
                // Do action
                action.Invoke();
                // This function may be used by extending classes
                OnDoActionOnEntity();
            }
            else
            {
                // Move to target entity
                UpdateTargetEntityPosition(measuringPosition, targetPosition, distance);
            }
        }

        protected virtual void OnDoActionOnEntity()
        {

        }

        protected void AttackOrMoveToEntity(IDamageableEntity entity, float distance, int layerMask)
        {
            Transform damageTransform = PlayerCharacterEntity.GetWeaponDamageInfo(ref isLeftHandAttacking).GetDamageTransform(PlayerCharacterEntity, isLeftHandAttacking);
            Vector3 measuringPosition = damageTransform.position;
            Vector3 targetPosition = entity.OpponentAimTransform.position;
            if (Vector3.Distance(measuringPosition, targetPosition) <= distance)
            {
                // Stop movement to attack
                PlayerCharacterEntity.StopMove();
                // Turn character to attacking target
                TurnCharacterToEntity(entity.Entity);
                // Do action
                RequestAttack();
                // This function may be used by extending classes
                OnAttackOnEntity();
            }
            else
            {
                // Move to target entity
                UpdateTargetEntityPosition(measuringPosition, targetPosition, distance);
            }
        }

        protected virtual void OnAttackOnEntity()
        {

        }

        protected void UseSkillOrMoveToEntity(IDamageableEntity entity, float distance)
        {
            if (queueUsingSkill.skill != null)
            {
                Transform applyTransform = queueUsingSkill.skill.GetApplyTransform(PlayerCharacterEntity, false);
                Vector3 measuringPosition = applyTransform.position;
                Vector3 targetPosition = entity.OpponentAimTransform.position;
                if (entity.GetObjectId() == PlayerCharacterEntity.GetObjectId() /* Applying skill to user? */ ||
                    Vector3.Distance(measuringPosition, targetPosition) <= distance)
                {
                    // Set next frame target action type
                    targetActionType = queueUsingSkill.skill.IsAttack() ? TargetActionType.Attack : TargetActionType.Activate;
                    // Stop movement to use skill
                    PlayerCharacterEntity.StopMove();
                    // Turn character to attacking target
                    TurnCharacterToEntity(entity.Entity);
                    // Use the skill
                    RequestUsePendingSkill();
                    // This function may be used by extending classes
                    OnUseSkillOnEntity();
                }
                else
                {
                    // Move to target entity
                    UpdateTargetEntityPosition(measuringPosition, targetPosition, distance);
                }
            }
            else
            {
                // Can't use skill
                targetActionType = TargetActionType.Activate;
                ClearQueueUsingSkill();
                return;
            }
        }

        protected virtual void OnUseSkillOnEntity()
        {

        }

        protected void UpdateTargetEntityPosition(Vector3 measuringPosition, Vector3 targetPosition, float distance)
        {
            if (PlayerCharacterEntity.IsPlayingActionAnimation())
                return;
            
            Vector3 direction = (targetPosition - measuringPosition).normalized;
            Vector3 position = targetPosition - (direction * (distance - StoppingDistance));
            if (Vector3.Distance(previousPointClickPosition, position) > 0.01f)
            {
                PlayerCharacterEntity.PointClickMovement(position);
                previousPointClickPosition = position;
            }
        }

        protected void TurnCharacterToEntity(BaseGameEntity entity)
        {
            if (entity == null)
                return;
            TurnCharacterToPosition(entity.CacheTransform.position);
        }

        protected void TurnCharacterToPosition(Vector3 position)
        {
            Vector3 lookAtDirection = (position - MovementTransform.position).normalized;
            if (lookAtDirection.sqrMagnitude > 0)
                PlayerCharacterEntity.SetLookRotation(Quaternion.LookRotation(lookAtDirection));
        }

        public override void UseHotkey(HotkeyType type, string relateId, Vector3? aimPosition)
        {
            ClearQueueUsingSkill();
            switch (type)
            {
                case HotkeyType.Skill:
                    UseSkill(relateId, aimPosition);
                    break;
                case HotkeyType.Item:
                    UseItem(relateId, aimPosition);
                    break;
            }
        }

        protected void UseSkill(string id, Vector3? aimPosition)
        {
            BaseSkill skill = null;
            short skillLevel = 0;
            // Avoid empty data
            if (!GameInstance.Skills.TryGetValue(BaseGameData.MakeDataId(id), out skill) || skill == null ||
                !PlayerCharacterEntity.GetCaches().Skills.TryGetValue(skill, out skillLevel))
                return;
            SetQueueUsingSkill(aimPosition, skill, skillLevel);
        }

        protected void UseItem(string id, Vector3? aimPosition)
        {
            InventoryType inventoryType;
            int itemIndex;
            byte equipWeaponSet;
            CharacterItem characterItem;
            if (PlayerCharacterEntity.IsEquipped(
                id,
                out inventoryType,
                out itemIndex,
                out equipWeaponSet,
                out characterItem))
            {
                PlayerCharacterEntity.RequestUnEquipItem(inventoryType, (short)itemIndex, equipWeaponSet);
                return;
            }

            if (itemIndex < 0)
                return;

            BaseItem item = characterItem.GetItem();
            if (item == null)
                return;

            if (item.IsEquipment())
            {
                PlayerCharacterEntity.RequestEquipItem((short)itemIndex);
            }
            else if (item.IsSkill())
            {
                // Set aim position to use immediately (don't add to queue)
                BaseSkill skill = (item as ISkillItem).UsingSkill;
                short skillLevel = (item as ISkillItem).UsingSkillLevel;
                SetQueueUsingSkill(aimPosition, skill, skillLevel, (short)itemIndex);
            }
            else if (item.IsBuilding())
            {
                destination = null;
                PlayerCharacterEntity.StopMove();
                buildingItemIndex = itemIndex;
                ShowConstructBuildingDialog();
            }
            else if (item.IsUsable())
            {
                PlayerCharacterEntity.RequestUseItem((short)itemIndex);
            }
        }

        public override Vector3? UpdateBuildAimControls(Vector2 aimAxes, BuildingEntity prefab)
        {
            // Instantiate constructing building
            if (ConstructingBuildingEntity == null)
                InstantiateConstructingBuilding(prefab);
            // Rotate by keys
            if (InputManager.GetButtonDown("RotateLeft"))
                ConstructingBuildingEntity.CacheTransform.eulerAngles -= Vector3.up * buildRotateAngle;
            else if (InputManager.GetButtonDown("RotateRight"))
                ConstructingBuildingEntity.CacheTransform.eulerAngles += Vector3.up * buildRotateAngle;
            // Find position to place building
            if (InputManager.useMobileInputOnNonMobile || Application.isMobilePlatform)
                FindAndSetBuildingAreaByAxes(aimAxes);
            else
                FindAndSetBuildingAreaByMousePosition();
            return ConstructingBuildingEntity.Position;
        }

        public override void FinishBuildAimControls(bool isCancel)
        {
            if (isCancel)
                CancelBuild();
        }
    }
}
