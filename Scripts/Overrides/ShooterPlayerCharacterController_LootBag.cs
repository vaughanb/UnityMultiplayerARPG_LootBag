using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class ShooterPlayerCharacterController_LootBag : ShooterPlayerCharacterController 
    {
        protected override async UniTaskVoid UpdateInputs_BattleMode()
        {
            updatingInputs = true;
            FireType rightHandFireType = FireType.SingleFire;
            FireType leftHandFireType = FireType.SingleFire;
            if (rightHandWeapon != null)
                rightHandFireType = rightHandWeapon.FireType;
            if (leftHandWeapon != null)
                leftHandFireType = leftHandWeapon.FireType;
            // Have to release fire key, then check press fire key later on next frame
            if (mustReleaseFireKey)
            {
                tempPressAttackRight = false;
                tempPressAttackLeft = false;
                if (!isLeftHandAttacking &&
                    (GetPrimaryAttackButtonUp() ||
                    !GetPrimaryAttackButton()))
                {
                    mustReleaseFireKey = false;
                    // Button released, start attacking while fire type is fire on release
                    if (rightHandFireType == FireType.FireOnRelease)
                        Attack(isLeftHandAttacking);
                }
                if (isLeftHandAttacking &&
                    (GetSecondaryAttackButtonUp() ||
                    !GetSecondaryAttackButton()))
                {
                    mustReleaseFireKey = false;
                    // Button released, start attacking while fire type is fire on release
                    if (leftHandFireType == FireType.FireOnRelease)
                        Attack(isLeftHandAttacking);
                }
            }
            if (PlayerCharacterEntity.IsPlayingAttackOrUseSkillAnimation())
                lastPlayingAttackOrUseSkillAnimationTime = Time.unscaledTime;
            bool anyKeyPressed = false;
            bool activatingEntityOrDoAction = false;
            if (queueUsingSkill.skill != null ||
                tempPressAttackRight ||
                tempPressAttackLeft ||
                activateInput.IsPress ||
                activateInput.IsRelease ||
                activateInput.IsHold ||
                PlayerCharacterEntity.IsPlayingActionAnimation())
            {
                anyKeyPressed = true;
                // Find forward character / npc / building / warp entity from camera center
                // Check is character playing action animation to turn character forwarding to aim position
                targetPlayer = null;
                targetNpc = null;
                targetBuilding = null;
                targetVehicle = null;
                targetWarpPortal = null;
                targetItemsContainer = null;
                if (!tempPressAttackRight && !tempPressAttackLeft)
                {
                    if (activateInput.IsHold)
                    {
                        if (SelectedEntity is BuildingEntity)
                        {
                            activatingEntityOrDoAction = true;
                            targetBuilding = SelectedEntity as BuildingEntity;
                        }
                    }
                    else if (activateInput.IsRelease)
                    {
                        if (SelectedEntity == null)
                        {
                            if (warpPortalEntityDetector?.warpPortals.Count > 0)
                            {
                                activatingEntityOrDoAction = true;
                                // It may not able to raycast from inside warp portal, so try to get it from the detector
                                targetWarpPortal = warpPortalEntityDetector.warpPortals[0];
                            }
                        }
                        else
                        {
                            if (SelectedEntity is BasePlayerCharacterEntity)
                            {
                                activatingEntityOrDoAction = true;
                                targetPlayer = SelectedEntity as BasePlayerCharacterEntity;
                            }
                            if (SelectedEntity is NpcEntity)
                            {
                                activatingEntityOrDoAction = true;
                                targetNpc = SelectedEntity as NpcEntity;
                            }
                            if (SelectedEntity is BuildingEntity)
                            {
                                activatingEntityOrDoAction = true;
                                targetBuilding = SelectedEntity as BuildingEntity;
                            }
                            if (SelectedEntity is VehicleEntity)
                            {
                                activatingEntityOrDoAction = true;
                                targetVehicle = SelectedEntity as VehicleEntity;
                            }
                            if (SelectedEntity is WarpPortalEntity)
                            {
                                activatingEntityOrDoAction = true;
                                targetWarpPortal = SelectedEntity as WarpPortalEntity;
                            }
                            if (SelectedEntity is ItemsContainerEntity)
                            {
                                activatingEntityOrDoAction = true;
                                targetItemsContainer = SelectedEntity as ItemsContainerEntity;
                            }
                        }
                    }
                }

                // Update look direction
                if (PlayerCharacterEntity.IsPlayingAttackOrUseSkillAnimation())
                {
                    activatingEntityOrDoAction = true;
                    while (!SetTargetLookDirectionWhileDoingAction())
                    {
                        await UniTask.Yield();
                    }
                }
                else if (queueUsingSkill.skill != null)
                {
                    activatingEntityOrDoAction = true;
                    while (!SetTargetLookDirectionWhileDoingAction())
                    {
                        await UniTask.Yield();
                    }
                    UpdateLookAtTarget();
                    UseSkill(isLeftHandAttacking);
                }
                else if (tempPressAttackRight || tempPressAttackLeft)
                {
                    activatingEntityOrDoAction = true;
                    while (!SetTargetLookDirectionWhileDoingAction())
                    {
                        await UniTask.Yield();
                    }
                    UpdateLookAtTarget();
                    if (!isLeftHandAttacking)
                    {
                        // Fire on release weapons have to release to fire, so when start holding, play weapon charge animation
                        if (rightHandFireType == FireType.FireOnRelease)
                            WeaponCharge(isLeftHandAttacking);
                        else
                            Attack(isLeftHandAttacking);
                    }
                    else
                    {
                        // Fire on release weapons have to release to fire, so when start holding, play weapon charge animation
                        if (leftHandFireType == FireType.FireOnRelease)
                            WeaponCharge(isLeftHandAttacking);
                        else
                            Attack(isLeftHandAttacking);
                    }
                }
                else if (activateInput.IsHold && activatingEntityOrDoAction)
                {
                    while (!SetTargetLookDirectionWhileDoingAction())
                    {
                        await UniTask.Yield();
                    }
                    UpdateLookAtTarget();
                    HoldActivate();
                }
                else if (activateInput.IsRelease && activatingEntityOrDoAction)
                {
                    while (!SetTargetLookDirectionWhileDoingAction())
                    {
                        await UniTask.Yield();
                    }
                    UpdateLookAtTarget();
                    Activate();
                }
                else
                {
                    SetTargetLookDirectionWhileMoving();
                }
            }

            if (tempPressWeaponAbility && !activatingEntityOrDoAction)
            {
                anyKeyPressed = true;
                // Toggle weapon ability
                switch (WeaponAbilityState)
                {
                    case WeaponAbilityState.Activated:
                    case WeaponAbilityState.Activating:
                        DeactivateWeaponAbility();
                        break;
                    case WeaponAbilityState.Deactivated:
                    case WeaponAbilityState.Deactivating:
                        ActivateWeaponAbility();
                        break;
                }
            }

            if (pickupItemInput.IsPress && !activatingEntityOrDoAction)
            {
                anyKeyPressed = true;

                // If target is entity with lootbag, open it
                if (SelectedEntity != null && SelectedEntity is BaseCharacterEntity)
                {
                    BaseCharacterEntity c = SelectedEntity as BaseCharacterEntity;
                    if (c != null && c.IsDead() && c.useLootBag)
                        (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(c);
                }
                // Otherwise find item to pick up
                else if (SelectedEntity != null && SelectedEntity is ItemDropEntity)
                {
                    activatingEntityOrDoAction = true;
                    PlayerCharacterEntity.CallServerPickupItem(SelectedEntity.ObjectId);
                }
            }

            if (pickupItemInput.IsPress && !activatingEntityOrDoAction)
            {
                anyKeyPressed = true;
                // Find for item to pick up
                if (SelectedEntity != null && SelectedEntity is ItemDropEntity)
                {
                    activatingEntityOrDoAction = true;
                    PlayerCharacterEntity.CallServerPickupItem(SelectedEntity.ObjectId);
                }
            }

            if (reloadInput.IsPress && !activatingEntityOrDoAction)
            {
                anyKeyPressed = true;
                // Reload ammo when press the button
                Reload();
            }

            if (exitVehicleInput.IsPress && !activatingEntityOrDoAction)
            {
                anyKeyPressed = true;
                // Exit vehicle
                PlayerCharacterEntity.CallServerExitVehicle();
            }

            if (switchEquipWeaponSetInput.IsPress && !activatingEntityOrDoAction)
            {
                anyKeyPressed = true;
                // Switch equip weapon set
                GameInstance.ClientInventoryHandlers.RequestSwitchEquipWeaponSet(new RequestSwitchEquipWeaponSetMessage()
                {
                    equipWeaponSet = (byte)(PlayerCharacterEntity.EquipWeaponSet + 1),
                }, ClientInventoryActions.ResponseSwitchEquipWeaponSet);
            }

            // Setup releasing state
            if (tempPressAttackRight && rightHandFireType != FireType.Automatic)
            {
                // The weapon's fire mode is single fire or fire on release, so player have to release fire key for next fire
                mustReleaseFireKey = true;
            }
            else if (tempPressAttackLeft && leftHandFireType != FireType.Automatic)
            {
                // The weapon's fire mode is single fire or fire on release, so player have to release fire key for next fire
                mustReleaseFireKey = true;
            }

            // Reloading
            if (PlayerCharacterEntity.EquipWeapons.rightHand.IsAmmoEmpty() ||
                PlayerCharacterEntity.EquipWeapons.leftHand.IsAmmoEmpty())
            {
                switch (emptyAmmoAutoReload)
                {
                    case EmptyAmmoAutoReload.ReloadImmediately:
                        Reload();
                        break;
                    case EmptyAmmoAutoReload.ReloadOnKeysReleased:
                        // Auto reload when ammo empty
                        if (!tempPressAttackRight && !tempPressAttackLeft && !reloadInput.IsPress)
                        {
                            // Reload ammo when empty and not press any keys
                            Reload();
                        }
                        break;
                }
            }

            // Update look direction
            if (!anyKeyPressed && !activatingEntityOrDoAction)
            {
                // Update look direction while moving without doing any action
                if (Time.unscaledTime - lastPlayingAttackOrUseSkillAnimationTime < stoppedPlayingAttackOrUseSkillAnimationDelay)
                {
                    activatingEntityOrDoAction = true;
                    while (!SetTargetLookDirectionWhileDoingAction())
                    {
                        await UniTask.Yield();
                    }
                }
                else
                {
                    SetTargetLookDirectionWhileMoving();
                }
            }

            updatingInputs = false;
        }
    }
}