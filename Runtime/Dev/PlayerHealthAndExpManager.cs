using UdonSharp;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerHealthAndExpManager : UdonSharpBehaviour
    {
        [HideInInspector] [SerializeField] [SingletonReference] private PlayerDataManager playerDataManager;
        [HideInInspector] [SerializeField] [SingletonReference] private BoneAttachmentManager boneAttachmentManager;
        [HideInInspector] [SerializeField] [SingletonReference] private LockstepAPI lockstep;

        public GameObject hitBoxPrefab;
        public LayerMask shootingLayerMask;
        private VRCPlayerApi localPlayer;
        private DataDictionary hitBoxesByPlayerId = new DataDictionary();
        public Transform localPlayerHead;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            playerDataManager.RegisterCustomPlayerData<PlayerHealthAndExpData>(nameof(PlayerHealthAndExpData));
            boneAttachmentManager.AttachToLocalTrackingData(VRCPlayerApi.TrackingDataType.Head, localPlayerHead);
            localPlayerHead.localPosition = Vector3.zero;
            localPlayerHead.localRotation = Quaternion.identity;
        }

        private void CreateHitBoxForPlayer(uint playerId)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById((int)playerId);
            if (player == null || !player.IsValid())
                return;
            GameObject hitBoxGo = Instantiate(hitBoxPrefab);
            Transform hitBoxTransform = hitBoxGo.transform;
            boneAttachmentManager.AttachToBone(player, HumanBodyBones.Head, hitBoxTransform);
            hitBoxTransform.localPosition = Vector3.up * 0.125f;
            hitBoxTransform.localRotation = Quaternion.identity;
            LookAtConstraint constraint = hitBoxGo.GetComponentInChildren<LookAtConstraint>();
            ConstraintSource constraintSource = new ConstraintSource();
            constraintSource.sourceTransform = localPlayerHead;
            constraintSource.weight = 1f;
            constraint.AddSource(constraintSource);
            TestPlayerHitBox hitBox = hitBoxGo.GetComponent<TestPlayerHitBox>();
            hitBox.playerId = playerId;
            hitBox.playerData = playerDataManager.GetPlayerData<PlayerHealthAndExpData>(nameof(PlayerHealthAndExpData), playerId);
            UpdateHealthBar(hitBox);
            hitBoxesByPlayerId.Add(playerId, hitBox);
        }

        private void UpdateHealthBar(TestPlayerHitBox hitBox)
        {
            hitBox.GetComponentInChildren<Slider>().value = ((float)hitBox.playerData.health / PlayerHealthAndExpData.MaxHealth);
        }

        [LockstepEvent(LockstepEventType.OnInit)]
        public void OnInit()
        {
            CreateHitBoxForPlayer(lockstep.MasterPlayerId);
        }

        [LockstepEvent(LockstepEventType.OnPreClientJoined)]
        public void OnPreClientJoined()
        {
            CreateHitBoxForPlayer(lockstep.JoinedPlayerId);
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp)]
        public void OnClientBeginCatchUp()
        {
            foreach (uint playerId in lockstep.AllClientPlayerIds)
                CreateHitBoxForPlayer(playerId);
        }

        [LockstepEvent(LockstepEventType.OnClientLeft)]
        public void OnClientLeft()
        {
            if (!hitBoxesByPlayerId.Remove(lockstep.LeftPlayerId, out DataToken hitBoxToken))
                return;
            TestPlayerHitBox hitBox = (TestPlayerHitBox)hitBoxToken.Reference;
            boneAttachmentManager.DetachFromBone((int)lockstep.LeftPlayerId, HumanBodyBones.Head, hitBox.transform);
            Destroy(hitBox.gameObject);
        }

        private void Shoot()
        {
            var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            if (!Physics.Raycast(head.position, head.rotation * Vector3.forward, out RaycastHit hit, 100f, shootingLayerMask))
                return;
            if (hit.transform == null) // VRC internal object got hit.
                return;
            TestPlayerHitBox hitBox = hit.transform.GetComponent<TestPlayerHitBox>();
            if (hitBox == null)
                return;
            SendPlayerGotShotIA(hitBox.playerId);
        }

        private void SendPlayerGotShotIA(uint playerId)
        {
            lockstep.WriteSmallUInt(playerId);
            lockstep.SendInputAction(onPlayerGotShotIAId);
        }

        [HideInInspector] [SerializeField] private uint onPlayerGotShotIAId;
        [LockstepInputAction(nameof(onPlayerGotShotIAId))]
        public void OnPlayerGotShotIA()
        {
            uint playerId = lockstep.ReadSmallUInt();
            if (!lockstep.ClientStateExists(playerId))
                return;
            TestPlayerHitBox hitBox = (TestPlayerHitBox)hitBoxesByPlayerId[playerId].Reference;
            PlayerHealthAndExpData playerData = hitBox.playerData;
            playerData.health = playerData.health <= 15u ? 0u : (playerData.health - 15u);
            UpdateHealthBar(hitBox);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                Shoot();

            if (Input.GetKeyDown(KeyCode.F))
                foreach (PlayerHealthAndExpData playerData in playerDataManager.GetAllPlayerData<PlayerHealthAndExpData>(nameof(PlayerHealthAndExpData)))
                    Debug.Log($"<dlt> {playerData.corePlayerData.displayName} (isOffline: {playerData.corePlayerData.isOffline}): health: {playerData.health}, exp: {playerData.exp}");
        }
    }
}
