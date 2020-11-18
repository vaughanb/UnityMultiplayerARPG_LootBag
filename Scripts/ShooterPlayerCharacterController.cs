using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class ShooterPlayerCharacterController : BasePlayerCharacterController, IShooterWeaponController, IWeaponAbilityController, IAimAssistAvoidanceListener
    {
        public enum ControllerMode
        {
            Adventure,
            Combat,
        }

        public enum ExtraMoveActiveMode
        {
            None,
            Toggle,
            Hold
        }

        [Header("Camera Controls Prefabs")]
        [SerializeField]
        private FollowCameraControls gameplayCameraPrefab;
        [SerializeField]
        private FollowCameraControls minimapCameraPrefab;

        [Header("Controller Settings")]
        [SerializeField]
        private ControllerMode mode;
        [SerializeField]
        private bool canSwitchViewMode;
        [SerializeField]
        private ShooterControllerViewMode viewMode;
        [SerializeField]
        private ExtraMoveActiveMode sprintActiveMode;
        [SerializeField]
        private ExtraMoveActiveMode crouchActiveMode;
        [SerializeField]
        private ExtraMoveActiveMode crawlActiveMode;
        [SerializeField]
        private bool unToggleCrouchWhenJump;
        [SerializeField]
        private bool unToggleCrawlWhenJump;
        [SerializeField]
        private float findTargetRaycastDistance = 16f;
        [SerializeField]
        private bool showConfirmConstructionUI = false;
        [SerializeField]
        private bool clampBuildPositionByBuildDistance = false;
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
        private Vector3 tpsTargetOffsetWhileCrouching = new Vector3(0.75f, 0.75f, 0f);
        [SerializeField]
        private Vector3 tpsTargetOffsetWhileCrawling = new Vector3(0.75f, 0.5f, 0f);
        [SerializeField]
        private float tpsFov = 60f;
        [SerializeField]
        private float tpsNearClipPlane = 0.3f;
        [SerializeField]
        private float tpsFarClipPlane = 1000f;
        [SerializeField]
        private bool turnForwardWhileDoingAction = true;
        [SerializeField]
        [Tooltip("Use this to turn character smoothly, Set this <= 0 to turn immediately")]
        private float turnSpeed = 0f;
        [SerializeField]
        [Tooltip("Use this to turn character smoothly, Set this <= 0 to turn immediately")]
        private float turnSpeedWhileSprinting = 0f;
        [SerializeField]
        [Tooltip("Use this to turn character smoothly, Set this <= 0 to turn immediately")]
        private float turnSpeedWhileCrouching = 0f;
        [SerializeField]
        [Tooltip("Use this to turn character smoothly, Set this <= 0 to turn immediately")]
        private float turnSpeedWileCrawling = 0f;
        [SerializeField]
        [Tooltip("Use this to turn character smoothly, Set this <= 0 to turn immediately")]
        private float turnSpeedWileSwimming = 0f;
        [SerializeField]
        [Tooltip("Use this to turn character smoothly, Set this <= 0 to turn immediately")]
        private float turnSpeedWileDoingAction = 0f;

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

        [Header("Aim Assist Settings")]
        [SerializeField]
        private bool enableAimAssist = false;
        [SerializeField]
        private bool enableAimAssistX = false;
        [SerializeField]
        private bool enableAimAssistY = true;
        [SerializeField]
        private bool aimAssistOnFireOnly = true;
        [SerializeField]
        private float aimAssistRadius = 0.5f;
        [SerializeField]
        private float aimAssistXSpeed = 20f;
        [SerializeField]
        private float aimAssistYSpeed = 20f;
        [SerializeField]
        private bool aimAssistCharacter = true;
        [SerializeField]
        private bool aimAssistBuilding = false;
        [SerializeField]
        private bool aimAssistHarvestable = false;

        [Header("Recoil Settings")]
        [SerializeField]
        private float recoilRateWhileMoving = 1.5f;
        [SerializeField]
        private float recoilRateWhileSprinting = 2f;
        [SerializeField]
        private float recoilRateWhileCrouching = 0.5f;
        [SerializeField]
        private float recoilRateWhileCrawling = 0.5f;
        [SerializeField]
        private float recoilRateWhileSwimming = 0.5f;

        public bool IsBlockController { get; private set; }
        public FollowCameraControls CacheGameplayCameraControls { get; private set; }
        public FollowCameraControls CacheMinimapCameraControls { get; private set; }
        public Camera CacheGameplayCamera { get { return CacheGameplayCameraControls.CacheCamera; } }
        public Camera CacheMiniMapCamera { get { return CacheMinimapCameraControls.CacheCamera; } }
        public Transform CacheGameplayCameraTransform { get { return CacheGameplayCameraControls.CacheCameraTransform; } }
        public Transform CacheMiniMapCameraTransform { get { return CacheMinimapCameraControls.CacheCameraTransform; } }
        public Vector2 CurrentCrosshairSize { get; private set; }
        public CrosshairSetting CurrentCrosshairSetting { get; private set; }
        public BaseWeaponAbility WeaponAbility { get; private set; }
        public WeaponAbilityState WeaponAbilityState { get; private set; }

        public ControllerMode Mode
        {
            get
            {
                if (viewMode == ShooterControllerViewMode.Fps)
                {
                    // If view mode is fps, controls type must be combat
                    return ControllerMode.Combat;
                }
                return mode;
            }
        }

        public ShooterControllerViewMode ViewMode
        {
            get { return viewMode; }
            set { viewMode = value; }
        }

        public float CameraZoomDistance
        {
            get { return ViewMode == ShooterControllerViewMode.Tps ? tpsZoomDistance : fpsZoomDistance; }
        }

        public float CameraMinZoomDistance
        {
            get { return ViewMode == ShooterControllerViewMode.Tps ? tpsMinZoomDistance : fpsZoomDistance; }
        }

        public float CameraMaxZoomDistance
        {
            get { return ViewMode == ShooterControllerViewMode.Tps ? tpsMaxZoomDistance : fpsZoomDistance; }
        }

        public Vector3 CameraTargetOffset
        {
            get
            {
                if (ViewMode == ShooterControllerViewMode.Tps)
                {
                    if (PlayerCharacterEntity.ExtraMovementState.HasFlag(ExtraMovementState.IsCrouching))
                    {
                        return tpsTargetOffsetWhileCrouching;
                    }
                    else if (PlayerCharacterEntity.ExtraMovementState.HasFlag(ExtraMovementState.IsCrawling))
                    {
                        return tpsTargetOffsetWhileCrawling;
                    }
                    else
                    {
                        return tpsTargetOffset;
                    }
                }
                return fpsTargetOffset;
            }
        }

        public float CameraFov
        {
            get { return ViewMode == ShooterControllerViewMode.Tps ? tpsFov : fpsFov; }
        }

        public float CameraNearClipPlane
        {
            get { return ViewMode == ShooterControllerViewMode.Tps ? tpsNearClipPlane : fpsNearClipPlane; }
        }

        public float CameraFarClipPlane
        {
            get { return ViewMode == ShooterControllerViewMode.Tps ? tpsFarClipPlane : fpsFarClipPlane; }
        }

        public float CurrentCameraFov
        {
            get { return CacheGameplayCamera.fieldOfView; }
            set { CacheGameplayCamera.fieldOfView = value; }
        }

        public float RotationSpeedScale
        {
            get { return CacheGameplayCameraControls.rotationSpeedScale; }
            set { CacheGameplayCameraControls.rotationSpeedScale = value; }
        }

        public bool HideCrosshair { get; set; }

        public float CurrentTurnSpeed
        {
            get
            {
                if (PlayerCharacterEntity.MovementState.HasFlag(MovementState.IsUnderWater))
                    return turnSpeedWileSwimming;
                switch (PlayerCharacterEntity.ExtraMovementState)
                {
                    case ExtraMovementState.IsSprinting:
                        return turnSpeedWhileSprinting;
                    case ExtraMovementState.IsCrouching:
                        return turnSpeedWhileCrouching;
                    case ExtraMovementState.IsCrawling:
                        return turnSpeedWileCrawling;
                }
                return turnSpeed;
            }
        }

        // Input data
        InputStateManager activateInput;
        InputStateManager pickupItemInput;
        InputStateManager reloadInput;
        InputStateManager exitVehicleInput;
        InputStateManager switchEquipWeaponSetInput;
        // Entity detector
        NearbyEntityDetector warpPortalEntityDetector;
        // Temp physic variables
        RaycastHit[] raycasts = new RaycastHit[512];
        Collider[] overlapColliders = new Collider[512];
        RaycastHit tempHitInfo;
        // Temp target
        BasePlayerCharacterEntity targetPlayer;
        NpcEntity targetNpc;
        BuildingEntity targetBuilding;
        VehicleEntity targetVehicle;
        WarpPortalEntity targetWarpPortal;
        // Temp data
        IGameEntity tempGameEntity;
        Ray centerRay;
        float centerOriginToCharacterDistance;
        Vector3 moveDirection;
        Vector3 cameraForward;
        Vector3 cameraRight;
        float inputV;
        float inputH;
        Vector2 normalizedInput;
        Vector3 moveLookDirection;
        Vector3 targetLookDirection;
        float tempDeltaTime;
        bool tempPressAttackRight;
        bool tempPressAttackLeft;
        bool tempPressWeaponAbility;
        bool isLeftHandAttacking;
        float pitch;
        Vector3 aimPosition;
        Vector3 aimDirection;
        bool toggleSprintOn;
        bool toggleCrouchOn;
        bool toggleCrawlOn;
        // Controlling states
        ShooterControllerViewMode dirtyViewMode;
        IWeaponItem rightHandWeapon;
        IWeaponItem leftHandWeapon;
        MovementState movementState;
        ExtraMovementState extraMovementState;
        ShooterControllerViewMode? viewModeBeforeDead;
        bool isDoingAction;
        bool mustReleaseFireKey;
        float buildYRotate;

        protected override void Awake()
        {
            base.Awake();
            if (gameplayCameraPrefab != null)
                CacheGameplayCameraControls = Instantiate(gameplayCameraPrefab);
            if (minimapCameraPrefab != null)
                CacheMinimapCameraControls = Instantiate(minimapCameraPrefab);
            buildingItemIndex = -1;
            isLeftHandAttacking = false;
            ConstructingBuildingEntity = null;
            activateInput = new InputStateManager("Activate");
            pickupItemInput = new InputStateManager("PickUpItem");
            reloadInput = new InputStateManager("Reload");
            exitVehicleInput = new InputStateManager("ExitVehicle");
            switchEquipWeaponSetInput = new InputStateManager("SwitchEquipWeaponSet");
            // Initialize warp portal entity detector
            GameObject tempGameObject = new GameObject("_WarpPortalEntityDetector");
            warpPortalEntityDetector = tempGameObject.AddComponent<NearbyEntityDetector>();
            warpPortalEntityDetector.detectingRadius = CurrentGameInstance.conversationDistance;
            warpPortalEntityDetector.findWarpPortal = true;
        }

        protected override void Setup(BasePlayerCharacterEntity characterEntity)
        {
            base.Setup(characterEntity);

            if (characterEntity == null)
                return;

            targetLookDirection = MovementTransform.forward;
            SetupEquipWeapons(characterEntity.EquipWeapons);
            characterEntity.onEquipWeaponSetChange += SetupEquipWeapons;
            characterEntity.onSelectableWeaponSetsOperation += SetupEquipWeapons;
            characterEntity.onAttackRoutine += OnAttackRoutine;
            characterEntity.ModelManager.InstantiateFpsModel(CacheGameplayCameraTransform);
            characterEntity.ModelManager.SetIsFps(ViewMode == ShooterControllerViewMode.Fps);
            CacheGameplayCameraControls.startYRotation = characterEntity.CurrentRotation.y;
            UpdateCameraSettings();
        }

        protected override void Desetup(BasePlayerCharacterEntity characterEntity)
        {
            base.Desetup(characterEntity);

            if (CacheGameplayCameraControls != null)
                CacheGameplayCameraControls.target = null;

            if (CacheMinimapCameraControls != null)
                CacheMinimapCameraControls.target = null;

            if (characterEntity == null)
                return;

            characterEntity.onEquipWeaponSetChange -= SetupEquipWeapons;
            characterEntity.onSelectableWeaponSetsOperation -= SetupEquipWeapons;
            characterEntity.onAttackRoutine -= OnAttackRoutine;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (CacheGameplayCameraControls != null)
                Destroy(CacheGameplayCameraControls.gameObject);
            if (CacheMinimapCameraControls != null)
                Destroy(CacheMinimapCameraControls.gameObject);
            if (warpPortalEntityDetector != null)
                Destroy(warpPortalEntityDetector.gameObject);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void SetupEquipWeapons(byte equipWeaponSet)
        {
            SetupEquipWeapons(PlayerCharacterEntity.EquipWeapons);
        }

        private void SetupEquipWeapons(LiteNetLibManager.LiteNetLibSyncList.Operation operation, int index)
        {
            SetupEquipWeapons(PlayerCharacterEntity.EquipWeapons);
        }

        private void SetupEquipWeapons(EquipWeapons equipWeapons)
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

        protected override void Update()
        {
            if (PlayerCharacterEntity == null || !PlayerCharacterEntity.IsOwnerClient)
                return;

            if (CacheGameplayCameraControls != null)
                CacheGameplayCameraControls.target = CameraTargetTransform;

            if (CacheMinimapCameraControls != null)
                CacheMinimapCameraControls.target = CameraTargetTransform;

            if (PlayerCharacterEntity.IsDead())
            {
                // Deactivate weapon ability immediately when dead
                if (WeaponAbility != null && WeaponAbilityState != WeaponAbilityState.Deactivated)
                {
                    WeaponAbility.ForceDeactivated();
                    WeaponAbilityState = WeaponAbilityState.Deactivated;
                }
                // Set view mode to TPS when character dead
                if (!viewModeBeforeDead.HasValue)
                    viewModeBeforeDead = ViewMode;
                ViewMode = ShooterControllerViewMode.Tps;
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
                // Update camera zoom distance when change view mode only, to allow zoom controls
                CacheGameplayCameraControls.zoomDistance = CameraZoomDistance;
                CacheGameplayCameraControls.minZoomDistance = CameraMinZoomDistance;
                CacheGameplayCameraControls.maxZoomDistance = CameraMaxZoomDistance;
            }
            CacheGameplayCameraControls.targetOffset = CameraTargetOffset;
            CacheGameplayCameraControls.enableWallHitSpring = viewMode == ShooterControllerViewMode.Tps ? true : false;
            CacheGameplayCameraControls.target = ViewMode == ShooterControllerViewMode.Fps ? PlayerCharacterEntity.FpsCameraTargetTransform : PlayerCharacterEntity.CameraTargetTransform;

            // Set temp data
            tempDeltaTime = Time.deltaTime;

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
            centerOriginToCharacterDistance = Vector3.Distance(centerRay.origin, CacheTransform.position);
            cameraForward = CacheGameplayCameraTransform.forward;
            cameraRight = CacheGameplayCameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Update look target and aim position
            if (ConstructingBuildingEntity == null)
                UpdateTarget_BattleMode();
            else
                UpdateTarget_BuildMode();

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
            {
                if (unToggleCrouchWhenJump && PlayerCharacterEntity.ExtraMovementState.HasFlag(ExtraMovementState.IsCrouching))
                    toggleCrouchOn = false;
                else if (unToggleCrawlWhenJump && PlayerCharacterEntity.ExtraMovementState.HasFlag(ExtraMovementState.IsCrawling))
                    toggleCrawlOn = false;
                else
                    movementState |= MovementState.IsJump;
            }
            else if (PlayerCharacterEntity.MovementState.HasFlag(MovementState.IsGrounded))
            {
                if (DetectExtraActive("Sprint", sprintActiveMode, ref toggleSprintOn))
                {
                    extraMovementState = ExtraMovementState.IsSprinting;
                    toggleCrouchOn = false;
                    toggleCrawlOn = false;
                }
                if (DetectExtraActive("Crouch", crouchActiveMode, ref toggleCrouchOn))
                {
                    extraMovementState = ExtraMovementState.IsCrouching;
                    toggleSprintOn = false;
                    toggleCrawlOn = false;
                }
                if (DetectExtraActive("Crawl", crawlActiveMode, ref toggleCrawlOn))
                {
                    extraMovementState = ExtraMovementState.IsCrawling;
                    toggleSprintOn = false;
                    toggleCrouchOn = false;
                }
            }

            PlayerCharacterEntity.KeyMovement(moveDirection, movementState);
            PlayerCharacterEntity.SetExtraMovement(extraMovementState);
            UpdateLookAtTarget();

            if (canSwitchViewMode && InputManager.GetButtonDown("SwitchViewMode"))
            {
                switch (ViewMode)
                {
                    case ShooterControllerViewMode.Tps:
                        ViewMode = ShooterControllerViewMode.Fps;
                        break;
                    case ShooterControllerViewMode.Fps:
                        ViewMode = ShooterControllerViewMode.Tps;
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
            bool attacking = false;
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

                attacking = tempPressAttackRight || tempPressAttackLeft;
                if (attacking && !PlayerCharacterEntity.IsAttackingOrUsingSkill)
                {
                    // Priority is right > left
                    isLeftHandAttacking = !tempPressAttackRight && tempPressAttackLeft;
                }

                // Calculate aim distance by skill or weapon
                if (PlayerCharacterEntity.UsingSkill != null && PlayerCharacterEntity.UsingSkill.IsAttack())
                {
                    // Increase aim distance by skill attack distance
                    attackDistance = PlayerCharacterEntity.UsingSkill.GetCastDistance(PlayerCharacterEntity, PlayerCharacterEntity.UsingSkillLevel, isLeftHandAttacking);
                    attacking = true;
                }
                else if (queueUsingSkill.skill != null && queueUsingSkill.skill.IsAttack())
                {
                    // Increase aim distance by skill attack distance
                    attackDistance = queueUsingSkill.skill.GetCastDistance(PlayerCharacterEntity, queueUsingSkill.level, isLeftHandAttacking);
                    attacking = true;
                }
                else
                {
                    // Increase aim distance by attack distance
                    attackDistance = PlayerCharacterEntity.GetAttackDistance(isLeftHandAttacking);
                }
            }
            // Temporary disable colliders
            // Default aim position (aim to sky/space)
            aimPosition = centerRay.origin + centerRay.direction * (centerOriginToCharacterDistance + attackDistance);
            // Raycast from camera position to center of screen
            int tempCount = PhysicUtils.SortedRaycastNonAlloc3D(centerRay.origin, centerRay.direction, raycasts, findTargetRaycastDistance, Physics.DefaultRaycastLayers);
            float tempDistance;
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempHitInfo = raycasts[tempCounter];

                if (tempHitInfo.collider.GetComponent<IUnHittable>() != null)
                {
                    // Don't aim to unhittable objects
                    continue;
                }

                // Get distance between character and raycast hit point
                tempDistance = Vector3.Distance(CacheTransform.position, tempHitInfo.point);
                tempGameEntity = tempHitInfo.collider.GetComponent<IGameEntity>();

                if (tempGameEntity == null || !tempGameEntity.Entity || tempGameEntity.Entity.IsHide() ||
                    tempGameEntity.GetObjectId() == PlayerCharacterEntity.ObjectId)
                {
                    // Skip empty game entity / hiddeing entity / controlling player's entity
                    continue;
                }

                if (tempGameEntity is IDamageableEntity)
                {
                    // Entity isn't in front of character, so it's not the target
                    if (turnForwardWhileDoingAction && !IsInFront(tempHitInfo.point))
                        continue;

                    // Skip dead entity while attacking (to allow to use resurrect skills)
                    if (attacking && (tempGameEntity as IDamageableEntity).IsDead())
                        continue;

                    // Entity is in front of character, so this is target
                    aimPosition = tempHitInfo.point;
                    SelectedEntity = tempGameEntity.Entity;
                    break;
                }
                // Find item drop entity
                if (tempGameEntity.Entity is ItemDropEntity &&
                    tempDistance <= CurrentGameInstance.pickUpItemDistance)
                {
                    // Entity is in front of character, so this is target
                    if (!turnForwardWhileDoingAction || IsInFront(tempHitInfo.point))
                        aimPosition = tempHitInfo.point;
                    SelectedEntity = tempGameEntity.Entity;
                    break;
                }
                // Find activatable entity (NPC/Building/Mount/Etc)
                if (tempDistance <= CurrentGameInstance.conversationDistance)
                {
                    // Entity is in front of character, so this is target
                    if (!turnForwardWhileDoingAction || IsInFront(tempHitInfo.point))
                        aimPosition = tempHitInfo.point;
                    SelectedEntity = tempGameEntity.Entity;
                    break;
                }
            }
            // Calculate aim direction
            aimDirection = aimPosition - CacheTransform.position;
            aimDirection.y = 0f;
            aimDirection.Normalize();
            // Show target hp/mp
            CacheUISceneGameplay.SetTargetEntity(SelectedEntity);
            PlayerCharacterEntity.SetTargetEntity(SelectedEntity);
            // Update aim assist
            CacheGameplayCameraControls.enableAimAssist = enableAimAssist && (tempPressAttackRight || tempPressAttackLeft || !aimAssistOnFireOnly);
            CacheGameplayCameraControls.enableAimAssistX = enableAimAssistX;
            CacheGameplayCameraControls.enableAimAssistY = enableAimAssistY;
            CacheGameplayCameraControls.aimAssistRadius = aimAssistRadius;
            CacheGameplayCameraControls.aimAssistDistance = centerOriginToCharacterDistance + attackDistance;
            CacheGameplayCameraControls.aimAssistLayerMask = GetAimAssistLayerMask();
            CacheGameplayCameraControls.aimAssistXSpeed = aimAssistXSpeed;
            CacheGameplayCameraControls.aimAssistYSpeed = aimAssistYSpeed;
            CacheGameplayCameraControls.aimAssistAngleLessThan = 115f;
            CacheGameplayCameraControls.AimAssistAvoidanceListener = this;
        }

        public bool AvoidAimAssist(RaycastHit hitInfo)
        {
            IGameEntity entity = hitInfo.collider.GetComponent<IGameEntity>();
            if (entity != null && entity.Entity != null && entity.Entity != PlayerCharacterEntity)
            {
                DamageableEntity damageableEntity = entity.Entity as DamageableEntity;
                return damageableEntity == null || damageableEntity.IsDead();
            }
            return true;
        }

        private int GetAimAssistLayerMask()
        {
            int layerMask = 0;
            if (aimAssistCharacter)
                layerMask = layerMask | CurrentGameInstance.characterLayer.Mask;
            if (aimAssistBuilding)
                layerMask = layerMask | CurrentGameInstance.buildingLayer.Mask;
            if (aimAssistHarvestable)
                layerMask = layerMask | CurrentGameInstance.harvestableLayer.Mask;
            return layerMask;
        }

        private void UpdateTarget_BuildMode()
        {
            // Disable aim assist while constucting the building
            CacheGameplayCameraControls.enableAimAssist = false;
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
            normalizedInput = new Vector2(inputV, inputH).normalized;
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
                    if (normalizedInput.x > 0.5f || normalizedInput.x < -0.5f || normalizedInput.y > 0.5f || normalizedInput.y < -0.5f)
                        movementState = MovementState.Forward;
                    moveLookDirection = moveDirection;
                    moveLookDirection.y = 0f;
                    break;
                case ControllerMode.Combat:
                    if (normalizedInput.x > 0.5f)
                        movementState |= MovementState.Forward;
                    else if (normalizedInput.x < -0.5f)
                        movementState |= MovementState.Backward;
                    if (normalizedInput.y > 0.5f)
                        movementState |= MovementState.Right;
                    else if (normalizedInput.y < -0.5f)
                        movementState |= MovementState.Left;
                    moveLookDirection = cameraForward;
                    break;
            }

            if (ViewMode == ShooterControllerViewMode.Fps)
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
                        }
                    }
                }

                // Update look direction
                if (PlayerCharacterEntity.IsPlayingAttackOrUseSkillAnimation())
                {
                    activatingEntityOrDoAction = true;
                    SetTargetLookDirectionWhileDoingAction();
                }
                else if (queueUsingSkill.skill != null)
                {
                    activatingEntityOrDoAction = true;
                    SetTargetLookDirectionWhileDoingAction();
                    UpdateLookAtTarget();
                    UseSkill(isLeftHandAttacking);
                }
                else if (tempPressAttackRight || tempPressAttackLeft)
                {
                    activatingEntityOrDoAction = true;
                    SetTargetLookDirectionWhileDoingAction();
                    UpdateLookAtTarget();
                    Attack(isLeftHandAttacking);
                }
                else if (activateInput.IsHold && activatingEntityOrDoAction)
                {
                    SetTargetLookDirectionWhileDoingAction();
                    UpdateLookAtTarget();
                    HoldActivate();
                }
                else if (activateInput.IsRelease && activatingEntityOrDoAction)
                {
                    SetTargetLookDirectionWhileDoingAction();
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

            if (reloadInput.IsPress && !activatingEntityOrDoAction)
            {
                anyKeyPressed = true;
                // Reload ammo when press the button
                ReloadAmmo();
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
                PlayerCharacterEntity.CallServerSwitchEquipWeaponSet((byte)(PlayerCharacterEntity.EquipWeaponSet + 1));
            }

            if (!anyKeyPressed && !activatingEntityOrDoAction)
            {
                // Update look direction while moving without doing any action
                SetTargetLookDirectionWhileMoving();
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

            // Auto reload when ammo empty
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
            SetTargetLookDirectionWhileMoving();
        }

        private void ReloadAmmo()
        {
            // Reload ammo at server
            if (!PlayerCharacterEntity.EquipWeapons.rightHand.IsAmmoFull())
                PlayerCharacterEntity.CallServerReload(false);
            else if (!PlayerCharacterEntity.EquipWeapons.leftHand.IsAmmoFull())
                PlayerCharacterEntity.CallServerReload(true);
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
            // Show cross hair if weapon's crosshair setting isn't hidden or there is a constructing building
            crosshairRect.gameObject.SetActive((!setting.hidden && !HideCrosshair) || ConstructingBuildingEntity != null);
            // Not active?, don't update
            if (!crosshairRect.gameObject)
                return;
            // Change crosshair size by power
            Vector3 sizeDelta = crosshairRect.sizeDelta;
            sizeDelta.x += power;
            sizeDelta.y += power;
            CurrentCrosshairSize = sizeDelta;
            // Set crosshair size
            crosshairRect.sizeDelta = new Vector2(Mathf.Clamp(CurrentCrosshairSize.x, setting.minSpread, setting.maxSpread), Mathf.Clamp(CurrentCrosshairSize.y, setting.minSpread, setting.maxSpread));
        }

        private void UpdateRecoil()
        {
            if (movementState.HasFlag(MovementState.Forward) ||
                movementState.HasFlag(MovementState.Backward) ||
                movementState.HasFlag(MovementState.Left) ||
                movementState.HasFlag(MovementState.Right))
            {
                if (movementState.HasFlag(MovementState.IsUnderWater))
                    CacheGameplayCameraControls.XRotationVelocity += CurrentCrosshairSetting.recoil * recoilRateWhileSwimming;
                else if (extraMovementState.HasFlag(ExtraMovementState.IsSprinting))
                    CacheGameplayCameraControls.XRotationVelocity += CurrentCrosshairSetting.recoil * recoilRateWhileSprinting;
                else
                    CacheGameplayCameraControls.XRotationVelocity += CurrentCrosshairSetting.recoil * recoilRateWhileMoving;
            }
            else if (extraMovementState.HasFlag(ExtraMovementState.IsCrouching))
            {
                CacheGameplayCameraControls.XRotationVelocity += CurrentCrosshairSetting.recoil * recoilRateWhileCrouching;
            }
            else if (extraMovementState.HasFlag(ExtraMovementState.IsCrawling))
            {
                CacheGameplayCameraControls.XRotationVelocity += CurrentCrosshairSetting.recoil * recoilRateWhileCrawling;
            }
            else
            {
                CacheGameplayCameraControls.XRotationVelocity += CurrentCrosshairSetting.recoil;
            }
        }

        private void OnAttackRoutine(bool isLeftHand, CharacterItem weapon, int hitIndex, DamageInfo damageInfo, Dictionary<DamageElement, MinMaxFloat> damageAmounts, Vector3 aimPosition, int randomSeed)
        {
            UpdateRecoil();
        }

        private void SetTargetLookDirectionWhileDoingAction()
        {
            switch (ViewMode)
            {
                case ShooterControllerViewMode.Fps:
                    // Just look at camera forward while character playing action animation
                    targetLookDirection = cameraForward;
                    break;
                case ShooterControllerViewMode.Tps:
                    // Just look at camera forward while character playing action animation while `turnForwardWhileDoingAction` is `true`
                    Vector3 doActionLookDirection = turnForwardWhileDoingAction ? cameraForward : aimDirection;
                    if (turnSpeedWileDoingAction > 0f)
                    {
                        Quaternion currentRot = Quaternion.LookRotation(targetLookDirection);
                        Quaternion targetRot = Quaternion.LookRotation(doActionLookDirection);
                        currentRot = Quaternion.Slerp(currentRot, targetRot, turnSpeedWileDoingAction * Time.deltaTime);
                        targetLookDirection = currentRot * Vector3.forward;
                    }
                    else
                    {
                        // Turn immediately because turn speed <= 0
                        targetLookDirection = doActionLookDirection;
                    }
                    break;
            }
        }

        private void SetTargetLookDirectionWhileMoving()
        {
            switch (ViewMode)
            {
                case ShooterControllerViewMode.Fps:
                    // Just look at camera forward while character playing action animation
                    targetLookDirection = cameraForward;
                    break;
                case ShooterControllerViewMode.Tps:
                    // Turn character look direction to move direction while moving without doing any action
                    if (moveDirection.sqrMagnitude > 0f)
                    {
                        float currentTurnSpeed = CurrentTurnSpeed;
                        if (currentTurnSpeed > 0f)
                        {
                            Quaternion currentRot = Quaternion.LookRotation(targetLookDirection);
                            Quaternion targetRot = Quaternion.LookRotation(moveLookDirection);
                            currentRot = Quaternion.Slerp(currentRot, targetRot, currentTurnSpeed * Time.deltaTime);
                            targetLookDirection = currentRot * Vector3.forward;
                        }
                        else
                        {
                            // Turn immediately because turn speed <= 0
                            targetLookDirection = moveLookDirection;
                        }
                    }
                    break;
            }
        }

        private void UpdateLookAtTarget()
        {
            // Turn character to look direction immediately
            PlayerCharacterEntity.SetLookRotation(Quaternion.LookRotation(targetLookDirection));
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

        private void UseSkill(string id, Vector3? aimPosition)
        {
            BaseSkill skill;
            short skillLevel;
            if (!GameInstance.Skills.TryGetValue(BaseGameData.MakeDataId(id), out skill) || skill == null ||
                !PlayerCharacterEntity.GetCaches().Skills.TryGetValue(skill, out skillLevel))
                return;
            SetQueueUsingSkill(aimPosition, skill, skillLevel);
        }

        private void UseItem(string id, Vector3? aimPosition)
        {
            int itemIndex;
            BaseItem item;
            int dataId = BaseGameData.MakeDataId(id);
            if (GameInstance.Items.ContainsKey(dataId))
            {
                item = GameInstance.Items[dataId];
                itemIndex = OwningCharacter.IndexOfNonEquipItem(dataId);
            }
            else
            {
                InventoryType inventoryType;
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
                item = characterItem.GetItem();
            }

            if (itemIndex < 0)
                return;

            if (item == null)
                return;

            if (item.IsEquipment())
            {
                PlayerCharacterEntity.CallServerEquipItem((short)itemIndex);
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
                PlayerCharacterEntity.CallServerUseItem((short)itemIndex);
            }
        }

        public void Attack(bool isLeftHand)
        {
            // Set this to `TRUE` to update crosshair
            isDoingAction = PlayerCharacterEntity.CallServerAttack(isLeftHand);
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
            if (targetPlayer != null)
                CacheUISceneGameplay.SetActivePlayerCharacter(targetPlayer);
            else if (targetNpc != null)
                PlayerCharacterEntity.CallServerNpcActivate(targetNpc.ObjectId);
            else if (targetBuilding != null)
                ActivateBuilding(targetBuilding);
            else if (targetVehicle != null)
                PlayerCharacterEntity.CallServerEnterVehicle(targetVehicle.ObjectId);
            else if (targetWarpPortal != null)
                PlayerCharacterEntity.CallServerEnterWarp(targetWarpPortal.ObjectId);
        }

        public void UseSkill(bool isLeftHand)
        {
            if (queueUsingSkill.skill != null)
            {
                if (queueUsingSkill.itemIndex >= 0)
                {
                    if (queueUsingSkill.aimPosition.HasValue)
                        isDoingAction = PlayerCharacterEntity.CallServerUseSkillItem(queueUsingSkill.itemIndex, isLeftHand, queueUsingSkill.aimPosition.Value);
                    else
                        isDoingAction = PlayerCharacterEntity.CallServerUseSkillItem(queueUsingSkill.itemIndex, isLeftHand);
                }
                else
                {
                    if (queueUsingSkill.aimPosition.HasValue)
                        isDoingAction = PlayerCharacterEntity.CallServerUseSkill(queueUsingSkill.skill.DataId, isLeftHand, queueUsingSkill.aimPosition.Value);
                    else
                        isDoingAction = PlayerCharacterEntity.CallServerUseSkill(queueUsingSkill.skill.DataId, isLeftHand);
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
            int tempCount = OverlapObjects(CacheTransform.position, actDistance, layerMask);
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                if (overlapColliders[tempCounter].gameObject == target)
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

        public void UpdateCameraSettings()
        {
            CacheGameplayCamera.fieldOfView = CameraFov;
            CacheGameplayCamera.nearClipPlane = CameraNearClipPlane;
            CacheGameplayCamera.farClipPlane = CameraFarClipPlane;
            PlayerCharacterEntity.ModelManager.SetIsFps(viewMode == ShooterControllerViewMode.Fps);
        }

        public bool IsInFront(Vector3 position)
        {
            return Vector3.Angle(cameraForward, position - CacheTransform.position) < 115f;
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
            aimPosition = centerRay.origin + centerRay.direction * (centerOriginToCharacterDistance + findTargetRaycastDistance);
            // Raycast from camera position to center of screen
            bool hitGround;
            FindConstructingBuildingArea(centerRay, centerOriginToCharacterDistance + findTargetRaycastDistance, out hitGround);
            // Not hit ground, find ground to snap
            if (!hitGround)
            {
                // Find nearest grounded position
                FindConstructingBuildingArea(new Ray(aimPosition, Vector3.down), 100f, out _);
            }
            // Place constructing building
            if ((ConstructingBuildingEntity.BuildingArea && !ConstructingBuildingEntity.BuildingArea.snapBuildingObject) ||
                !ConstructingBuildingEntity.BuildingArea)
            {
                // Place the building on the ground when the building area is not snapping
                // Or place it anywhere if there is no building area
                // It's also no snapping build area, so set building rotation by camera look direction
                if (!clampBuildPositionByBuildDistance)
                    ConstructingBuildingEntity.Position = aimPosition;
                else
                    ConstructingBuildingEntity.Position = GameplayUtils.ClampPosition(CacheTransform.position, aimPosition, ConstructingBuildingEntity.BuildDistance);
                // Rotate to camera
                Vector3 direction = aimPosition - CacheGameplayCameraTransform.position;
                direction.y = 0f;
                direction.Normalize();
                ConstructingBuildingEntity.CacheTransform.eulerAngles = Quaternion.LookRotation(direction).eulerAngles + (Vector3.up * buildYRotate);
            }
            return ConstructingBuildingEntity.Position;
        }

        private int FindConstructingBuildingArea(Ray ray, float distance, out bool hitGround)
        {
            hitGround = false;
            int tempCount = PhysicUtils.SortedRaycastNonAlloc3D(ray.origin, ray.direction, raycasts, distance, CurrentGameInstance.GetBuildLayerMask());
            IGameEntity gameEntity;
            BuildingArea buildingArea;
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempHitInfo = raycasts[tempCounter];

                if (!IsInFront(tempHitInfo.point))
                {
                    // Skip because this position is not allowed to build the building
                    continue;
                }

                buildingArea = tempHitInfo.transform.GetComponent<BuildingArea>();
                if (buildingArea == null)
                {
                    // Hit something but it is not building area
                    gameEntity = tempHitInfo.transform.GetComponent<IGameEntity>();
                    if (gameEntity == null || gameEntity.Entity != ConstructingBuildingEntity)
                    {
                        // Hit something and it is not part of constructing building entity, assume that it is ground
                        aimPosition = tempHitInfo.point;
                        hitGround = true;
                        break;
                    }
                    continue;
                }

                if (buildingArea.IsPartOfBuildingEntity(ConstructingBuildingEntity) ||
                    !ConstructingBuildingEntity.BuildingTypes.Contains(buildingArea.buildingType))
                {
                    // Skip because this area is not allowed to build the building that you are going to build
                    continue;
                }

                // Found building area which can construct the building
                aimPosition = tempHitInfo.point;
                hitGround = true;
                ConstructingBuildingEntity.BuildingArea = buildingArea;
                break;
            }
            return tempCount;
        }

        public override void FinishBuildAimControls(bool isCancel)
        {
            if (isCancel)
                CancelBuild();
        }
    }
}
