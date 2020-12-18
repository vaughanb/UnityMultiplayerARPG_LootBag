using UnityEngine;

namespace MultiplayerARPG
{
    public class PlayerCharacterController_Custom : PlayerCharacterController
    {
        /// <summary>
        /// Make necessary changes to PlayerCharacterController settings on awake.
        /// For loot purposes, we need to find dead characters.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            EnemyEntityDetector.findOnlyAlivePlayers = false;
            EnemyEntityDetector.findOnlyAliveMonsters = false;
        }

        /// <summary>
        /// Calls base UpdateInput method and handles loot button controls.
        /// </summary>
        public override void UpdateInput()
        {
            base.UpdateInput();

            // Loot items from nearest lootable enemy
            if (InputManager.GetButtonDown("Loot"))
            {
                if (EnemyEntityDetector.characters.Count > 0)
                {
                    foreach (BaseCharacterEntity characterEntity in EnemyEntityDetector.characters)
                    {
                        if (characterEntity.IsDead() && characterEntity.useLootBag && characterEntity.LootBag.Count > 0)
                        {
                            TargetActionType loot = (TargetActionType)4;
                            SetTarget(characterEntity, loot);
                            (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(characterEntity);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates point and click input, including loot controls.
        /// </summary>
        public override void UpdatePointClickInput()
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
            isPointerOverUI = CacheUISceneGameplay.IsPointerOverUIObject();
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
                didActionOnTarget = false;
                // Prepare temp variables
                Transform tempTransform;
                Vector3 tempVector3;
                bool tempHasMapPosition = false;
                Vector3 tempMapPosition = Vector3.zero;
                BuildingMaterial tempBuildingMaterial;
                // If mouse up while cursor point to target (character, item, npc and so on)
                bool mouseUpOnTarget = getMouseUp && !isMouseDragOrHoldOrOverUI;
                int tempCount = FindClickObjects(out tempVector3);
                for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
                {
                    tempTransform = physicFunctions.GetRaycastTransform(tempCounter);
                    // When holding on target, or already enter edit building mode
                    if (isMouseHoldAndNotDrag)
                    {
                        targetBuilding = null;
                        tempBuildingMaterial = tempTransform.GetComponent<BuildingMaterial>();
                        if (tempBuildingMaterial != null)
                            targetBuilding = tempBuildingMaterial.BuildingEntity;
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
                        targetCharacterEntity = tempTransform.GetComponent<BaseCharacterEntity>();
                        targetPlayer = tempTransform.GetComponent<BasePlayerCharacterEntity>();
                        targetMonster = tempTransform.GetComponent<BaseMonsterCharacterEntity>();
                        targetNpc = tempTransform.GetComponent<NpcEntity>();
                        targetItemDrop = tempTransform.GetComponent<ItemDropEntity>();
                        targetHarvestable = tempTransform.GetComponent<HarvestableEntity>();
                        targetBuilding = null;
                        tempBuildingMaterial = tempTransform.GetComponent<BuildingMaterial>();
                        if (tempBuildingMaterial != null)
                            targetBuilding = tempBuildingMaterial.BuildingEntity;
                        targetVehicle = tempTransform.GetComponent<VehicleEntity>();
                        if (targetCharacterEntity && targetCharacterEntity.IsDead() && targetCharacterEntity.useLootBag)
                        {
                            // Found activating entity as lootable character entity
                            TargetActionType loot = (TargetActionType)4;
                            SetTarget(targetCharacterEntity, loot);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        if (targetPlayer && !targetPlayer.IsHideOrDead())
                        {
                            // Found activating entity as player character entity
                            if (!targetPlayer.IsHideOrDead() && !targetPlayer.IsAlly(PlayerCharacterEntity))
                                SetTarget(targetPlayer, TargetActionType.Attack);
                            else
                                SetTarget(targetPlayer, TargetActionType.Activate);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (targetMonster && !targetMonster.IsHideOrDead())
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
                            if (targetVehicle.ShouldBeAttackTarget)
                                SetTarget(targetVehicle, TargetActionType.Attack);
                            else
                                SetTarget(targetVehicle, TargetActionType.Activate);
                            isFollowingTarget = true;
                            tempHasMapPosition = false;
                            break;
                        }
                        else if (!physicFunctions.GetRaycastIsTrigger(tempCounter))
                        {
                            // Set clicked map position, it will be used if no activating entity found
                            tempHasMapPosition = true;
                            tempMapPosition = physicFunctions.GetRaycastPoint(tempCounter);
                            break;
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
                    if (PlayerCharacterEntity.IsPlayingActionAnimation())
                    {
                        if (pointClickInterruptCastingSkill)
                            PlayerCharacterEntity.CallServerSkillCastingInterrupt();
                    }
                    else
                    {
                        OnPointClickOnGround(targetPosition.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Calls base Update method and updates follow target lootable.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            UpdateFollowTarget_Lootable();
        }

        /// <summary>
        /// Follows the lootable target and shows loot bag if close.
        /// </summary>
        public void UpdateFollowTarget_Lootable()
        {
            if (!isFollowingTarget)
                return;

            if (TryGetLootableCharacter(out targetCharacterEntity))
            {
                if (targetCharacterEntity.deathTime != null)
                {
                    // If target was just killed, clear it and return so that the loot 
                    // window does not appear during combat
                    if (System.DateTime.Now < targetCharacterEntity.deathTime.AddSeconds(1))
                    {
                        ClearTarget();
                        return;
                    }
                }

                DoActionOrMoveToEntity(targetCharacterEntity, CurrentGameInstance.conversationDistance, () =>
                {
                    if (!InputManager.useMobileInputOnNonMobile && !Application.isMobilePlatform)
                        (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(targetCharacterEntity);
                });
            }

            UpdateFollowTarget();
        }
    }
}
