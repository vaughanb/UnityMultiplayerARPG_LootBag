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
                        foreach (BaseCharacterEntity character in EnemyEntityDetector.characters)
                        {
                            if (character.IsDead() && character.useLootBag && character.LootBag.Count > 0)
                            {
                                SetTarget(character, TargetActionType.Loot);
                                (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(character);
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
                        if (!EnemyEntityDetector.characters[findingEnemyIndex].GetCaches().IsHide &&
                            !EnemyEntityDetector.characters[findingEnemyIndex].IsDead())
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
            EnemyEntityDetector.detectingRadius = PlayerCharacterEntity.GetAttackDistance(false) + lockAttackTargetDistance;
            // Update inputs
            UpdatePointClickInput();
            UpdateWASDInput();
            UpdateBuilding();
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
                        if (tempBuildingMaterial != null && tempBuildingMaterial.entity != null)
                            targetBuilding = tempBuildingMaterial.entity;
                        if (targetBuilding && !targetBuilding.IsDead())
                        {
                            SetTarget(targetBuilding, TargetActionType.Undefined);
                            IsEditingBuilding = true;
                            tempHasMapPosition = false;
                            break;
                        }
                    }
                    // When clicking on target
                    else if (mouseUpOnTarget)
                    {
                        targetPlayer = tempTransform.GetComponent<BasePlayerCharacterEntity>();
                        targetMonster = tempTransform.GetComponent<BaseMonsterCharacterEntity>();
                        targetNpc = tempTransform.GetComponent<NpcEntity>();
                        targetItemDrop = tempTransform.GetComponent<ItemDropEntity>();
                        targetHarvestable = tempTransform.GetComponent<HarvestableEntity>();
                        targetBuilding = null;
                        tempBuildingMaterial = tempTransform.GetComponent<BuildingMaterial>();
                        if (tempBuildingMaterial != null && tempBuildingMaterial.entity != null)
                            targetBuilding = tempBuildingMaterial.entity;
                        targetVehicle = tempTransform.GetComponent<VehicleEntity>();
                        lastNpcObjectId = 0;
                        if (targetPlayer && !targetPlayer.GetCaches().IsHide)
                        {
                            // Found activating entity as player character entity
                            SetTarget(targetPlayer, TargetActionType.Attack);
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetCharacter && targetCharacter.IsDead() && targetCharacter.LootBag.Count > 0)
                        {
                            // Found activating entity as character entity with loot bag
                            SetTarget(targetCharacter, TargetActionType.Loot);
                            tempHasMapPosition = false;
                        }
                        else if (targetMonster && !targetMonster.GetCaches().IsHide)
                        {
                            // Found activating entity as monster character entity
                            SetTarget(targetMonster, TargetActionType.Attack);
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetNpc)
                        {
                            // Found activating entity as npc entity
                            SetTarget(targetNpc, TargetActionType.Undefined);
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetItemDrop)
                        {
                            // Found activating entity as item drop entity
                            SetTarget(targetItemDrop, TargetActionType.Undefined);
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetHarvestable && !targetHarvestable.IsDead())
                        {
                            // Found activating entity as harvestable entity
                            SetTarget(targetHarvestable, TargetActionType.Undefined);
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetBuilding && !targetBuilding.IsDead() && targetBuilding.Activatable)
                        {
                            // Found activating entity as building entity
                            SetTarget(targetBuilding, TargetActionType.Undefined);
                            IsEditingBuilding = false;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetVehicle)
                        {
                            // Found activating entity as vehicle entity
                            SetTarget(targetVehicle, TargetActionType.Undefined);
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
                    }
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
                // If Found target, do something
                if (targetPosition.HasValue)
                {
                    if (controllerMode == PlayerCharacterControllerMode.WASD)
                    {
                        destination = null;
                    }
                    else
                    {
                        // Close NPC dialog, when target changes
                        HideNpcDialog();
                        ClearQueueUsingSkill();

                        // Move to target, will hide destination when target is object
                        if (TargetEntity != null)
                        {
                            destination = null;
                        }
                        else
                        {
                            destination = targetPosition.Value;
                            targetLookDirection = null;
                            PlayerCharacterEntity.PointClickMovement(targetPosition.Value);
                        }
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
            targetActionType = TargetActionType.Undefined;
        }

        public override void DeselectBuilding()
        {
            base.DeselectBuilding();
            ClearTarget();
        }

        public virtual void UpdateWASDInput()
        {
            if (controllerMode != PlayerCharacterControllerMode.WASD &&
                controllerMode != PlayerCharacterControllerMode.Both)
                return;

            // If mobile platforms, don't receive input raw to make it smooth
            bool raw = !InputManager.useMobileInputOnNonMobile && !Application.isMobilePlatform;
            Vector3 moveDirection = GetMoveDirection(InputManager.GetAxis("Horizontal", raw), InputManager.GetAxis("Vertical", raw));
            moveDirection.Normalize();

            if (moveDirection.sqrMagnitude > 0f)
            {
                HideNpcDialog();
                ClearQueueUsingSkill();
                FindAndSetBuildingAreaFromCharacterDirection();
            }

            // Attack when player pressed attack button
            if (queueUsingSkill.skill != null)
                UpdateWASDPendingSkill(queueUsingSkill.skill, queueUsingSkill.level);
            else if (InputManager.GetButton("Attack"))
                UpdateWASDAttack();

            // Move
            if (moveDirection.sqrMagnitude > 0f)
            {
                destination = null;
                ClearTarget();
                targetLookDirection = moveDirection;
            }
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

            if (IsLockTarget() && !TryGetAttackingCharacter(out targetEntity))
            {
                // Find nearest target and move to the target
                SelectedEntity = PlayerCharacterEntity
                    .FindNearestAliveCharacter<BaseCharacterEntity>(
                    PlayerCharacterEntity.GetAttackDistance(isLeftHandAttacking) + lockAttackTargetDistance,
                    false,
                    true,
                    false);
                if (SelectedEntity != null)
                {
                    // Set target, then attack later when moved nearby target
                    SetTarget(SelectedEntity, TargetActionType.Attack, false);
                }
                else
                {
                    // No nearby target, so attack immediately
                    if (PlayerCharacterEntity.RequestAttack(isLeftHandAttacking))
                        isLeftHandAttacking = !isLeftHandAttacking;
                }
            }
            else if (!IsLockTarget())
            {
                // Find nearest target and set selected target to show character hp/mp UIs
                SelectedEntity = PlayerCharacterEntity
                    .FindNearestAliveCharacter<BaseCharacterEntity>(
                    PlayerCharacterEntity.GetAttackDistance(isLeftHandAttacking),
                    false,
                    true,
                    false,
                    true,
                    PlayerCharacterEntity.GetAttackFov(isLeftHandAttacking));
                // Not lock target, so not finding target and attack immediately
                if (PlayerCharacterEntity.RequestAttack(isLeftHandAttacking))
                    isLeftHandAttacking = !isLeftHandAttacking;
            }
        }

        protected void UpdateWASDPendingSkill(BaseSkill skill, short skillLevel)
        {
            destination = null;
            BaseCharacterEntity targetEntity;

            if (skill.IsAttack())
            {
                if (TryGetSelectedTargetAsAttackingEntity(out targetEntity))
                    SetTarget(targetEntity, TargetActionType.Attack, false);

                if (IsLockTarget() && !TryGetAttackingCharacter(out targetEntity))
                {
                    BaseCharacterEntity nearestTarget = PlayerCharacterEntity
                        .FindNearestAliveCharacter<BaseCharacterEntity>(
                        skill.GetCastDistance(PlayerCharacterEntity, skillLevel, isLeftHandAttacking) + lockAttackTargetDistance,
                        false,
                        true,
                        false);
                    if (nearestTarget != null)
                    {
                        // Set target, then use skill later when moved nearby target
                        SetTarget(nearestTarget, TargetActionType.Attack, false);
                    }
                    else
                    {
                        // No nearby target, so use skill immediately
                        RequestUsePendingSkill();
                    }
                }
                else if (!IsLockTarget())
                {
                    // Not lock target, so not finding target and use skill immediately
                    RequestUsePendingSkill();
                }
            }
            else
            {
                // Not attack skill, so use skill immediately
                if (skill.RequiredTarget())
                {
                    if (IsLockTarget())
                    {
                        // Let's update follow target do it
                        return;
                    }
                    // TODO: Check is target nearby or not
                    ClearQueueUsingSkill();
                }
                else
                {
                    // Target not required, use skill immediately
                    RequestUsePendingSkill();
                }
            }
        }

        public void UpdateBuilding()
        {
            if (ConstructingBuildingEntity == null)
                return;

            bool isPointerOverUI = CacheUISceneGameplay != null && CacheUISceneGameplay.IsPointerOverUIObject();
            if (Input.GetMouseButton(0) && !isPointerOverUI)
            {
                mouseDownTime = Time.unscaledTime;
                mouseDownPosition = Input.mousePosition;
                FindAndSetBuildingAreaFromMousePosition();
            }
        }

        public void UpdateFollowTarget()
        {
            if (!IsLockTarget())
                return;

            if (TryGetLootableCharacter(out targetCharacter))
            {
                if (Vector3.Distance(MovementTransform.position, targetCharacter.CacheTransform.position) <= CurrentGameInstance.conversationDistance &&
                    !InputManager.useMobileInputOnNonMobile && !Application.isMobilePlatform)
                {
                    (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(targetMonster);
                    return;
                }
            }
            else if (TryGetAttackingCharacter(out targetCharacter))
            {
                if (targetCharacter.GetCaches().IsHide || targetCharacter.IsDead())
                {
                    ClearQueueUsingSkill();
                    PlayerCharacterEntity.StopMove();
                    ClearTarget();
                    return;
                }

                if (queueUsingSkill.skill != null && !queueUsingSkill.skill.IsAttack())
                {
                    // Try use non-attack skill
                    PlayerCharacterEntity.StopMove();
                    RequestUsePendingSkill();
                    return;
                }

                // Find attack distance and fov, from weapon or skill
                float attackDistance = 0f;
                float attackFov = 0f;
                GetAttackDistanceAndFov(isLeftHandAttacking, out attackDistance, out attackFov);
                AttackOrMoveToEntity(targetCharacter, attackDistance, CurrentGameInstance.characterLayer.Mask, () =>
                {
                    // If has queue using skill, attack by the skill
                    if (queueUsingSkill.skill != null)
                    {
                        RequestUsePendingSkill();
                        return;
                    }
                    else
                    {
                        RequestAttack();
                        return;
                    }
                });
            }
            else if (TryGetUsingSkillCharacter(out targetCharacter))
            {
                // Find attack distance and fov, from weapon or skill
                float castDistance = 0f;
                float castFov = 0f;
                GetUseSkillDistanceAndFov(out castDistance, out castFov);
                UseSkillOrMoveToEntity(targetCharacter, castDistance);
            }
            else if (PlayerCharacterEntity.TryGetTargetEntity(out targetCharacter))
            {
                DoActionOrMoveToEntity(targetCharacter, CurrentGameInstance.conversationDistance, () =>
                {
                    // TODO: Do something
                });
            }
            else if (PlayerCharacterEntity.TryGetTargetEntity(out targetNpc))
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
            else if (PlayerCharacterEntity.TryGetTargetEntity(out targetItemDrop))
            {
                DoActionOrMoveToEntity(targetItemDrop, CurrentGameInstance.pickUpItemDistance, () =>
                {
                    PlayerCharacterEntity.RequestPickupItem(targetItemDrop.ObjectId);
                    ClearTarget();
                });
            }
            else if (PlayerCharacterEntity.TryGetTargetEntity(out targetBuilding))
            {
                DoActionOrMoveToEntity(targetBuilding, CurrentGameInstance.conversationDistance, () =>
                {
                    if (!IsEditingBuilding)
                    {
                        ActivateBuilding(targetBuilding);
                        ClearTarget();
                    }
                    else
                    {
                        ShowCurrentBuildingDialog();
                    }
                });
            }
            else if (PlayerCharacterEntity.TryGetTargetEntity(out targetHarvestable))
            {
                if (targetHarvestable.IsDead())
                {
                    ClearQueueUsingSkill();
                    PlayerCharacterEntity.StopMove();
                    ClearTarget();
                    return;
                }

                // Find attack distance and fov, from weapon
                float attackDistance = 0f;
                float attackFov = 0f;
                GetAttackDistanceAndFov(isLeftHandAttacking, out attackDistance, out attackFov);
                AttackOrMoveToEntity(targetHarvestable, attackDistance, CurrentGameInstance.harvestableLayer.Mask, () =>
                {
                    RequestAttack();
                });
            }
            else if (PlayerCharacterEntity.TryGetTargetEntity(out targetVehicle))
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
            if (Vector3.Distance(MovementTransform.position, entity.CacheTransform.position) <= distance)
            {
                // Stop movement to do action
                PlayerCharacterEntity.StopMove();
                // Do action
                action.Invoke();
            }
            else
            {
                // Move to target entity
                UpdateTargetEntityPosition(entity, distance);
            }
        }

        protected void AttackOrMoveToEntity(IDamageableEntity entity, float distance, int layerMask, System.Action action)
        {
            if (IsTargetInAttackDistance(entity, distance, layerMask))
            {
                // Stop movement to attack
                PlayerCharacterEntity.StopMove();
                // Turn character to attacking target
                TurnCharacterToEntity(entity.Entity);
                // Set direction to turn character to target, now use fov = 10, to make character always turn to target
                if (PlayerCharacterEntity.IsPositionInFov(10f, entity.GetTransform().position))
                {
                    // Do action
                    action.Invoke();
                }
            }
            else
            {
                // Move to target entity
                UpdateTargetEntityPosition(entity.Entity, distance);
            }
        }

        protected void UseSkillOrMoveToEntity(IDamageableEntity entity, float distance)
        {
            if (entity.GetObjectId() == PlayerCharacterEntity.GetObjectId() || 
                Vector3.Distance(MovementTransform.position, entity.GetTransform().position) <= distance)
            {
                // Stop movement to use skill
                PlayerCharacterEntity.StopMove();
                // Turn character to attacking target
                TurnCharacterToEntity(entity.Entity);
                // Set direction to turn character to target, now use fov = 10, to make character always turn to target
                if (entity.GetObjectId() == PlayerCharacterEntity.GetObjectId() || 
                    PlayerCharacterEntity.IsPositionInFov(10f, entity.GetTransform().position))
                {
                    if (queueUsingSkill.skill != null)
                    {
                        // Can use skill
                        RequestUsePendingSkill();
                        targetActionType = TargetActionType.Undefined;
                        return;
                    }
                    else
                    {
                        // Can't use skill
                        targetActionType = TargetActionType.Undefined;
                        ClearQueueUsingSkill();
                        return;
                    }
                }
            }
            else
            {
                // Move to target entity
                UpdateTargetEntityPosition(entity.Entity, distance);
            }
        }

        protected void UpdateTargetEntityPosition(BaseGameEntity entity, float distance)
        {
            if (entity == null)
                return;
            Vector3 direction = (entity.CacheTransform.position - MovementTransform.position).normalized;
            Vector3 position = entity.CacheTransform.position - (direction * (distance - StoppingDistance));
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
            targetLookDirection = (entity.CacheTransform.position - MovementTransform.position).normalized;
        }

        public void UpdateLookAtTarget()
        {
            if (targetLookDirection.HasValue && Vector3.Angle(tempLookAt * Vector3.forward, targetLookDirection.Value) > 1)
            {
                // Update rotation when angle difference more than 1
                tempLookAt = Quaternion.RotateTowards(tempLookAt, Quaternion.LookRotation(targetLookDirection.Value), Time.deltaTime * angularSpeed);
                PlayerCharacterEntity.SetLookRotation(tempLookAt);
            }
            else
            {
                // Update temp look at to character's rotation
                tempLookAt = PlayerCharacterEntity.GetLookRotation();
                targetLookDirection = null;
            }
        }

        public override void UseHotkey(int hotkeyIndex, Vector3? aimPosition)
        {
            if (hotkeyIndex < 0 || hotkeyIndex >= PlayerCharacterEntity.Hotkeys.Count)
                return;

            CancelBuild();
            buildingItemIndex = -1;
            ConstructingBuildingEntity = null;
            ClearQueueUsingSkill();

            CharacterHotkey hotkey = PlayerCharacterEntity.Hotkeys[hotkeyIndex];
            switch (hotkey.type)
            {
                case HotkeyType.Skill:
                    UseSkill(hotkey.relateId, aimPosition);
                    break;
                case HotkeyType.Item:
                    UseItem(hotkey.relateId, aimPosition);
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

            // Set aim position to use immediately (don't add to queue)
            UseSkill(
                skill,
                skillLevel,
                () => SetQueueUsingSkill(aimPosition, skill, skillLevel),
                () =>
                {
                    if (!aimPosition.HasValue)
                        PlayerCharacterEntity.RequestUseSkill(skill.DataId, isLeftHandAttacking);
                    else
                        PlayerCharacterEntity.RequestUseSkill(skill.DataId, isLeftHandAttacking, aimPosition.Value);
                });
        }

        protected void UseSkill(
            BaseSkill skill,
            short skillLevel,
            System.Action setQueueFunction,
            System.Action useFunction)
        {
            BaseCharacterEntity attackingCharacter;
            if (TryGetAttackingCharacter(out attackingCharacter))
            {
                // If attacking any character, will use skill later
                setQueueFunction.Invoke();
            }
            else
            {
                // If not attacking any character, use skill immediately
                if (skill.IsAttack())
                {
                    // Default damage type attacks
                    if (IsLockTarget() && !skill.HasCustomAimControls())
                    {
                        // If attacking any character, will use skill later
                        setQueueFunction.Invoke();
                        if (SelectedEntity == null && !(SelectedEntity is BaseCharacterEntity))
                        {
                            // Attacking nearest target
                            BaseCharacterEntity nearestTarget = PlayerCharacterEntity
                                .FindNearestAliveCharacter<BaseCharacterEntity>(
                                skill.GetCastDistance(PlayerCharacterEntity, skillLevel, isLeftHandAttacking) + lockAttackTargetDistance,
                                false,
                                true,
                                false);
                            if (nearestTarget != null)
                                SetTarget(nearestTarget, TargetActionType.Attack);
                        }
                    }
                    else
                    {
                        // Not lock target, use it immediately
                        destination = null;
                        PlayerCharacterEntity.StopMove();
                        useFunction.Invoke();
                    }
                }
                else
                {
                    // This is not attack skill, use it immediately
                    destination = null;
                    PlayerCharacterEntity.StopMove();
                    if (skill.RequiredTarget())
                    {
                        setQueueFunction.Invoke();
                        if (IsLockTarget())
                            SetTarget(SelectedEntity, TargetActionType.UseSkill, false);
                    }
                    else
                    {
                        useFunction.Invoke();
                    }
                }
            }
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
            else if (item.IsUsable())
            {
                if (item.IsSkill())
                {
                    // Set aim position to use immediately (don't add to queue)
                    BaseSkill skill = (item as ISkillItem).UsingSkill;
                    short skillLevel = (item as ISkillItem).UsingSkillLevel;
                    UseSkill(
                        skill,
                        skillLevel,
                        () => SetQueueUsingSkill(aimPosition, skill, skillLevel, (short)itemIndex),
                        () =>
                        {
                            if (!aimPosition.HasValue)
                                PlayerCharacterEntity.RequestUseSkillItem((short)itemIndex, isLeftHandAttacking);
                            else
                                PlayerCharacterEntity.RequestUseSkillItem((short)itemIndex, isLeftHandAttacking, aimPosition.Value);
                        });
                }
                else
                {
                    PlayerCharacterEntity.RequestUseItem((short)itemIndex);
                }
            }
            else if (item.IsBuilding())
            {
                destination = null;
                PlayerCharacterEntity.StopMove();
                buildingItemIndex = itemIndex;
                ConstructingBuildingEntity = Instantiate((item as IBuildingItem).BuildingEntity);
                ConstructingBuildingEntity.SetupAsBuildMode();
                ConstructingBuildingEntity.CacheTransform.parent = null;
                FindAndSetBuildingAreaFromCharacterDirection();
                ShowConstructBuildingDialog();
            }
        }
    }
}
