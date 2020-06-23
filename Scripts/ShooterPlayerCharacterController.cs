using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class ShooterPlayerCharacterController : BasePlayerCharacterController
    {
        public enum ControllerMode
        {
            Adventure,
            Combat,
        }

        public enum ControllerViewMode
        {
            Tps,
            Fps,
        }

        public enum ExtraMoveActiveMode
        {
            None,
            Toggle,
            Hold
        }

        public enum TurningState
        {
            None,
            Attack,
            Activate,
            UseSkill,
        }

        [SerializeField]
        private ControllerMode mode;
        [SerializeField]
        private bool canSwitchViewMode;
        [SerializeField]
        private ControllerViewMode viewMode;
        [SerializeField]
        private ExtraMoveActiveMode sprintActiveMode;
        [SerializeField]
        private ExtraMoveActiveMode crouchActiveMode;
        [SerializeField]
        private ExtraMoveActiveMode crawlActiveMode;
        [SerializeField]
        private float angularSpeed = 800f;
        [Range(0, 1f)]
        [SerializeField]
        private float turnToTargetDuration = 0.1f;
        [SerializeField]
        private float findTargetRaycastDistance = 16f;
        [SerializeField]
        private bool showConfirmConstructionUI = false;
        [SerializeField]
        private float buildRotateAngle = 45f;
        [SerializeField]
        private RectTransform crosshairRect;

        [Header("TPS Settings")]
        [SerializeField]
        private float tpsZoomDistance = 3f;
        [SerializeField]
        private float tpsMinZoomDistance = 3f;
        [SerializeField]
        private float tpsMaxZoomDistance = 3f;
        [SerializeField]
        private Vector3 tpsTargetOffset = new Vector3(0.75f, 1.25f, 0f);
        [SerializeField]
        private float tpsFov = 60f;
        [SerializeField]
        private float tpsNearClipPlane = 0.3f;
        [SerializeField]
        private float tpsFarClipPlane = 1000f;
        [SerializeField]
        private bool turnForwardWhileDoingAction;

        [Header("FPS Settings")]
        [SerializeField]
        private float fpsZoomDistance = 0f;
        [SerializeField]
        private Vector3 fpsTargetOffset = new Vector3(0f, 0f, 0f);
        [SerializeField]
        private float fpsFov = 40f;
        [SerializeField]
        private float fpsNearClipPlane = 0.01f;
        [SerializeField]
        private float fpsFarClipPlane = 1000f;

        public bool IsBlockController { get; protected set; }

        public ControllerMode Mode
        {
            get
            {
                if (viewMode == ControllerViewMode.Fps)
                {
                    // If view mode is fps, controls type must be combat
                    return ControllerMode.Combat;
                }
                return mode;
            }
        }

        public ControllerViewMode ViewMode
        {
            get { return viewMode; }
            set { viewMode = value; }
        }

        public float CameraZoomDistance
        {
            get { return ViewMode == ControllerViewMode.Tps ? tpsZoomDistance : fpsZoomDistance; }
        }

        public float CameraMinZoomDistance
        {
            get { return ViewMode == ControllerViewMode.Tps ? tpsMinZoomDistance : fpsZoomDistance; }
        }

        public float CameraMaxZoomDistance
        {
            get { return ViewMode == ControllerViewMode.Tps ? tpsMaxZoomDistance : fpsZoomDistance; }
        }

        public Vector3 CameraTargetOffset
        {
            get { return ViewMode == ControllerViewMode.Tps ? tpsTargetOffset : fpsTargetOffset; }
        }

        public float CameraFov
        {
            get { return ViewMode == ControllerViewMode.Tps ? tpsFov : fpsFov; }
        }

        public float CameraNearClipPlane
        {
            get { return ViewMode == ControllerViewMode.Tps ? tpsNearClipPlane : fpsNearClipPlane; }
        }

        public float CameraFarClipPlane
        {
            get { return ViewMode == ControllerViewMode.Tps ? tpsFarClipPlane : fpsFarClipPlane; }
        }

        // Input data
        InputStateManager activateInput;
        InputStateManager pickupItemInput;
        InputStateManager reloadInput;
        InputStateManager exitVehicleInput;
        InputStateManager switchEquipWeaponSetInput;
        // Temp data
        ControllerViewMode dirtyViewMode;
        BuildingMaterial tempBuildingMaterial;
        IDamageableEntity tempDamageableEntity;
        BaseGameEntity tempEntity;
        Ray centerRay;
        float centerOriginToCharacterDistance;
        Vector3 moveDirection;
        Vector3 cameraForward;
        Vector3 cameraRight;
        float inputV;
        float inputH;
        Vector3 moveLookDirection;
        Vector3 targetLookDirection;
        Quaternion tempLookAt;
        TurningState turningState;
        float tempDeltaTime;
        float calculatedTurnDuration;
        float tempCalculateAngle;
        bool tempPressAttackRight;
        bool tempPressAttackLeft;
        bool tempPressWeaponAbility;
        bool isLeftHandAttacking;
        GameObject tempGameObject;
        BasePlayerCharacterEntity targetPlayer;
        NpcEntity targetNpc;
        BuildingEntity targetBuilding;
        VehicleEntity targetVehicle;
        RaycastHit[] raycasts = new RaycastHit[512];
        Collider[] overlapColliders = new Collider[512];
        RaycastHit tempHitInfo;
        float pitch;
        Vector3 aimPosition;
        Vector3 aimDirection;
        // Crosshair
        public Vector2 CurrentCrosshairSize { get; private set; }
        public CrosshairSetting CurrentCrosshairSetting { get; private set; }
        // Controlling states
        bool isDoingAction;
        bool mustReleaseFireKey;
        IWeaponItem rightHandWeapon;
        IWeaponItem leftHandWeapon;
        MovementState movementState;
        ExtraMovementState extraMovementState;
        bool toggleSprintOn;
        bool toggleCrouchOn;
        bool toggleCrawlOn;
        ControllerViewMode? viewModeBeforeDead;
        float buildYRotate;
        public BaseWeaponAbility WeaponAbility { get; private set; }
        public WeaponAbilityState WeaponAbilityState { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            buildingItemIndex = -1;
            ConstructingBuildingEntity = null;
            activateInput = new InputStateManager("Activate");
            pickupItemInput = new InputStateManager("PickUpItem");
            reloadInput = new InputStateManager("Reload");
            exitVehicleInput = new InputStateManager("ExitVehicle");
            switchEquipWeaponSetInput = new InputStateManager("SwitchEquipWeaponSet");
        }

        protected override void Setup(BasePlayerCharacterEntity characterEntity)
        {
            base.Setup(characterEntity);

            if (characterEntity == null)
                return;

            tempLookAt = characterEntity.GetLookRotation();

            SetupEquipWeapons(characterEntity.EquipWeapons);

            characterEntity.onEquipWeaponSetChange += SetupEquipWeapons;
            characterEntity.onSelectableWeaponSetsOperation += SetupEquipWeapons;
            characterEntity.ModelManager.InstantiateFpsModel(CacheGameplayCameraTransform);
            characterEntity.ModelManager.SetIsFps(ViewMode == ControllerViewMode.Fps);
        }

        protected override void Desetup(BasePlayerCharacterEntity characterEntity)
        {
            base.Desetup(characterEntity);

            if (characterEntity == null)
                return;

            characterEntity.onEquipWeaponSetChange -= SetupEquipWeapons;
            characterEntity.onSelectableWeaponSetsOperation -= SetupEquipWeapons;
        }

        protected void SetupEquipWeapons(byte equipWeaponSet)
        {
            SetupEquipWeapons(PlayerCharacterEntity.EquipWeapons);
        }

        protected void SetupEquipWeapons(LiteNetLibManager.LiteNetLibSyncList.Operation operation, int index)
        {
            SetupEquipWeapons(PlayerCharacterEntity.EquipWeapons);
        }

        protected void SetupEquipWeapons(EquipWeapons equipWeapons)
        {
            CurrentCrosshairSetting = PlayerCharacterEntity.GetCrosshairSetting();
            UpdateCrosshair(CurrentCrosshairSetting, -CurrentCrosshairSetting.shrinkPerFrame);

            rightHandWeapon = equipWeapons.GetRightHandWeaponItem();
            leftHandWeapon = equipWeapons.GetLeftHandWeaponItem();
            // Weapon ability will be able to use when equip weapon at main-hand only
            if (rightHandWeapon != null && leftHandWeapon == null)
            {
                if (rightHandWeapon.WeaponAbility != WeaponAbility)
                {
                    if (WeaponAbility != null)
                        WeaponAbility.Desetup();
                    WeaponAbility = rightHandWeapon.WeaponAbility;
                    if (WeaponAbility != null)
                        WeaponAbility.Setup(this, equipWeapons.rightHand);
                    WeaponAbilityState = WeaponAbilityState.Deactivated;
                }
            }
            else
            {
                if (WeaponAbility != null)
                    WeaponAbility.Desetup();
                WeaponAbility = null;
                WeaponAbilityState = WeaponAbilityState.Deactivated;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        protected override void Update()
        {
            if (PlayerCharacterEntity == null || !PlayerCharacterEntity.IsOwnerClient)
                return;

            base.Update();

            if (PlayerCharacterEntity.IsDead())
            {
                // Set view mode to TPS when character dead
                if (!viewModeBeforeDead.HasValue)
                    viewModeBeforeDead = ViewMode;
                ViewMode = ControllerViewMode.Tps;
            }
            else
            {
                // Set view mode to view mode before dead when character alive
                if (viewModeBeforeDead.HasValue)
                {
                    ViewMode = viewModeBeforeDead.Value;
                    viewModeBeforeDead = null;
                }
            }

            if (dirtyViewMode != viewMode)
            {
                dirtyViewMode = viewMode;
                UpdateCameraSettings();
            }
            CacheGameplayCameraControls.target = ViewMode == ControllerViewMode.Fps ? PlayerCharacterEntity.FpsCameraTargetTransform : PlayerCharacterEntity.CameraTargetTransform;

            // Set temp data
            tempDeltaTime = Time.deltaTime;
            calculatedTurnDuration += tempDeltaTime;

            // Update inputs
            activateInput.OnUpdate(tempDeltaTime);
            pickupItemInput.OnUpdate(tempDeltaTime);
            reloadInput.OnUpdate(tempDeltaTime);
            exitVehicleInput.OnUpdate(tempDeltaTime);
            switchEquipWeaponSetInput.OnUpdate(tempDeltaTime);

            // Check is any UIs block controller or not?
            IsBlockController = CacheUISceneGameplay.IsBlockController();

            // Lock cursor when not show UIs
            if (InputManager.useMobileInputOnNonMobile || Application.isMobilePlatform)
            {
                // Control camera by touch-screen
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                CacheGameplayCameraControls.updateRotationX = false;
                CacheGameplayCameraControls.updateRotationY = false;
                CacheGameplayCameraControls.updateRotation = InputManager.GetButton("CameraRotate");
                CacheGameplayCameraControls.updateZoom = !IsBlockController;
            }
            else
            {
                // Control camera by mouse-move
                Cursor.lockState = !IsBlockController ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = IsBlockController;
                CacheGameplayCameraControls.updateRotation = !IsBlockController;
                CacheGameplayCameraControls.updateZoom = !IsBlockController;
            }
            // Clear selected entity
            SelectedEntity = null;

            // Update crosshair (with states from last update)
            UpdateCrosshair();

            // Clear controlling states from last update
            isDoingAction = false;
            movementState = MovementState.None;
            extraMovementState = ExtraMovementState.None;
            UpdateActivatedWeaponAbility(tempDeltaTime);

            if (IsBlockController || GenericUtils.IsFocusInputField())
            {
                mustReleaseFireKey = false;

                PlayerCharacterEntity.KeyMovement(Vector3.zero, MovementState.None);
                DeactivateWeaponAbility();
                return;
            }

            // Prepare variables to find nearest raycasted hit point
            centerRay = CacheGameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            centerOriginToCharacterDistance = Vector3.Distance(centerRay.origin, MovementTransform.position);
            cameraForward = CacheGameplayCameraTransform.forward;
            cameraRight = CacheGameplayCameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Update look target and aim position
            if (ConstructingBuildingEntity == null)
                UpdateTarget_BattleMode();

            // Update movement and camera pitch
            UpdateMovementInputs();

            // Update aim position
            PlayerCharacterEntity.HasAimPosition = true;
            PlayerCharacterEntity.AimPosition = aimPosition;

            // Update input
            if (ConstructingBuildingEntity == null)
                UpdateInputs_BattleMode();
            else
                UpdateInputs_BuildMode();

            // Hide Npc UIs when move
            if (moveDirection.sqrMagnitude > 0f)
                HideNpcDialog();

            // If jumping add jump state
            if (InputManager.GetButtonDown("Jump"))
                movementState |= MovementState.IsJump;

            if (DetectExtraActive("Sprint", sprintActiveMode, ref toggleSprintOn))
            {
                extraMovementState = ExtraMovementState.IsSprinting;
                toggleCrouchOn = false;
                toggleCrawlOn = false;
            }
            else if (DetectExtraActive("Crouch", crouchActiveMode, ref toggleCrouchOn))
            {
                extraMovementState = ExtraMovementState.IsCrouching;
                toggleSprintOn = false;
                toggleCrawlOn = false;
            }
            else if (DetectExtraActive("Crawl", crawlActiveMode, ref toggleCrawlOn))
            {
                extraMovementState = ExtraMovementState.IsCrawling;
                toggleSprintOn = false;
                toggleCrouchOn = false;
            }

            PlayerCharacterEntity.KeyMovement(moveDirection, movementState);
            PlayerCharacterEntity.SetExtraMovement(extraMovementState);
            UpdateLookAtTarget();

            if (canSwitchViewMode && InputManager.GetButtonDown("SwitchViewMode"))
            {
                switch (ViewMode)
                {
                    case ControllerViewMode.Tps:
                        ViewMode = ControllerViewMode.Fps;
                        break;
                    case ControllerViewMode.Fps:
                        ViewMode = ControllerViewMode.Tps;
                        break;
                }
            }
        }

        private void LateUpdate()
        {
            if (PlayerCharacterEntity.MovementState.HasFlag(MovementState.IsUnderWater))
            {
                // Clear toggled sprint, crouch and crawl
                toggleSprintOn = false;
                toggleCrouchOn = false;
                toggleCrawlOn = false;
            }
            // Update inputs
            activateInput.OnLateUpdate();
            pickupItemInput.OnLateUpdate();
            reloadInput.OnLateUpdate();
            exitVehicleInput.OnLateUpdate();
            switchEquipWeaponSetInput.OnLateUpdate();
        }

        private bool DetectExtraActive(string key, ExtraMoveActiveMode activeMode, ref bool state)
        {
            switch (activeMode)
            {
                case ExtraMoveActiveMode.Hold:
                    state = InputManager.GetButton(key);
                    break;
                case ExtraMoveActiveMode.Toggle:
                    if (InputManager.GetButtonDown(key))
                        state = !state;
                    break;
            }
            return state;
        }

        private void UpdateTarget_BattleMode()
        {
            // Prepare raycast distance / fov
            float attackDistance = 0f;
            float attackFov = 90f;
            // Calculating aim distance, also read attack inputs here
            // Attack inputs will be used to calculate attack distance
            if (IsUsingHotkey())
            {
                mustReleaseFireKey = true;
            }
            else
            {
                // Attack with right hand weapon
                tempPressAttackRight = GetPrimaryAttackButton();
                if (WeaponAbility == null && leftHandWeapon != null)
                {
                    // Attack with left hand weapon if left hand weapon not empty
                    tempPressAttackLeft = GetSecondaryAttackButton();
                }
                else if (WeaponAbility != null)
                {
                    // Use weapon ability if it can
                    tempPressWeaponAbility = GetSecondaryAttackButtonDown();
                }

                if ((tempPressAttackRight || tempPressAttackLeft) &&
                    turningState == TurningState.None)
                {
                    // So priority is right > left
                    isLeftHandAttacking = !tempPressAttackRight && tempPressAttackLeft;
                }

                // Calculate aim distance by skill or weapon
                if (PlayerCharacterEntity.UsingSkill != null && PlayerCharacterEntity.UsingSkill.IsAttack())
                {
                    // Increase aim distance by skill attack distance
                    attackDistance = PlayerCharacterEntity.UsingSkill.GetCastDistance(PlayerCharacterEntity, PlayerCharacterEntity.UsingSkillLevel, isLeftHandAttacking);
                    attackFov = PlayerCharacterEntity.UsingSkill.GetCastFov(PlayerCharacterEntity, PlayerCharacterEntity.UsingSkillLevel, isLeftHandAttacking);
                }
                else if (queueUsingSkill.skill != null && queueUsingSkill.skill.IsAttack())
                {
                    // Increase aim distance by skill attack distance
                    attackDistance = queueUsingSkill.skill.GetCastDistance(PlayerCharacterEntity, queueUsingSkill.level, isLeftHandAttacking);
                    attackFov = queueUsingSkill.skill.GetCastFov(PlayerCharacterEntity, queueUsingSkill.level, isLeftHandAttacking);
                }
                else
                {
                    // Increase aim distance by attack distance
                    attackDistance = PlayerCharacterEntity.GetAttackDistance(isLeftHandAttacking);
                    attackFov = PlayerCharacterEntity.GetAttackFov(isLeftHandAttacking);
                }
            }
            // Default aim position (aim to sky/space)
            aimPosition = centerRay.origin + centerRay.direction * (centerOriginToCharacterDistance + attackDistance);
            // Raycast from camera position to center of screen
            int tempCount = PhysicUtils.SortedRaycastNonAlloc3D(centerRay.origin, centerRay.direction, raycasts, findTargetRaycastDistance, Physics.AllLayers);
            float tempDistance;
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempHitInfo = raycasts[tempCounter];

                // Get distance between character and raycast hit point
                tempDistance = Vector3.Distance(MovementTransform.position, tempHitInfo.point);
                // If this is damageable entity
                tempDamageableEntity = tempHitInfo.collider.GetComponent<IDamageableEntity>();
                if (tempDamageableEntity != null)
                {
                    tempEntity = tempDamageableEntity.Entity;

                    // Entity is in front of character, so this is target
                    if (turnForwardWhileDoingAction && !IsInFront(tempHitInfo.point))
                        continue;

                    // Target must be damageable, not player character entity, within aim distance and alive
                    if (tempDamageableEntity.GetObjectId() == PlayerCharacterEntity.ObjectId)
                        continue;

                    // Target must not hidding
                    if (tempDamageableEntity.Entity is BaseCharacterEntity &&
                        (tempDamageableEntity.Entity as BaseCharacterEntity).GetCaches().IsHide)
                        continue;

                    // Entity is in front of character, so this is target
                    aimPosition = tempHitInfo.point;
                    SelectedEntity = tempEntity;
                    break;
                }
                // Find item drop entity
                tempEntity = tempHitInfo.collider.GetComponent<ItemDropEntity>();
                if (tempEntity != null && tempDistance <= CurrentGameInstance.pickUpItemDistance)
                {
                    // Entity is in front of character, so this is target
                    if (!turnForwardWhileDoingAction || IsInFront(tempHitInfo.point))
                        aimPosition = tempHitInfo.point;
                    SelectedEntity = tempEntity;
                    break;
                }
                // Find activatable entity (NPC/Building/Mount/Etc)
                tempEntity = tempHitInfo.collider.GetComponent<BaseGameEntity>();
                if (tempEntity != null && tempDistance <= CurrentGameInstance.conversationDistance)
                {
                    // Entity is in front of character, so this is target
                    if (!turnForwardWhileDoingAction || IsInFront(tempHitInfo.point))
                        aimPosition = tempHitInfo.point;
                    SelectedEntity = tempEntity;
                    break;
                }
            }
            aimDirection = aimPosition - MovementTransform.position;
            aimDirection.y = 0f;
            aimDirection.Normalize();
            // Show target hp/mp
            CacheUISceneGameplay.SetTargetEntity(SelectedEntity);
            PlayerCharacterEntity.SetTargetEntity(SelectedEntity);
        }

        private void UpdateMovementInputs()
        {
            pitch = CacheGameplayCameraTransform.eulerAngles.x;

            // Update charcter pitch
            PlayerCharacterEntity.Pitch = pitch;

            // If mobile platforms, don't receive input raw to make it smooth
            bool raw = !InputManager.useMobileInputOnNonMobile && !Application.isMobilePlatform;
            moveDirection = Vector3.zero;
            inputV = InputManager.GetAxis("Vertical", raw);
            inputH = InputManager.GetAxis("Horizontal", raw);
            moveDirection += cameraForward * inputV;
            moveDirection += cameraRight * inputH;
            if (moveDirection.sqrMagnitude > 0f)
            {
                if (pitch > 180f)
                    pitch -= 360f;
                moveDirection.y = -pitch / 90f;
            }
            // Set movement state by inputs
            switch (Mode)
            {
                case ControllerMode.Adventure:
                    if (inputV > 0.5f || inputV < -0.5f || inputH > 0.5f || inputH < -0.5f)
                        movementState = MovementState.Forward;
                    moveLookDirection = moveDirection;
                    moveLookDirection.y = 0f;
                    break;
                case ControllerMode.Combat:
                    if (inputV > 0.5f)
                        movementState |= MovementState.Forward;
                    else if (inputV < -0.5f)
                        movementState |= MovementState.Backward;
                    if (inputH > 0.5f)
                        movementState |= MovementState.Right;
                    else if (inputH < -0.5f)
                        movementState |= MovementState.Left;
                    moveLookDirection = cameraForward;
                    break;
            }

            if (ViewMode == ControllerViewMode.Fps)
            {
                // Force turn to look direction
                moveLookDirection = cameraForward;
                targetLookDirection = cameraForward;
            }

            moveDirection.Normalize();
        }

        private void UpdateInputs_BattleMode()
        {
            // Have to release fire key, then check press fire key later on next frame
            if (mustReleaseFireKey)
            {
                tempPressAttackRight = false;
                tempPressAttackLeft = false;
                if (!isLeftHandAttacking &&
                    (GetPrimaryAttackButtonUp() ||
                    !GetPrimaryAttackButton()))
                    mustReleaseFireKey = false;
                if (isLeftHandAttacking &&
                    (GetSecondaryAttackButtonUp() ||
                    !GetSecondaryAttackButton()))
                    mustReleaseFireKey = false;
            }

            if (queueUsingSkill.skill != null ||
                tempPressAttackRight ||
                tempPressAttackLeft ||
                activateInput.IsPress ||
                activateInput.IsRelease ||
                activateInput.IsHold ||
                PlayerCharacterEntity.IsPlayingActionAnimation())
            {
                // Find forward character / npc / building / warp entity from camera center
                // Check is character playing action animation to turn character forwarding to aim position
                targetPlayer = null;
                targetNpc = null;
                targetBuilding = null;
                targetVehicle = null;
                if (!tempPressAttackRight && !tempPressAttackLeft)
                {
                    if (activateInput.IsHold)
                    {
                        if (SelectedEntity is BuildingEntity)
                            targetBuilding = SelectedEntity as BuildingEntity;
                    }
                    else if (activateInput.IsRelease)
                    {
                        if (SelectedEntity is BasePlayerCharacterEntity)
                            targetPlayer = SelectedEntity as BasePlayerCharacterEntity;
                        if (SelectedEntity is NpcEntity)
                            targetNpc = SelectedEntity as NpcEntity;
                        if (SelectedEntity is BuildingEntity)
                            targetBuilding = SelectedEntity as BuildingEntity;
                        if (SelectedEntity is VehicleEntity)
                            targetVehicle = SelectedEntity as VehicleEntity;
                    }
                }
                // While attacking turn character to aim direction
                tempCalculateAngle = Vector3.Angle(MovementTransform.forward, aimDirection);

                if (PlayerCharacterEntity.IsPlayingActionAnimation())
                {
                    // Just look at camera forward while character playing action animation
                    switch (ViewMode)
                    {
                        case ControllerViewMode.Fps:
                            targetLookDirection = cameraForward;
                            break;
                        case ControllerViewMode.Tps:
                            targetLookDirection = turnForwardWhileDoingAction ? cameraForward : aimDirection;
                            break;
                    }
                }
                else if (tempCalculateAngle > 15f && ViewMode == ControllerViewMode.Tps)
                {
                    // Fps mode character always turn to camera forward.
                    // So set turning state for Tps view mode only
                    if (queueUsingSkill.skill != null && queueUsingSkill.skill.IsAttack())
                    {
                        turningState = TurningState.UseSkill;
                    }
                    else if (tempPressAttackRight || tempPressAttackLeft)
                    {
                        turningState = TurningState.Attack;
                    }
                    else if (activateInput.IsPress)
                    {
                        turningState = TurningState.None;
                    }
                    else if (activateInput.IsRelease)
                    {
                        turningState = TurningState.Activate;
                    }
                    // Calculate turn duration to smoothing character rotation in `UpdateLookAtTarget()`
                    calculatedTurnDuration = (180f - tempCalculateAngle) / 180f * turnToTargetDuration;
                    targetLookDirection = turnForwardWhileDoingAction ? cameraForward : aimDirection;
                    // Set movement state by inputs
                    if (inputV > 0.5f)
                        movementState |= MovementState.Forward;
                    else if (inputV < -0.5f)
                        movementState |= MovementState.Backward;
                    if (inputH > 0.5f)
                        movementState |= MovementState.Right;
                    else if (inputH < -0.5f)
                        movementState |= MovementState.Left;
                }
                else
                {
                    // Attack immediately if character already look at target
                    if (queueUsingSkill.skill != null && queueUsingSkill.skill.IsAttack())
                    {
                        UseSkill(isLeftHandAttacking, aimPosition);
                        isDoingAction = true;
                    }
                    else if (tempPressAttackRight || tempPressAttackLeft)
                    {
                        Attack(isLeftHandAttacking);
                        isDoingAction = true;
                    }
                    else if (activateInput.IsHold)
                    {
                        HoldActivate();
                    }
                    else if (activateInput.IsRelease)
                    {
                        Activate();
                    }
                }

                // If skill is not attack skill, use it immediately
                if (queueUsingSkill.skill != null && !queueUsingSkill.skill.IsAttack())
                {
                    UseSkill(isLeftHandAttacking, aimPosition);
                }
            }
            else if (tempPressWeaponAbility)
            {
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
            else if (pickupItemInput.IsPress)
            {
                // If target is entity with lootbag, open it
                if (SelectedEntity != null && SelectedEntity is BaseCharacterEntity)
                {
                    BaseCharacterEntity c = SelectedEntity as BaseCharacterEntity;
                    if (c != null && c.IsDead() && c.useLootBag)
                        (CacheUISceneGameplay as UISceneGameplay).OnShowLootBag(c);
                }
                // Otherwise find item to pick up
                else if (SelectedEntity != null && SelectedEntity is ItemDropEntity)
                    PlayerCharacterEntity.RequestPickupItem(SelectedEntity.ObjectId);
            }
            else if (reloadInput.IsPress)
            {
                // Reload ammo when press the button
                ReloadAmmo();
            }
            else if (exitVehicleInput.IsPress)
            {
                // Exit vehicle
                PlayerCharacterEntity.RequestExitVehicle();
            }
            else if (switchEquipWeaponSetInput.IsPress)
            {
                // Switch equip weapon set
                PlayerCharacterEntity.RequestSwitchEquipWeaponSet((byte)(PlayerCharacterEntity.EquipWeaponSet + 1));
            }
            else
            {
                // Update move direction
                if (moveDirection.sqrMagnitude > 0f && ViewMode == ControllerViewMode.Tps)
                    targetLookDirection = moveLookDirection;
            }

            // Setup releasing state
            if (tempPressAttackRight && rightHandWeapon != null && rightHandWeapon.FireType == FireType.SingleFire)
            {
                // The weapon's fire mode is single fire, so player have to release fire key for next fire
                mustReleaseFireKey = true;
            }
            else if (tempPressAttackLeft && leftHandWeapon != null && leftHandWeapon.FireType == FireType.SingleFire)
            {
                // The weapon's fire mode is single fire, so player have to release fire key for next fire
                mustReleaseFireKey = true;
            }

            // Auto reload
            if (!tempPressAttackRight && !tempPressAttackLeft && !reloadInput.IsPress &&
                (PlayerCharacterEntity.EquipWeapons.rightHand.IsAmmoEmpty() ||
                PlayerCharacterEntity.EquipWeapons.leftHand.IsAmmoEmpty()))
            {
                // Reload ammo when empty and not press any keys
                ReloadAmmo();
            }
        }

        private void UpdateInputs_BuildMode()
        {
            // Update move direction
            if (moveDirection.sqrMagnitude > 0f && ViewMode == ControllerViewMode.Tps)
                targetLookDirection = moveLookDirection;
        }

        private void ReloadAmmo()
        {
            // Reload ammo at server
            if (!PlayerCharacterEntity.EquipWeapons.rightHand.IsAmmoFull())
                PlayerCharacterEntity.RequestReload(false);
            else if (!PlayerCharacterEntity.EquipWeapons.leftHand.IsAmmoFull())
                PlayerCharacterEntity.RequestReload(true);
        }

        private void UpdateCrosshair()
        {
            if (isDoingAction)
            {
                UpdateCrosshair(CurrentCrosshairSetting, CurrentCrosshairSetting.expandPerFrameWhileAttacking);
            }
            else if (movementState.HasFlag(MovementState.Forward) ||
                movementState.HasFlag(MovementState.Backward) ||
                movementState.HasFlag(MovementState.Left) ||
                movementState.HasFlag(MovementState.Right) ||
                movementState.HasFlag(MovementState.IsJump))
            {
                UpdateCrosshair(CurrentCrosshairSetting, CurrentCrosshairSetting.expandPerFrameWhileMoving);
            }
            else
            {
                UpdateCrosshair(CurrentCrosshairSetting, -CurrentCrosshairSetting.shrinkPerFrame);
            }
        }

        private void UpdateCrosshair(CrosshairSetting setting, float power)
        {
            if (crosshairRect == null)
                return;

            crosshairRect.gameObject.SetActive(!setting.hidden);
            // Change crosshair size by power
            Vector3 sizeDelta = crosshairRect.sizeDelta;
            sizeDelta.x += power;
            sizeDelta.y += power;
            CurrentCrosshairSize = sizeDelta;
            // Set crosshair size
            crosshairRect.sizeDelta = new Vector2(Mathf.Clamp(CurrentCrosshairSize.x, setting.minSpread, setting.maxSpread), Mathf.Clamp(CurrentCrosshairSize.y, setting.minSpread, setting.maxSpread));
        }

        protected void UpdateLookAtTarget()
        {
            if (ViewMode == ControllerViewMode.Tps)
            {
                if (PlayerCharacterEntity.IsPlayingActionAnimation())
                {
                    // Turn character to look direction immediately
                    // If character playing action animation
                    tempLookAt = Quaternion.LookRotation(targetLookDirection);
                    PlayerCharacterEntity.SetLookRotation(tempLookAt);
                    return;
                }
                tempCalculateAngle = Vector3.Angle(tempLookAt * Vector3.forward, targetLookDirection);
                if (turningState != TurningState.None)
                {
                    if (tempCalculateAngle > 0)
                    {
                        // Update rotation when angle difference more than 0
                        tempLookAt = Quaternion.Slerp(tempLookAt, Quaternion.LookRotation(targetLookDirection), calculatedTurnDuration / turnToTargetDuration);
                        PlayerCharacterEntity.SetLookRotation(tempLookAt);
                    }
                    else
                    {
                        // Update temp look at to character's rotation
                        tempLookAt = PlayerCharacterEntity.GetLookRotation();
                        // Do actions
                        switch (turningState)
                        {
                            case TurningState.Attack:
                                Attack(isLeftHandAttacking);
                                break;
                            case TurningState.Activate:
                                Activate();
                                break;
                            case TurningState.UseSkill:
                                UseSkill(isLeftHandAttacking, aimPosition);
                                break;
                        }
                        turningState = TurningState.None;
                    }
                }
                else
                {
                    if (tempCalculateAngle > 0)
                    {
                        // Update rotation when angle difference more than 0
                        tempLookAt = Quaternion.RotateTowards(tempLookAt, Quaternion.LookRotation(targetLookDirection), Time.deltaTime * angularSpeed);
                        PlayerCharacterEntity.SetLookRotation(tempLookAt);
                    }
                    else
                    {
                        // Update temp look at to character's rotation
                        tempLookAt = PlayerCharacterEntity.GetLookRotation();
                    }
                }
            }
            else if (ViewMode == ControllerViewMode.Fps)
            {
                // Turn character to look direction immediately
                tempLookAt = Quaternion.LookRotation(targetLookDirection);
                PlayerCharacterEntity.SetLookRotation(tempLookAt);
            }
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
                SetQueueUsingSkill(aimPosition, (item as ISkillItem).UsingSkill, (item as ISkillItem).UsingSkillLevel, (short)itemIndex);
            }
            else if (item.IsBuilding())
            {
                buildingItemIndex = itemIndex;
                if (showConfirmConstructionUI)
                {
                    // Show confirm UI
                    ShowConstructBuildingDialog();
                }
                else
                {
                    // Build when click
                    ConfirmBuild();
                }
                mustReleaseFireKey = true;
            }
            else if (item.IsUsable())
            {
                PlayerCharacterEntity.RequestUseItem((short)itemIndex);
            }
        }

        public void Attack(bool isLeftHand)
        {
            PlayerCharacterEntity.RequestAttack(isLeftHand);
        }

        public void ActivateWeaponAbility()
        {
            if (WeaponAbility == null)
                return;

            if (WeaponAbilityState == WeaponAbilityState.Activated ||
                WeaponAbilityState == WeaponAbilityState.Activating)
                return;

            WeaponAbility.OnPreActivate();
            WeaponAbilityState = WeaponAbilityState.Activating;
        }

        private void UpdateActivatedWeaponAbility(float deltaTime)
        {
            if (WeaponAbility == null)
                return;

            if (WeaponAbilityState == WeaponAbilityState.Activated ||
                WeaponAbilityState == WeaponAbilityState.Deactivated)
                return;

            WeaponAbilityState = WeaponAbility.UpdateActivation(WeaponAbilityState, deltaTime);
        }

        private void DeactivateWeaponAbility()
        {
            if (WeaponAbility == null)
                return;

            if (WeaponAbilityState == WeaponAbilityState.Deactivated ||
                WeaponAbilityState == WeaponAbilityState.Deactivating)
                return;

            WeaponAbility.OnPreDeactivate();
            WeaponAbilityState = WeaponAbilityState.Deactivating;
        }

        public void HoldActivate()
        {
            if (targetBuilding != null)
            {
                TargetEntity = targetBuilding;
                ShowCurrentBuildingDialog();
            }
        }

        public void Activate()
        {
            // Priority Player -> Npc -> Buildings
            if (targetPlayer != null && CacheUISceneGameplay != null)
                CacheUISceneGameplay.SetActivePlayerCharacter(targetPlayer);
            else if (targetNpc != null)
                PlayerCharacterEntity.RequestNpcActivate(targetNpc.ObjectId);
            else if (targetBuilding != null)
                ActivateBuilding(targetBuilding);
            else if (targetVehicle != null)
                PlayerCharacterEntity.RequestEnterVehicle(targetVehicle.ObjectId);
        }

        public void UseSkill(bool isLeftHand, Vector3 defaultAimPosition)
        {
            if (queueUsingSkill.skill != null)
            {
                if (queueUsingSkill.itemIndex >= 0)
                {
                    if (queueUsingSkill.aimPosition.HasValue)
                        PlayerCharacterEntity.RequestUseSkillItem(queueUsingSkill.itemIndex, isLeftHand, queueUsingSkill.aimPosition.Value);
                    else
                        PlayerCharacterEntity.RequestUseSkillItem(queueUsingSkill.itemIndex, isLeftHand);
                }
                else
                {
                    if (queueUsingSkill.aimPosition.HasValue)
                        PlayerCharacterEntity.RequestUseSkill(queueUsingSkill.skill.DataId, isLeftHand, queueUsingSkill.aimPosition.Value);
                    else
                        PlayerCharacterEntity.RequestUseSkill(queueUsingSkill.skill.DataId, isLeftHand);
                }
            }
            ClearQueueUsingSkill();
        }

        public int OverlapObjects(Vector3 position, float distance, int layerMask)
        {
            return Physics.OverlapSphereNonAlloc(position, distance, overlapColliders, layerMask);
        }

        public bool FindTarget(GameObject target, float actDistance, int layerMask)
        {
            int tempCount = OverlapObjects(MovementTransform.position, actDistance, layerMask);
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempGameObject = overlapColliders[tempCounter].gameObject;
                if (tempGameObject == target)
                    return true;
            }
            return false;
        }

        public bool IsUsingHotkey()
        {
            // Check using hotkey for PC only
            if (!InputManager.useMobileInputOnNonMobile &&
                !Application.isMobilePlatform &&
                UICharacterHotkeys.UsingHotkey != null)
            {
                return true;
            }
            return false;
        }

        public bool GetPrimaryAttackButton()
        {
            return InputManager.GetButton("Fire1") || InputManager.GetButton("Attack");
        }

        public bool GetSecondaryAttackButton()
        {
            return InputManager.GetButton("Fire2");
        }

        public bool GetPrimaryAttackButtonUp()
        {
            return InputManager.GetButtonUp("Fire1") || InputManager.GetButtonUp("Attack");
        }

        public bool GetSecondaryAttackButtonUp()
        {
            return InputManager.GetButtonUp("Fire2");
        }

        public bool GetPrimaryAttackButtonDown()
        {
            return InputManager.GetButtonDown("Fire1") || InputManager.GetButtonDown("Attack");
        }

        public bool GetSecondaryAttackButtonDown()
        {
            return InputManager.GetButtonDown("Fire2");
        }

        public void SetActiveCrosshair(bool isActive)
        {
            if (crosshairRect != null &&
                crosshairRect.gameObject.activeSelf != isActive)
            {
                // Hide crosshair when not active
                crosshairRect.gameObject.SetActive(isActive);
            }
        }

        public void UpdateCameraSettings()
        {
            CacheGameplayCamera.fieldOfView = CameraFov;
            CacheGameplayCamera.nearClipPlane = CameraNearClipPlane;
            CacheGameplayCamera.farClipPlane = CameraFarClipPlane;
            CacheGameplayCameraControls.targetOffset = CameraTargetOffset;
            CacheGameplayCameraControls.zoomDistance = CameraZoomDistance;
            CacheGameplayCameraControls.minZoomDistance = CameraMinZoomDistance;
            CacheGameplayCameraControls.maxZoomDistance = CameraMaxZoomDistance;
            CacheGameplayCameraControls.enableWallHitSpring = viewMode == ControllerViewMode.Tps ? true : false;
            PlayerCharacterEntity.ModelManager.SetIsFps(viewMode == ControllerViewMode.Fps);
        }

        public bool IsInFront(Vector3 position)
        {
            return Vector3.Angle(cameraForward, MovementTransform.position - position) > 135f;
        }

        public override Vector3? UpdateBuildAimControls(Vector2 aimAxes, BuildingEntity prefab)
        {
            // Instantiate constructing building
            if (ConstructingBuildingEntity == null)
            {
                InstantiateConstructingBuilding(prefab);
                buildYRotate = 0f;
            }
            // Rotate by keys
            if (InputManager.GetButtonDown("RotateLeft"))
                buildYRotate -= buildRotateAngle;
            else if (InputManager.GetButtonDown("RotateRight"))
                buildYRotate += buildRotateAngle;
            // Clear area before next find
            ConstructingBuildingEntity.BuildingArea = null;
            // Default aim position (aim to sky/space)
            aimPosition = centerRay.origin + centerRay.direction * (centerOriginToCharacterDistance + ConstructingBuildingEntity.buildDistance);
            // Raycast from camera position to center of screen
            int tempCount = PhysicUtils.SortedRaycastNonAlloc3D(centerRay.origin, centerRay.direction, raycasts, 100f, CurrentGameInstance.GetBuildLayerMask());
            float tempDistance;
            BuildingArea buildingArea;
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempHitInfo = raycasts[tempCounter];

                // Set aim position
                tempDistance = Vector3.Distance(CacheGameplayCameraTransform.position, tempHitInfo.point);
                if (!IsInFront(tempHitInfo.point))
                {
                    // Skip because this position is not allowed to build the building
                    continue;
                }

                aimPosition = tempHitInfo.point;
                buildingArea = tempHitInfo.transform.GetComponent<BuildingArea>();
                if (buildingArea == null ||
                    (buildingArea.Entity && buildingArea.GetObjectId() == ConstructingBuildingEntity.ObjectId) ||
                    !ConstructingBuildingEntity.buildingTypes.Contains(buildingArea.buildingType))
                {
                    // Skip because this area is not allowed to build the building
                    continue;
                }

                ConstructingBuildingEntity.BuildingArea = buildingArea;
                if (!buildingArea.snapBuildingObject)
                {
                    // There is no snap build position, set building rotation by camera look direction
                    ConstructingBuildingEntity.CacheTransform.position = GameplayUtils.ClampPosition(MovementTransform.position, aimPosition, ConstructingBuildingEntity.buildDistance);
                    // Rotate to camera
                    Vector3 direction = aimPosition - CacheGameplayCameraTransform.position;
                    direction.y = 0f;
                    direction.Normalize();
                    ConstructingBuildingEntity.CacheTransform.eulerAngles = Quaternion.LookRotation(direction).eulerAngles + (Vector3.up * buildYRotate);
                }
                break;
            }

            if (Vector3.Distance(PlayerCharacterEntity.CacheTransform.position, aimPosition) > ConstructingBuildingEntity.buildDistance)
            {
                // Mark as unable to build when the building is far from character
                ConstructingBuildingEntity.BuildingArea = null;
            }

            return ConstructingBuildingEntity.Position;
        }

        public override void FinishBuildAimControls(bool isCancel)
        {
            if (isCancel)
                CancelBuild();
        }
    }
}
