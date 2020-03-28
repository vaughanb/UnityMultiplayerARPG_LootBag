using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class PlayerCharacterController : BasePlayerCharacterController
    {
        public enum PlayerCharacterControllerMode
        {
            PointClick,
            WASD,
            Both,
        }

        public enum TargetActionType
        {
            Undefined,
            Attack,
            UseSkill,
            Loot
        }
        
        public const float DETECT_MOUSE_DRAG_DISTANCE_SQUARED = 100f;
        public const float DETECT_MOUSE_HOLD_DURATION = 1f;

        public float angularSpeed = 800f;
        public PlayerCharacterControllerMode controllerMode;
        [Tooltip("Set this to TRUE to find nearby enemy and look to it while attacking when `Controller Mode` is `WASD`")]
        public bool wasdLockAttackTarget;
        [Tooltip("This will be used to find nearby enemy when `Controller Mode` is `Point Click` or when `Wasd Lock Attack Target` is `TRUE`")]
        public float lockAttackTargetDistance = 10f;
        [Tooltip("Set this to TRUE to move to target immediately when clicked on target, if this is FALSE it will not move to target immediately")]
        public bool pointClickSetTargetImmediately;
        public GameObject targetObjectPrefab;

        [Header("Building Settings")]
        public bool buildGridSnap;
        public float buildGridSize = 4f;
        public bool buildRotationSnap;
        public float buildRotateAngle = 45f;

        protected bool isSprinting;
        protected Vector3? destination;
        protected Vector3 mouseDownPosition;
        protected float mouseDownTime;
        protected bool isMouseDragOrHoldOrOverUI;
        protected uint lastNpcObjectId;

        public GameObject CacheTargetObject { get; protected set; }
        protected Vector3? targetPosition;
        protected TargetActionType targetActionType;

        // Optimizing garbage collection
        protected bool getMouseUp;
        protected bool getMouseDown;
        protected bool getMouse;
        protected bool isPointerOverUI;
        protected bool isMouseDragDetected;
        protected bool isMouseHoldDetected;
        protected bool isMouseHoldAndNotDrag;
        protected BaseCharacterEntity targetCharacter;
        protected BasePlayerCharacterEntity targetPlayer;
        protected BaseMonsterCharacterEntity targetMonster;
        protected NpcEntity targetNpc;
        protected ItemDropEntity targetItemDrop;
        protected BuildingEntity targetBuilding;
        protected VehicleEntity targetVehicle;
        protected HarvestableEntity targetHarvestable;
        protected Quaternion tempLookAt;
        protected Vector3? targetLookDirection;
        protected Vector3 previousPointClickPosition = Vector3.positiveInfinity;
        public NearbyEntityDetector ActivatableEntityDetector { get; protected set; }
        public NearbyEntityDetector ItemDropEntityDetector { get; protected set; }
        public NearbyEntityDetector EnemyEntityDetector { get; protected set; }
        protected int findingEnemyIndex;
        protected bool isLeftHandAttacking;

        protected override void Awake()
        {
            base.Awake();
            buildingItemIndex = -1;
            findingEnemyIndex = -1;
            isLeftHandAttacking = false;
            ConstructingBuildingEntity = null;

            if (targetObjectPrefab != null)
            {
                // Set parent transform to root for the best performance
                CacheTargetObject = Instantiate(targetObjectPrefab);
                CacheTargetObject.SetActive(false);
            }
            // This entity detector will be find entities to use when pressed activate key
            GameObject tempGameObject = new GameObject("_ActivatingEntityDetector");
            ActivatableEntityDetector = tempGameObject.AddComponent<NearbyEntityDetector>();
            ActivatableEntityDetector.detectingRadius = CurrentGameInstance.conversationDistance;
            ActivatableEntityDetector.findPlayer = true;
            ActivatableEntityDetector.findOnlyAlivePlayers = true;
            ActivatableEntityDetector.findNpc = true;
            ActivatableEntityDetector.findBuilding = true;
            ActivatableEntityDetector.findOnlyAliveBuildings = true;
            ActivatableEntityDetector.findOnlyActivatableBuildings = true;
            ActivatableEntityDetector.findVehicle = true;
            // This entity detector will be find item drop entities to use when pressed pickup key
            tempGameObject = new GameObject("_ItemDropEntityDetector");
            ItemDropEntityDetector = tempGameObject.AddComponent<NearbyEntityDetector>();
            ItemDropEntityDetector.detectingRadius = CurrentGameInstance.pickUpItemDistance;
            ItemDropEntityDetector.findItemDrop = true;
            // This entity detector will be find item drop entities to use when pressed pickup key
            tempGameObject = new GameObject("_EnemyEntityDetector");
            EnemyEntityDetector = tempGameObject.AddComponent<NearbyEntityDetector>();
            EnemyEntityDetector.findPlayer = true;
            EnemyEntityDetector.findOnlyAlivePlayers = false;
            EnemyEntityDetector.findPlayerToAttack = true;
            EnemyEntityDetector.findMonster = true;
            EnemyEntityDetector.findOnlyAliveMonsters = false;
            EnemyEntityDetector.findMonsterToAttack = true;
        }

        protected override void Setup(BasePlayerCharacterEntity characterEntity)
        {
            base.Setup(characterEntity);

            if (characterEntity == null)
                return;

            tempLookAt = MovementTransform.rotation;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (CacheTargetObject != null)
                Destroy(CacheTargetObject.gameObject);
            if (ActivatableEntityDetector != null)
                Destroy(ActivatableEntityDetector.gameObject);
            if (ItemDropEntityDetector != null)
                Destroy(ItemDropEntityDetector.gameObject);
            if (EnemyEntityDetector != null)
                Destroy(EnemyEntityDetector.gameObject);
        }

        protected override void Update()
        {
            if (PlayerCharacterEntity == null || !PlayerCharacterEntity.IsOwnerClient)
                return;

            base.Update();

            if (CacheTargetObject != null)
                CacheTargetObject.gameObject.SetActive(destination.HasValue);

            if (PlayerCharacterEntity.IsDead())
            {
                ClearQueueUsingSkill();
                destination = null;
                if (CacheUISceneGameplay != null)
                    CacheUISceneGameplay.SetTargetEntity(null);
                CancelBuild();
            }
            else
            {
                if (CacheUISceneGameplay != null)
                    CacheUISceneGameplay.SetTargetEntity(SelectedEntity);
            }

            if (destination.HasValue)
            {
                if (CacheTargetObject != null)
                    CacheTargetObject.transform.position = destination.Value;
                if (Vector3.Distance(destination.Value, MovementTransform.position) < StoppingDistance + 0.5f)
                    destination = null;
            }

            UpdateInput();
            UpdateFollowTarget();
            UpdateLookAtTarget();
        }

        private Vector3 GetBuildingPlacePosition(Vector3 position)
        {
            if (buildGridSnap)
                position = new Vector3(Mathf.Round(position.x / buildGridSize) * buildGridSize, position.y, Mathf.Round(position.z / buildGridSize) * buildGridSize);
            return position;
        }

        private Vector3 GetBuildingPlaceEulerAngles(Vector3 eulerAngles)
        {
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            // Make Y rotation set to 0, 90, 180
            if (buildRotationSnap)
                eulerAngles.y = Mathf.Round(eulerAngles.y / 90) * 90;
            return eulerAngles;
        }

        public bool TryGetSelectedTargetAsAttackingEntity(out BaseCharacterEntity character)
        {
            character = null;
            if (SelectedEntity != null)
            {
                character = SelectedEntity as BaseCharacterEntity;
                if (character != null &&
                    character != PlayerCharacterEntity &&
                    character.CanReceiveDamageFrom(PlayerCharacterEntity))
                    return true;
                else
                    character = null;
            }
            return false;
        }

        public bool TryGetAttackingCharacter(out BaseCharacterEntity character)
        {
            character = null;
            if (targetActionType != TargetActionType.Attack)
                return false;

            if (TargetEntity != null)
            {
                character = TargetEntity as BaseCharacterEntity;
                if (character != null &&
                    character != PlayerCharacterEntity &&
                    character.CanReceiveDamageFrom(PlayerCharacterEntity))
                    return true;
                else
                    character = null;
            }
            return false;
        }

        public bool TryGetUsingSkillCharacter(out BaseCharacterEntity character)
        {
            character = null;
            if (targetActionType != TargetActionType.UseSkill)
                return false;

            if (TargetEntity != null)
            {
                character = TargetEntity as BaseCharacterEntity;
                if (character != null)
                    return true;
                else
                    character = null;
            }
            return false;
        }

        public bool TryGetTargetCharacter(out BaseCharacterEntity character)
        {
            character = null;
            return PlayerCharacterEntity.TryGetTargetEntity(out character);
        }

        public bool TryGetLootableCharacter(out BaseCharacterEntity character)
        {
            character = null;

            if (TargetEntity != null && TargetEntity is BaseCharacterEntity)
            {
                character = TargetEntity as BaseCharacterEntity;
                if (character.IsDead() && character.LootBag.Count > 0)
                    return true;
                else
                    character = null;
            }
            return false;
        }

        public void GetAttackDistanceAndFov(bool isLeftHand, out float attackDistance, out float attackFov)
        {
            attackDistance = PlayerCharacterEntity.GetAttackDistance(isLeftHand);
            attackFov = PlayerCharacterEntity.GetAttackFov(isLeftHand);

            if (queueUsingSkill.skill != null && queueUsingSkill.skill.IsAttack())
            {
                // If skill is attack skill, set distance and fov by skill
                GetUseSkillDistanceAndFov(out attackDistance, out attackFov);
            }
            attackDistance -= PlayerCharacterEntity.StoppingDistance;
        }

        public void GetUseSkillDistanceAndFov(out float castDistance, out float castFov)
        {
            castDistance = CurrentGameInstance.conversationDistance;
            castFov = 360f;
            if (queueUsingSkill.skill != null)
            {
                // If skill is attack skill, set distance and fov by skill
                castDistance = queueUsingSkill.skill.GetCastDistance(PlayerCharacterEntity, queueUsingSkill.level, false);
                castFov = queueUsingSkill.skill.GetCastFov(PlayerCharacterEntity, queueUsingSkill.level, false);
            }
            castDistance -= PlayerCharacterEntity.StoppingDistance;
        }

        public bool IsLockTarget()
        {
            return controllerMode == PlayerCharacterControllerMode.Both ||
                controllerMode == PlayerCharacterControllerMode.PointClick ||
                (controllerMode == PlayerCharacterControllerMode.WASD && wasdLockAttackTarget);
        }

        public Vector3 GetMoveDirection(float horizontalInput, float verticalInput)
        {
            Vector3 moveDirection = Vector3.zero;
            switch (CurrentGameInstance.DimensionType)
            {
                case DimensionType.Dimension3D:
                    Vector3 forward = CacheGameplayCameraTransform.forward;
                    Vector3 right = CacheGameplayCameraTransform.right;
                    forward.y = 0f;
                    right.y = 0f;
                    forward.Normalize();
                    right.Normalize();
                    moveDirection += forward * verticalInput;
                    moveDirection += right * horizontalInput;
                    // normalize input if it exceeds 1 in combined length:
                    if (moveDirection.sqrMagnitude > 1)
                        moveDirection.Normalize();
                    break;
                case DimensionType.Dimension2D:
                    moveDirection = new Vector2(horizontalInput, verticalInput);
                    break;
            }
            return moveDirection;
        }

        public void RequestAttack()
        {
            if (PlayerCharacterEntity.RequestAttack(isLeftHandAttacking))
                isLeftHandAttacking = !isLeftHandAttacking;
        }

        public void RequestUsePendingSkill()
        {
            if (queueUsingSkill.skill != null && PlayerCharacterEntity.CanUseSkill())
            {
                bool canUseSkill = false;
                if (queueUsingSkill.itemIndex >= 0)
                {
                    if (queueUsingSkill.aimPosition.HasValue)
                        canUseSkill = PlayerCharacterEntity.RequestUseSkillItem(queueUsingSkill.itemIndex, isLeftHandAttacking, queueUsingSkill.aimPosition.Value);
                    else
                        canUseSkill = PlayerCharacterEntity.RequestUseSkillItem(queueUsingSkill.itemIndex, isLeftHandAttacking);
                }
                else
                {
                    if (queueUsingSkill.aimPosition.HasValue)
                        canUseSkill = PlayerCharacterEntity.RequestUseSkill(queueUsingSkill.skill.DataId, isLeftHandAttacking, queueUsingSkill.aimPosition.Value);
                    else
                        canUseSkill = PlayerCharacterEntity.RequestUseSkill(queueUsingSkill.skill.DataId, isLeftHandAttacking);
                }
                if (canUseSkill)
                    isLeftHandAttacking = !isLeftHandAttacking;
                ClearQueueUsingSkill();
            }
        }
    }
}
