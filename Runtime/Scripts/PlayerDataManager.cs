using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [CustomRaisedEventsDispatcher(typeof(PlayerDataEventAttribute), typeof(PlayerDataEventType))]
    public class PlayerDataManager : PlayerDataManagerAPI
    {
        public override string GameStateInternalName => "jansharp.player-data";
        public override string GameStateDisplayName => "Player Data";
        public override bool GameStateSupportsImportExport => true;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;
        public override LockstepGameStateOptionsUI ExportUI => null;
        public override LockstepGameStateOptionsUI ImportUI => importUI;
        [SerializeField] private PlayerDataImportUI importUI;

        [HideInInspector][SerializeField][SingletonReference] private WannaBeClassesManager wannaBeClasses;

        /// <summary>
        /// <para>Sorted alphabetically, use binary search.</para>
        /// </summary>
        private string[] playerDataClassNames = new string[ArrList.MinCapacity];
        private int playerDataClassNamesCount = 0;
        /// <summary>
        /// <para>Sorted associated with <see cref="playerDataClassNames"/>, use index of.</para>
        /// </summary>
        private string[] playerDataInternalNames = new string[ArrList.MinCapacity];
        private int playerDataInternalNamesCount = 0;
        /// <summary>
        /// <para>Sorted alphabetically, use binary search.</para>
        /// <para>Keys are class names.</para>
        /// </summary>
        [HideInInspector][SerializeField] private string[] internalNameByClassNameKeys;
        /// <summary>
        /// <para>Sorted associated with <see cref="internalNameByClassNameKeys"/>, use index of.</para>
        /// <para>Values are internal names.</para>
        /// </summary>
        [HideInInspector][SerializeField] private string[] internalNameByClassNameValues;

        /// <summary>
        /// <para><c>0u</c> is an invalid id.</para>
        /// </summary>
        private uint nextPersistentId = 1u;
        /// <summary>
        /// <para>All players.</para>
        /// </summary>
        private CorePlayerData[] allPlayerData = new CorePlayerData[ArrList.MinCapacity];
        private int allPlayerDataCount = 0;
        /// <summary>
        /// <para>All players.</para>
        /// </summary>
        private DataDictionary playerDataByPersistentId = new DataDictionary();
        /// <summary>
        /// <para>All non overshadowed players.</para>
        /// </summary>
        private DataDictionary playerDataByName = new DataDictionary();
        /// <summary>
        /// <para>All online players.</para>
        /// </summary>
        private DataDictionary playerDataByPlayerId = new DataDictionary();

        /// <summary>
        /// <para>Only used during imports to allow other systems to remap imported persistent ids to current
        /// persistent ids.</para>
        /// </summary>
        private DataDictionary persistentIdByImportedPersistentId = new DataDictionary();

        private const long MaxWorkMSPerFrame = 5L;
        private System.Diagnostics.Stopwatch deSerializationSw = new System.Diagnostics.Stopwatch();
        private int suspendedIndexInCorePlayerDataArray;
        private int suspendedIndexInCustomPlayerDataArray;
        private bool suspendedInCustomPlayerData;
        private int deserializationStage = 0;
        private int exportStage = 0;
        private int exportUnknownSizeScopeSizePosition;
        private int importStage = 0;
        private CorePlayerData[] allImportedPlayerData;
        private CorePlayerData[] newlyCreatedImportedPlayerData;
        private int newlyCreatedImportedPlayerDataCount;
        private int importedCustomPlayerDataCount;
        private string importSuspendedInternalName;
        private string importSuspendedDisplayName;
        private uint importSuspendedDataVersion;
        private int importSuspendedScopeByteSize;
        private int importSuspendedClassNameIndex;
        private bool[] importSuspendedPresentClassNames;
        private bool importSuspendedAnyClassNamePresent;

        public override CorePlayerData[] AllCorePlayerDataRaw => allPlayerData;
        public override int AllCorePlayerDataCount => allPlayerDataCount;
        public override CorePlayerData[] AllCorePlayerData
        {
            get
            {
                CorePlayerData[] allCorePlayerData = new CorePlayerData[allPlayerDataCount];
                System.Array.Copy(allPlayerData, allCorePlayerData, allPlayerDataCount);
                return allCorePlayerData;
            }
        }
        public override CorePlayerData GetCorePlayerDataAt(int index) => allPlayerData[index];

        private uint localPlayerId;

        private bool isInitialized = false;
        public override bool IsInitialized => isInitialized;

        private void Start()
        {
            localPlayerId = (uint)Networking.LocalPlayer.playerId;
            RaiseOnRegisterCustomPlayerData();
            RaiseOnAllCustomPlayerDataRegistered();
        }

        public override void RegisterCustomPlayerDataDynamic(string playerDataClassName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  RegisterCustomPlayerDataInternal");
#endif
            int index = ~ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            if (index < 0)
            {
                Debug.LogError($"[PlayerData] Attempt to register the custom player data class name {playerDataClassName} twice.");
                return;
            }
            int internalNameLutIndex = System.Array.BinarySearch(internalNameByClassNameKeys, playerDataClassName);
            if (internalNameLutIndex < 0)
            {
                Debug.LogError($"[PlayerData] Attempt to register a custom player data class with the name "
                    + $"{playerDataClassName}, however there is no such class which derives from the {nameof(PlayerData)} class.");
                return;
            }
            string internalName = internalNameByClassNameValues[internalNameLutIndex];
            int internalNameIndex = ArrList.IndexOf(ref playerDataInternalNames, ref playerDataInternalNamesCount, internalName);
            if (internalNameIndex != -1)
            {
                Debug.LogError($"[PlayerData] '{playerDataClassNames[internalNameIndex]}' and '{playerDataClassName}' are both "
                    + $"trying to use '{internalName}' as their PlayerDataInternalName. They must use different internal names.");
                return;
            }
            ArrList.Insert(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName, index);
            ArrList.Insert(ref playerDataInternalNames, ref playerDataInternalNamesCount, internalName, index);
        }

        public override int GetPlayerDataClassNameIndexDynamic(string playerDataClassName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetPlayerDataClassNameIndexDynamic");
#endif
            return ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
        }

        private CorePlayerData localPlayerData;
        public override CorePlayerData LocalPlayerData => localPlayerData;

        public override CorePlayerData SendingPlayerData
        {
            get
            {
#if PLAYER_DATA_DEBUG
                Debug.Log($"[PlayerDataDebug] Manager  SendingPlayerData.get");
#endif
                return (CorePlayerData)playerDataByPlayerId[lockstep.SendingPlayerId].Reference;
            }
        }

        public override CorePlayerData GetCorePlayerDataForPlayerId(uint playerId)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetCorePlayerDataForPlayerId - playerId: {playerId}");
#endif
            return (CorePlayerData)playerDataByPlayerId[playerId].Reference;
        }

        public override CorePlayerData GetCorePlayerDataForPersistentId(uint persistentId)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetCorePlayerDataForPersistentId - persistentId: {persistentId}");
#endif
            return (CorePlayerData)playerDataByPersistentId[persistentId].Reference;
        }

        public override PlayerData GetPlayerDataForPlayerIdDynamic(string playerDataClassName, uint playerId)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetPlayerDataForPlayerIdDynamic - playerDataClassName: {playerDataClassName}, playerId: {playerId}");
#endif
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPlayerId[playerId].Reference;
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            return corePlayerData.customPlayerData[classIndex];
        }

        public override PlayerData GetPlayerDataForPersistentIdDynamic(string playerDataClassName, uint persistentId)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetPlayerDataForPersistentIdDynamic - playerDataClassName: {playerDataClassName}, persistentId: {persistentId}");
#endif
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPersistentId[persistentId].Reference;
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            return corePlayerData.customPlayerData[classIndex];
        }

        public override PlayerData GetPlayerDataFromCoreDynamic(string playerDataClassName, CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetPlayerDataFromCoreDynamic - playerDataClassName: {playerDataClassName}, corePlayerData != null: {corePlayerData != null}");
#endif
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            return corePlayerData.customPlayerData[classIndex];
        }

        public override PlayerData[] GetAllPlayerDataDynamic(string playerDataClassName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetAllPlayerDataDynamic - playerDataClassName: {playerDataClassName}");
#endif
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            PlayerData[] result = new PlayerData[allPlayerDataCount];
            for (int i = 0; i < allPlayerDataCount; i++)
                result[i] = allPlayerData[i].customPlayerData[classIndex];
            return result;
        }

        public override bool TryGetCorePlayerDataForPlayerId(uint playerId, out CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  TryGetCorePlayerDataForPlayerId - playerId: {playerId}");
#endif
            if (playerDataByPlayerId.TryGetValue(playerId, out DataToken corePlayerDataToken))
            {
                corePlayerData = (CorePlayerData)corePlayerDataToken.Reference;
                return true;
            }
            corePlayerData = null;
            return false;
        }

        public override bool TryGetCorePlayerDataForPersistentId(uint persistentId, out CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  TryGetCorePlayerDataForPersistentId - persistentId: {persistentId}");
#endif
            if (playerDataByPersistentId.TryGetValue(persistentId, out DataToken corePlayerDataToken))
            {
                corePlayerData = (CorePlayerData)corePlayerDataToken.Reference;
                return true;
            }
            corePlayerData = null;
            return false;
        }

        public override void WriteCorePlayerDataRef(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  WriteCorePlayerDataRef");
#endif
            if (corePlayerData == null)
                lockstep.WriteSmallUInt(0u);
            else
                lockstep.WriteSmallUInt(corePlayerData.persistentId);
        }

        public override CorePlayerData ReadCorePlayerDataRef()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ReadCorePlayerDataRef");
#endif
            uint persistentId = lockstep.ReadSmallUInt();
            return playerDataByPersistentId.TryGetValue(persistentId, out DataToken corePlayerDataToken)
                ? (CorePlayerData)corePlayerDataToken.Reference
                : null;
        }

        public override CorePlayerData ReadCorePlayerDataRef(bool isImport)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ReadCorePlayerDataRef");
#endif
            uint persistentId = lockstep.ReadSmallUInt();
            if (isImport)
            {
                if (persistentId != 0u)
                    return null;
                persistentId = persistentIdByImportedPersistentId[persistentId].UInt;
            }
            return playerDataByPersistentId.TryGetValue(persistentId, out DataToken corePlayerDataToken)
                ? (CorePlayerData)corePlayerDataToken.Reference
                : null;
        }

        public override void SendCreateOfflinePlayerDataIA(string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SendCreateOfflinePlayerDataIA");
#endif
            if (displayName == null)
                return;
            lockstep.WriteString(displayName);
            lockstep.SendInputAction(createOfflinePlayerDataIAId);
        }

        [HideInInspector][SerializeField] private uint createOfflinePlayerDataIAId;
        [LockstepInputAction(nameof(createOfflinePlayerDataIAId))]
        public void OnCreateOfflinePlayerDataIA()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnCreateOfflinePlayerDataIA");
#endif
            string displayName = lockstep.ReadString();
            CreateOfflinePlayerDataInGS(displayName);
        }

        public override CorePlayerData CreateOfflinePlayerDataInGS(string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CreateOfflinePlayerDataInGS");
#endif
            if (!isInitialized || displayName == null || playerDataByName.ContainsKey(displayName))
                return null;
            return InitializeOfflinePlayer(displayName);
        }

        public override void SendDeleteOfflinePlayerDataIA(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SendDeleteOfflinePlayerDataIA");
#endif
            if (!corePlayerData.isOffline)
                return;
            lockstep.WriteSmallUInt(corePlayerData.persistentId);
            lockstep.SendInputAction(deleteOfflinePlayerDataIAId);
        }

        [HideInInspector][SerializeField] private uint deleteOfflinePlayerDataIAId;
        [LockstepInputAction(nameof(deleteOfflinePlayerDataIAId))]
        public void OnDeleteOfflinePlayerDataIA()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnDeleteOfflinePlayerDataIA");
#endif
            uint persistentId = lockstep.ReadSmallUInt();
            if (!TryGetCorePlayerDataForPersistentId(persistentId, out CorePlayerData corePlayerData))
                return;
            DeleteOfflinePlayerDataInGS(corePlayerData);
        }

        public override void DeleteOfflinePlayerDataInGS(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  DeleteOfflinePlayerDataInGS");
#endif
            if (!isInitialized || !corePlayerData.isOffline)
                return; // There cannot be any offline player data while isInitialized is false anyway.
            UninitAllPlayerData(corePlayerData, force: true);
            DeleteCorePlayerData(corePlayerData, doRaiseEvent: true);
        }

        public override void SendDeleteAllOfflinePlayerDataIA()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SendDeleteAllOfflinePlayerDataIA");
#endif
            lockstep.SendInputAction(deleteAllOfflinePlayerDataIAId);
        }

        [HideInInspector][SerializeField] private uint deleteAllOfflinePlayerDataIAId;
        [LockstepInputAction(nameof(deleteAllOfflinePlayerDataIAId))]
        public void OnDeleteAllOfflinePlayerDataIA()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnDeleteAllOfflinePlayerDataIA");
#endif
            DeleteAllOfflinePlayerDataInGS();
        }

        public override void DeleteAllOfflinePlayerDataInGS()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  DeleteAllOfflinePlayerDataInGS");
#endif
            if (!isInitialized) // There cannot be any offline player data while isInitialized is false anyway.
                return;
            for (int i = allPlayerDataCount - 1; i >= 0; i--)
            {
                CorePlayerData corePlayerData = allPlayerData[i];
                if (corePlayerData.isOffline)
                    DeleteOfflinePlayerDataInGS(corePlayerData);
            }
        }

        private PlayerData NewPlayerData(string className, CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  NewPlayerData");
#endif
            PlayerData playerData = (PlayerData)wannaBeClasses.NewDynamic(className);
            playerData.core = corePlayerData;
            return playerData;
        }

        private CorePlayerData CreateNewCorePlayerDataCommon(string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CreateNewCorePlayerDataCommon");
#endif
            CorePlayerData corePlayerData = wannaBeClasses.New<CorePlayerData>(nameof(CorePlayerData));
            corePlayerData.persistentId = nextPersistentId++;
            corePlayerData.displayName = displayName;
            PlayerData[] customPlayerData = new PlayerData[playerDataClassNamesCount];
            corePlayerData.customPlayerData = customPlayerData;
            corePlayerData.index = allPlayerDataCount;
            ArrList.Add(ref allPlayerData, ref allPlayerDataCount, corePlayerData);
            playerDataByPersistentId.Add(corePlayerData.persistentId, corePlayerData);
            return corePlayerData;
        }

        private CorePlayerData CreateNewOnlineCorePlayerData(uint playerId, string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CreateNewOnlineCorePlayerData");
#endif
            CorePlayerData corePlayerData = CreateNewCorePlayerDataCommon(displayName);
            corePlayerData.playerId = playerId;
            corePlayerData.playerApi = VRCPlayerApi.GetPlayerById((int)playerId);
            bool isLocal = playerId == localPlayerId;
            corePlayerData.isLocal = isLocal;
            if (isLocal)
                localPlayerData = corePlayerData;
            return corePlayerData;
        }

        private CorePlayerData CreateNewOfflineCorePlayerData(string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CreateNewOfflineCorePlayerData");
#endif
            CorePlayerData corePlayerData = CreateNewCorePlayerDataCommon(displayName);
            corePlayerData.isOffline = true;
            playerDataByName.Add(displayName, corePlayerData);
            return corePlayerData;
        }

        private void CreatePlayerDataForNewCorePlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CreatePlayerDataForNewCorePlayerData");
#endif
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                PlayerData playerData = NewPlayerData(playerDataClassNames[i], corePlayerData);
                customPlayerData[i] = playerData;
                playerData.OnPlayerDataInit(isAboutToBeImported: false);
            }
        }

        private CorePlayerData InitializeOfflinePlayer(string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  InitializeOfflinePlayer");
#endif
            CorePlayerData corePlayerData = CreateNewOfflineCorePlayerData(displayName);
            CreatePlayerDataForNewCorePlayerData(corePlayerData);
            RaiseOnPlayerDataCreated(corePlayerData);
            return corePlayerData;
        }

        private void InitializeNewPlayer(uint playerId, string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  InitializeNewPlayer");
#endif
            CorePlayerData corePlayerData = CreateNewOnlineCorePlayerData(playerId, displayName);
            playerDataByPlayerId.Add(playerId, corePlayerData);
            playerDataByName.Add(displayName, corePlayerData);
            CreatePlayerDataForNewCorePlayerData(corePlayerData);
            RaiseOnPlayerDataCreated(corePlayerData);
        }

        private void InitializeNewOvershadowedPlayer(uint playerId, CorePlayerData overshadowingPlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  InitializeNewOvershadowedPlayer");
#endif
            CorePlayerData corePlayerData = CreateNewOnlineCorePlayerData(playerId, overshadowingPlayerData.displayName);
            playerDataByPlayerId.Add(playerId, corePlayerData);
            corePlayerData.overshadowingPlayerData = overshadowingPlayerData;
            if (!overshadowingPlayerData.IsOvershadowing)
            {
                overshadowingPlayerData.firstOvershadowedPlayerData = corePlayerData;
                overshadowingPlayerData.lastOvershadowedPlayerData = corePlayerData;
                CreatePlayerDataForNewCorePlayerData(corePlayerData);
                RaiseOnPlayerDataCreated(corePlayerData);
                RaiseOnPlayerDataStartedBeingOvershadowed(corePlayerData);
                RaiseOnPlayerDataStartedOvershadowing(overshadowingPlayerData);
            }
            else
            {
                CorePlayerData last = overshadowingPlayerData.lastOvershadowedPlayerData;
                last.nextOvershadowedPlayerData = corePlayerData;
                corePlayerData.prevOvershadowedPlayerData = last;
                overshadowingPlayerData.lastOvershadowedPlayerData = corePlayerData;
                CreatePlayerDataForNewCorePlayerData(corePlayerData);
                RaiseOnPlayerDataCreated(corePlayerData);
                RaiseOnPlayerDataStartedBeingOvershadowed(corePlayerData);
            }
        }

        private void InitializeRejoiningPlayer(uint playerId, CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  InitializeRejoiningPlayer");
#endif
            corePlayerData.isOffline = false;
            corePlayerData.playerId = playerId;
            corePlayerData.playerApi = VRCPlayerApi.GetPlayerById((int)playerId);
            bool isLocal = playerId == localPlayerId;
            corePlayerData.isLocal = isLocal;
            if (isLocal)
                localPlayerData = corePlayerData;
            playerDataByPlayerId.Add(playerId, corePlayerData);
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            for (int i = 0; i < playerDataClassNamesCount; i++)
                customPlayerData[i].OnPlayerDataRejoin();
            RaiseOnPlayerDataWentOnline(corePlayerData);
        }

        private void InitializePlayer(uint playerId)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  InitializePlayer");
#endif
            string displayName = lockstep.GetDisplayName(playerId);
            if (!playerDataByName.TryGetValue(displayName, out DataToken playerDataToken))
            {
                InitializeNewPlayer(playerId, displayName);
                return;
            }
            CorePlayerData playerData = (CorePlayerData)playerDataToken.Reference;
            if (playerData.isOffline)
                InitializeRejoiningPlayer(playerId, playerData);
            else
                InitializeNewOvershadowedPlayer(playerId, playerData);
        }

        [LockstepEvent(LockstepEventType.OnInit, Order = -10000)]
        public void OnInit()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnInit");
#endif
            RaiseOnPrePlayerDataManagerInit();
            isInitialized = true;
            InitializePlayer(lockstep.MasterPlayerId);
            RaiseOnPostPlayerDataManagerInit();
        }

        [LockstepEvent(LockstepEventType.OnPreClientJoined, Order = -10000)]
        public void OnPreClientJoined()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnPreClientJoined");
#endif
            InitializePlayer(lockstep.JoinedPlayerId);
        }

        [LockstepEvent(LockstepEventType.OnClientLeft, Order = 10000)]
        public void OnClientLeft()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnClientLeft");
#endif
            uint playerId = lockstep.LeftPlayerId;
            playerDataByPlayerId.Remove(playerId, out DataToken corePlayerDataToken);
            CorePlayerData corePlayerData = (CorePlayerData)corePlayerDataToken.Reference;

            if (corePlayerData.IsOvershadowed || corePlayerData.IsOvershadowing)
            {
                // Overshadowed player data cannot go offline, it does not exist in the playerDataByName lut.
                // Same for when a player was overshadowing another player, as the other player becomes no
                // longer overshadowed, thus taking the the leaving player's place in playerDataByName.
                UninitAllPlayerData(corePlayerData, force: true);
            }
            else
            {
                bool shouldPersist = UninitOrPersistPlayerData(corePlayerData);
                if (shouldPersist)
                {
                    corePlayerData.isOffline = true;
                    RaiseOnPlayerDataWentOffline(corePlayerData);
                    corePlayerData.playerId = 0u; // Explicitly after raising the event.
                    return;
                }
            }

            DeleteCorePlayerData(corePlayerData, doRaiseEvent: true);
        }

        private void UninitAllPlayerData(CorePlayerData corePlayerData, bool force)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  UninitAllPlayerData - force: {force}");
#endif
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                PlayerData playerData = customPlayerData[i];
                playerData.OnPlayerDataUninit(force);
                playerData.DecrementRefsCount();
                customPlayerData[i] = null;
            }
        }

        private bool UninitOrPersistPlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  UninitOrPersistPlayerData");
#endif
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            for (int i = 0; i < playerDataClassNamesCount; i++)
                if (customPlayerData[i].PersistPlayerDataWhileOffline())
                {
                    for (int j = 0; j < playerDataClassNamesCount; j++)
                        customPlayerData[j].OnPlayerDataLeft();
                    return true;
                }

            UninitAllPlayerData(corePlayerData, force: false);
            return false;
        }

        private void ResolveOvershadowingUponRemoval(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ResolveOvershadowingUponRemoval");
#endif
            CorePlayerData next; // C# moment. Cannot declare that local in the if and afterwards outside too.
            if (corePlayerData.IsOvershadowed)
            {
                CorePlayerData prev = corePlayerData.prevOvershadowedPlayerData;
                next = corePlayerData.nextOvershadowedPlayerData;
                CorePlayerData overshadowing = corePlayerData.overshadowingPlayerData;

                if (prev != null)
                    prev.nextOvershadowedPlayerData = next;
                else
                    overshadowing.firstOvershadowedPlayerData = next;

                if (next != null)
                    next.prevOvershadowedPlayerData = prev;
                else
                    overshadowing.lastOvershadowedPlayerData = prev;

                if (!overshadowing.IsOvershadowing)
                    RaiseOnPlayerDataStoppedOvershadowing(overshadowing);
                return;
            }

            if (!corePlayerData.IsOvershadowing)
            {
                playerDataByName.Remove(corePlayerData.displayName);
                return;
            }

            CorePlayerData playerTakingOver = corePlayerData.firstOvershadowedPlayerData;
            playerTakingOver.overshadowingPlayerData = null;
            playerDataByName[corePlayerData.displayName] = playerTakingOver;
            next = playerTakingOver.nextOvershadowedPlayerData;
            playerTakingOver.nextOvershadowedPlayerData = null;
            RaiseOnPlayerDataStoppedBeingOvershadowed(playerTakingOver);

            if (next == null)
                return;

            next.prevOvershadowedPlayerData = null;
            playerTakingOver.firstOvershadowedPlayerData = next;
            playerTakingOver.lastOvershadowedPlayerData = corePlayerData.lastOvershadowedPlayerData;
            RaiseOnPlayerDataStartedOvershadowing(playerTakingOver);
            do
            {
                next.overshadowingPlayerData = playerTakingOver;
                RaiseOnPlayerDataOvershadowingPlayerChanged(next);
                next = next.nextOvershadowedPlayerData;
            }
            while (next != null);
        }

        private void DeleteCorePlayerData(CorePlayerData corePlayerData, bool doRaiseEvent)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  DeleteCorePlayerData");
#endif
            ResolveOvershadowingUponRemoval(corePlayerData);
            playerDataByPersistentId.Remove(corePlayerData.persistentId);
            int index = corePlayerData.index;
            allPlayerData[index] = allPlayerData[--allPlayerDataCount];
            allPlayerData[index].index = index;
            corePlayerData.isDeleted = true;
            if (doRaiseEvent)
                RaiseOnPlayerDataDeleted(corePlayerData);
            corePlayerData.DecrementRefsCount();
        }

        #region Serialization

        private bool DeSerializationIsRunningLong()
        {
            bool result = deSerializationSw.ElapsedMilliseconds > MaxWorkMSPerFrame;
            if (result)
                lockstep.FlagToContinueNextFrame();
            return result;
        }

        private void SerializeExpectedPlayerDataClassNames()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SerializeExpectedPlayerDataClassNames");
#endif
            lockstep.WriteSmallUInt((uint)playerDataClassNamesCount);
            for (int i = 0; i < playerDataClassNamesCount; i++)
                lockstep.WriteString(playerDataClassNames[i]);
        }

        private bool ValidatePlayerDataClassNames(out string errorMessage)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ValidatePlayerDataClassNames");
#endif
            int count = (int)lockstep.ReadSmallUInt();
            string[] classNames = new string[count];
            for (int i = 0; i < count; i++)
                classNames[i] = lockstep.ReadString();
            string got = string.Join(", ", classNames);
            string expected = string.Join(", ", playerDataClassNames, 0, playerDataClassNamesCount);
            bool valid = count == playerDataClassNamesCount && got == expected;
            errorMessage = valid ? null : $"Registered player data class names mismatch, expected '{expected}', got '{got}'.";
            return valid;
        }

        private void SerializeCustomPlayerData(PlayerData playerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SerializeCustomPlayerData");
#endif
            playerData.Serialize(isExport: false);
        }

        private void DeserializeCustomPlayerData(CorePlayerData corePlayerData, int classNameIndex)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  DeserializeCustomPlayerData");
#endif
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            PlayerData playerData = customPlayerData[classNameIndex];
            if (playerData == null)
            {
                // Doesn't unconditionally create a new one as this might be a continuation from prev frame.
                playerData = NewPlayerData(playerDataClassNames[classNameIndex], corePlayerData);
                customPlayerData[classNameIndex] = playerData;
            }
            playerData.Deserialize(isImport: false, importedDataVersion: 0u);
        }

        private void SerializeCorePlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SerializeCorePlayerData");
#endif
            if (suspendedInCustomPlayerData)
                suspendedInCustomPlayerData = false;
            else
            {
                lockstep.WriteSmallUInt(corePlayerData.persistentId);

                lockstep.WriteFlags(
                    corePlayerData.isOffline,
                    corePlayerData.IsOvershadowed,
                    corePlayerData.prevOvershadowedPlayerData != null,
                    corePlayerData.nextOvershadowedPlayerData != null,
                    corePlayerData.IsOvershadowing);

                if (corePlayerData.isOffline)
                    lockstep.WriteString(corePlayerData.displayName);
                else
                    lockstep.WriteSmallUInt(corePlayerData.playerId);
                if (corePlayerData.IsOvershadowed)
                    lockstep.WriteSmallUInt((uint)corePlayerData.overshadowingPlayerData.index);
                if (corePlayerData.prevOvershadowedPlayerData != null)
                    lockstep.WriteSmallUInt((uint)corePlayerData.prevOvershadowedPlayerData.index);
                if (corePlayerData.nextOvershadowedPlayerData != null)
                    lockstep.WriteSmallUInt((uint)corePlayerData.nextOvershadowedPlayerData.index);
                if (corePlayerData.IsOvershadowing)
                {
                    lockstep.WriteSmallUInt((uint)corePlayerData.firstOvershadowedPlayerData.index);
                    lockstep.WriteSmallUInt((uint)corePlayerData.lastOvershadowedPlayerData.index);
                }
            }

            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            while (suspendedIndexInCustomPlayerDataArray < playerDataClassNamesCount)
            {
                if (DeSerializationIsRunningLong())
                {
                    suspendedInCustomPlayerData = true;
                    return;
                }
                SerializeCustomPlayerData(customPlayerData[suspendedIndexInCustomPlayerDataArray]);
                if (lockstep.FlaggedToContinueNextFrame)
                {
                    suspendedInCustomPlayerData = true;
                    return;
                }
                suspendedIndexInCustomPlayerDataArray++;
            }
            suspendedIndexInCustomPlayerDataArray = 0;
        }

        private void DeserializeCorePlayerData(int index)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  DeserializeCorePlayerData");
#endif
            CorePlayerData corePlayerData = allPlayerData[index];
            if (suspendedInCustomPlayerData)
                suspendedInCustomPlayerData = false;
            else
            {
                corePlayerData.index = index;
                corePlayerData.persistentId = lockstep.ReadSmallUInt();
                playerDataByPersistentId.Add(corePlayerData.persistentId, corePlayerData);

                lockstep.ReadFlags(
                    out corePlayerData.isOffline,
                    out bool isOvershadowed,
                    out bool hasPrevOvershadowed,
                    out bool hasNextOvershadowed,
                    out bool isOvershadowing);

                if (corePlayerData.isOffline)
                {
                    corePlayerData.displayName = lockstep.ReadString();
                    // playerId remains 0u, as it should.
                }
                else
                {
                    uint playerId = lockstep.ReadSmallUInt();
                    playerDataByPlayerId.Add(playerId, corePlayerData);
                    corePlayerData.playerId = playerId;
                    corePlayerData.playerApi = VRCPlayerApi.GetPlayerById((int)playerId);
                    bool isLocal = playerId == localPlayerId;
                    corePlayerData.isLocal = isLocal;
                    if (isLocal)
                        localPlayerData = corePlayerData;
                    corePlayerData.displayName = lockstep.GetDisplayName(playerId);
                }
                if (isOvershadowed)
                    corePlayerData.overshadowingPlayerData = allPlayerData[lockstep.ReadSmallUInt()];
                else
                    playerDataByName.Add(corePlayerData.displayName, corePlayerData);
                if (hasPrevOvershadowed)
                    corePlayerData.prevOvershadowedPlayerData = allPlayerData[lockstep.ReadSmallUInt()];
                if (hasNextOvershadowed)
                    corePlayerData.nextOvershadowedPlayerData = allPlayerData[lockstep.ReadSmallUInt()];
                if (isOvershadowing)
                {
                    corePlayerData.firstOvershadowedPlayerData = allPlayerData[lockstep.ReadSmallUInt()];
                    corePlayerData.lastOvershadowedPlayerData = allPlayerData[lockstep.ReadSmallUInt()];
                }

                corePlayerData.customPlayerData = new PlayerData[playerDataClassNamesCount];
            }

            while (suspendedIndexInCustomPlayerDataArray < playerDataClassNamesCount)
            {
                if (DeSerializationIsRunningLong())
                {
                    suspendedInCustomPlayerData = true;
                    return;
                }
                DeserializeCustomPlayerData(corePlayerData, suspendedIndexInCustomPlayerDataArray);
                if (lockstep.FlaggedToContinueNextFrame)
                {
                    suspendedInCustomPlayerData = true;
                    return;
                }
                suspendedIndexInCustomPlayerDataArray++;
            }
            suspendedIndexInCustomPlayerDataArray = 0;
        }

        private void SerializeAllCorePlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SerializeAllCorePlayerData");
#endif
            if (!lockstep.IsContinuationFromPrevFrame)
                lockstep.WriteSmallUInt((uint)allPlayerDataCount);
            while (suspendedIndexInCorePlayerDataArray < allPlayerDataCount)
            {
                if (DeSerializationIsRunningLong())
                    return;
                SerializeCorePlayerData(allPlayerData[suspendedIndexInCorePlayerDataArray]);
                if (suspendedInCustomPlayerData)
                    return;
                suspendedIndexInCorePlayerDataArray++;
            }
            suspendedIndexInCorePlayerDataArray = 0;
        }

        private void DeserializeAllCorePlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  DeserializeAllCorePlayerData");
#endif
            if (deserializationStage == 0)
            {
                allPlayerDataCount = (int)lockstep.ReadSmallUInt();
                ArrList.EnsureCapacity(ref allPlayerData, allPlayerDataCount);
                deserializationStage++;
            }

            if (deserializationStage == 1)
            {
                // Populate with empty instances so overshadowingPlayerData can be assigned immediately in the
                // deserialization pass.
                while (suspendedIndexInCorePlayerDataArray < allPlayerDataCount)
                {
                    if (DeSerializationIsRunningLong())
                        return;
                    allPlayerData[suspendedIndexInCorePlayerDataArray] = wannaBeClasses.New<CorePlayerData>(nameof(CorePlayerData));
                    suspendedIndexInCorePlayerDataArray++;
                }
                suspendedIndexInCorePlayerDataArray = 0;
                deserializationStage++;
            }

            if (deserializationStage == 2)
            {
                while (suspendedIndexInCorePlayerDataArray < allPlayerDataCount)
                {
                    if (DeSerializationIsRunningLong())
                        return;
                    DeserializeCorePlayerData(suspendedIndexInCorePlayerDataArray);
                    if (suspendedInCustomPlayerData)
                        return;
                    suspendedIndexInCorePlayerDataArray++;
                }
                suspendedIndexInCorePlayerDataArray = 0;
                deserializationStage = 0;
            }
        }

        private uint CountNonOvershadowedPlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CountNonOvershadowedPlayerData");
#endif
            uint count = 0;
            for (int i = 0; i < allPlayerDataCount; i++)
                if (!allPlayerData[i].IsOvershadowed)
                    count++;
            return count;
        }

        private uint CountPlayerDataSupportingExport(PlayerData[] customPlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CountPlayerDataSupportingExport");
#endif
            uint toExportCount = 0;
            for (int i = 0; i < playerDataClassNamesCount; i++)
                if (customPlayerData[i].SupportsImportExport)
                    toExportCount++;
            return toExportCount;
        }

        private void ExportCustomPlayerDataMetadata(PlayerData playerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ExportCustomPlayerDataMetadata");
#endif
            lockstep.WriteString(playerData.PlayerDataInternalName);
            lockstep.WriteString(playerData.PlayerDataDisplayName);
            lockstep.WriteSmallUInt(playerData.DataVersion);
        }

        private void ImportCustomPlayerDataMetadata()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ImportCustomPlayerDataMetadata");
#endif
            importSuspendedInternalName = lockstep.ReadString();
            importSuspendedDisplayName = lockstep.ReadString();
            importSuspendedDataVersion = lockstep.ReadSmallUInt();
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ImportCustomPlayerDataMetadata (inner) - internalName: {importSuspendedInternalName}, displayName: {importSuspendedDisplayName}, dataVersion: {importSuspendedDataVersion}");
#endif
        }

        private void ExportCorePlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ExportCorePlayerData");
#endif
            if (corePlayerData.IsOvershadowed)
                return;
            lockstep.WriteSmallUInt(corePlayerData.persistentId);
            lockstep.WriteString(corePlayerData.displayName);
        }

        private CorePlayerData ImportCorePlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ImportCorePlayerData");
#endif
            uint importedPersistentId = lockstep.ReadSmallUInt();
            string displayName = lockstep.ReadString();
            CorePlayerData corePlayerData = GetOrCreateCorePlayerDataForImport(displayName);
            corePlayerData.importedPersistentId = importedPersistentId;
            persistentIdByImportedPersistentId.Add(importedPersistentId, corePlayerData.persistentId);
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ImportCorePlayerData (inner) - displayName: {displayName}, importedPersistentId: {importedPersistentId}, corePlayerData.persistentId: {corePlayerData.persistentId}");
#endif
            return corePlayerData;
        }

        private CorePlayerData GetOrCreateCorePlayerDataForImport(string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetOrCreateCorePlayerDataForImport");
#endif
            if (playerDataByName.TryGetValue(displayName, out DataToken playerDataToken))
                return (CorePlayerData)playerDataToken.Reference;
            return CreateNewOfflineCorePlayerData(displayName);
        }

        private void ExportAllCustomPlayerData(int classNameIndex)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ExportAllCustomPlayerData");
#endif
            suspendedInCustomPlayerData = false;

            while (suspendedIndexInCorePlayerDataArray < allPlayerDataCount)
            {
                if (DeSerializationIsRunningLong())
                {
                    suspendedInCustomPlayerData = true;
                    return;
                }
                CorePlayerData corePlayerData = allPlayerData[suspendedIndexInCorePlayerDataArray];
                if (corePlayerData.IsOvershadowed)
                {
                    suspendedIndexInCorePlayerDataArray++;
                    continue;
                }
                PlayerData playerData = corePlayerData.customPlayerData[classNameIndex];
                playerData.Serialize(isExport: true);
                if (lockstep.FlaggedToContinueNextFrame)
                {
                    suspendedInCustomPlayerData = true;
                    return;
                }
                suspendedIndexInCorePlayerDataArray++;
            }
            suspendedIndexInCorePlayerDataArray = 0;
        }

        private bool TryImportAllCustomPlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  TryImportAllCustomPlayerData");
#endif
            if (suspendedInCustomPlayerData)
                suspendedInCustomPlayerData = false;
            else
            {
                importSuspendedClassNameIndex = ArrList.IndexOf(ref playerDataInternalNames, ref playerDataInternalNamesCount, importSuspendedInternalName);
#if PLAYER_DATA_DEBUG
                Debug.Log($"[PlayerDataDebug] Manager  TryImportAllCustomPlayerData (inner) - importSuspendedClassNameIndex: {importSuspendedClassNameIndex}, playerDataClassName: {(importSuspendedClassNameIndex == -1 ? "<null>" : playerDataClassNames[importSuspendedClassNameIndex])}");
#endif
                if (importSuspendedClassNameIndex == -1)
                    return false;

                PlayerData dummyPlayerData = GetDummyCustomPlayerData()[importSuspendedClassNameIndex];
#if PLAYER_DATA_DEBUG
                Debug.Log($"[PlayerDataDebug] Manager  TryImportAllCustomPlayerData (inner) - SupportsImportExport: {dummyPlayerData.SupportsImportExport}, LowestSupportedDataVersion: {dummyPlayerData.LowestSupportedDataVersion}, importSuspendedDataVersion: {importSuspendedDataVersion}");
#endif
                if (!dummyPlayerData.SupportsImportExport || dummyPlayerData.LowestSupportedDataVersion > importSuspendedDataVersion)
                    return false;

                importSuspendedPresentClassNames[importSuspendedClassNameIndex] = true;
                importSuspendedAnyClassNamePresent = true;
            }

            int count = allImportedPlayerData.Length;
            while (suspendedIndexInCorePlayerDataArray < count)
            {
                if (DeSerializationIsRunningLong())
                {
                    suspendedInCustomPlayerData = true;
                    return false; // Return value does not matter.
                }
                CorePlayerData corePlayerData = allImportedPlayerData[suspendedIndexInCorePlayerDataArray];
                PlayerData[] customPlayerData = corePlayerData.customPlayerData;
                PlayerData playerData = customPlayerData[importSuspendedClassNameIndex];
#if PLAYER_DATA_DEBUG
                Debug.Log($"[PlayerDataDebug] Manager  TryImportAllCustomPlayerData (inner) - corePlayerData.displayName: {corePlayerData.displayName}");
#endif
                if (playerData == null)
                {
                    playerData = NewPlayerData(playerDataClassNames[importSuspendedClassNameIndex], corePlayerData);
                    customPlayerData[importSuspendedClassNameIndex] = playerData;
                    playerData.OnPlayerDataInit(isAboutToBeImported: true);
                }
                playerData.Deserialize(isImport: true, importSuspendedDataVersion);
                if (lockstep.FlaggedToContinueNextFrame)
                {
                    suspendedInCustomPlayerData = true;
                    return false; // Return value does not matter.
                }
                suspendedIndexInCorePlayerDataArray++;
            }
            suspendedIndexInCorePlayerDataArray = 0;

            return true;
        }

        private void CleanUpEmptyImportedCorePlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CleanUpEmptyImportedCorePlayerData");
#endif
            for (int i = newlyCreatedImportedPlayerDataCount - 1; i >= 0; i--)
                DeleteCorePlayerData(newlyCreatedImportedPlayerData[i], doRaiseEvent: false);
        }

        private void ImportPopulateMissingCustomPlayerData(int classNameIndex)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  ImportPopulateMissingCustomPlayerData");
#endif
            int count = allImportedPlayerData.Length;
            while (suspendedIndexInCorePlayerDataArray < count)
            {
                if (DeSerializationIsRunningLong())
                    return;
                CorePlayerData corePlayerData = allImportedPlayerData[suspendedIndexInCorePlayerDataArray];
                PlayerData[] customPlayerData = corePlayerData.customPlayerData;
                PlayerData playerData = customPlayerData[classNameIndex];
                if (playerData == null)
                {
                    playerData = NewPlayerData(playerDataClassNames[classNameIndex], corePlayerData);
                    customPlayerData[classNameIndex] = playerData;
                    playerData.OnPlayerDataInit(isAboutToBeImported: false);
                }
                suspendedIndexInCorePlayerDataArray++;
            }
            suspendedIndexInCorePlayerDataArray = 0;
        }

        private PlayerData[] GetDummyCustomPlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetDummyCustomPlayerData");
#endif
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPlayerId[lockstep.MasterPlayerId].Reference;
            return corePlayerData.customPlayerData;
        }

        private void Export()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  Export");
#endif
            if (exportStage == 0)
            {
                lockstep.WriteSmallUInt(CountNonOvershadowedPlayerData());
                exportStage++;
            }

            if (exportStage == 1)
            {
                while (suspendedIndexInCorePlayerDataArray < allPlayerDataCount)
                {
                    if (DeSerializationIsRunningLong())
                        return;
                    ExportCorePlayerData(allPlayerData[suspendedIndexInCorePlayerDataArray]);
                    suspendedIndexInCorePlayerDataArray++;
                }
                suspendedIndexInCorePlayerDataArray = 0;
                exportStage++;
            }

            if (exportStage == 2)
            {
                PlayerData[] customPlayerData = GetDummyCustomPlayerData();
                lockstep.WriteSmallUInt(CountPlayerDataSupportingExport(customPlayerData));
                exportStage++;
            }

            if (exportStage == 3)
            {
                PlayerData[] customPlayerData = GetDummyCustomPlayerData();
                while (suspendedIndexInCustomPlayerDataArray < playerDataClassNamesCount)
                {
                    if (DeSerializationIsRunningLong())
                        return;
                    PlayerData playerData = customPlayerData[suspendedIndexInCustomPlayerDataArray];
                    if (!playerData.SupportsImportExport)
                    {
                        suspendedIndexInCustomPlayerDataArray++;
                        continue;
                    }
                    if (!suspendedInCustomPlayerData)
                    {
                        ExportCustomPlayerDataMetadata(playerData);
                        exportUnknownSizeScopeSizePosition = OpenUnknownSizeScope();
                    }
                    ExportAllCustomPlayerData(suspendedIndexInCustomPlayerDataArray);
                    if (suspendedInCustomPlayerData)
                        return;
                    CloseUnknownSizeScope(exportUnknownSizeScopeSizePosition);
                    suspendedIndexInCustomPlayerDataArray++;
                }
                suspendedIndexInCustomPlayerDataArray = 0;
                exportStage = 0;
            }
        }

        private void Import(uint importedDataVersion)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  Import");
#endif
            if (importStage == 0)
            {
                allImportedPlayerData = new CorePlayerData[lockstep.ReadSmallUInt()];
                newlyCreatedImportedPlayerData = new CorePlayerData[ArrList.MinCapacity];
                newlyCreatedImportedPlayerDataCount = 0;
#if PLAYER_DATA_DEBUG
                Debug.Log($"[PlayerDataDebug] Manager  Import (inner) - allImportedPlayerData.Length: {allImportedPlayerData.Length}");
#endif
                importStage++;
            }

            if (importStage == 1)
            {
                int length = allImportedPlayerData.Length;
                while (suspendedIndexInCorePlayerDataArray < length)
                {
                    if (DeSerializationIsRunningLong())
                        return;
                    allImportedPlayerData[suspendedIndexInCorePlayerDataArray] = ImportCorePlayerData();
                    suspendedIndexInCorePlayerDataArray++;
                }
                suspendedIndexInCorePlayerDataArray = 0;
                importStage++;
            }

            if (importStage == 2)
            {
                importedCustomPlayerDataCount = (int)lockstep.ReadSmallUInt();
                importSuspendedPresentClassNames = new bool[playerDataClassNamesCount];
                importSuspendedAnyClassNamePresent = false;
#if PLAYER_DATA_DEBUG
                Debug.Log($"[PlayerDataDebug] Manager  Import (inner) - importedCustomPlayerDataCount: {importedCustomPlayerDataCount}, playerDataClassNamesCount: {playerDataClassNamesCount}");
#endif
                importStage++;
            }

            if (importStage == 3)
            {
                while (suspendedIndexInCustomPlayerDataArray < importedCustomPlayerDataCount)
                {
                    if (DeSerializationIsRunningLong())
                        return;
                    if (!suspendedInCustomPlayerData)
                    {
                        ImportCustomPlayerDataMetadata();
                        importSuspendedScopeByteSize = lockstep.ReadInt();
                    }
                    bool success = TryImportAllCustomPlayerData();
                    if (suspendedInCustomPlayerData)
                        return;
                    if (!success)
                    {
#if PLAYER_DATA_DEBUG
                        Debug.Log($"[PlayerDataDebug] Manager  Import (inner) - discarding {importSuspendedScopeByteSize} bytes");
#endif
                        lockstep.ReadBytes(importSuspendedScopeByteSize, skip: true);
                    }
                    suspendedIndexInCustomPlayerDataArray++;
                }
                suspendedIndexInCustomPlayerDataArray = 0;
                importStage++;
            }

            if (importStage == 4)
            {
                if (!importSuspendedAnyClassNamePresent)
                {
                    CleanUpEmptyImportedCorePlayerData();
                    importStage++; // Skip stage 5.
                }
                importStage++;
            }

            if (importStage == 5)
            {
                while (suspendedIndexInCustomPlayerDataArray < playerDataClassNamesCount)
                {
                    if (importSuspendedPresentClassNames[suspendedIndexInCustomPlayerDataArray])
                    {
                        suspendedIndexInCustomPlayerDataArray++;
                        continue;
                    }
                    ImportPopulateMissingCustomPlayerData(suspendedIndexInCustomPlayerDataArray);
                    if (lockstep.FlaggedToContinueNextFrame)
                        return;
                    suspendedIndexInCustomPlayerDataArray++;
                }
                suspendedIndexInCustomPlayerDataArray = 0;
                importStage++;
            }

            if (importStage == 6)
            {
                importSuspendedPresentClassNames = null;
                importStage = 0;
            }
        }

        [LockstepEvent(LockstepEventType.OnImportFinishingUp, Order = -10000)]
        public void OnImportFinishingUp()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnImportFinishingUp");
#endif
            if (allImportedPlayerData == null) // The imported data did not contain the player data game state.
                return;
            // Can clean up now, because any other systems that got imported should have used their own game
            // state deserialize function in order to finish resolving their associated player data import.
            CleanUpUnnecessaryOfflineImportedPlayerData(allImportedPlayerData);
            allImportedPlayerData = null; // Free memory.
        }

        [LockstepEvent(LockstepEventType.OnImportFinished, Order = 10000)]
        public void OnImportFinished()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OnImportFinished");
#endif
            // This happens in OnImportFinished with Order 10000 to allow systems using default Order 0 to
            // still be able to use the import id remapping.
            persistentIdByImportedPersistentId.Clear();
        }

        private void CleanUpUnnecessaryOfflineImportedPlayerData(CorePlayerData[] allImportedPlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CleanUpUnnecessaryOfflineImportedPlayerData");
#endif
            // TODO: Spread out across frames?
            for (int i = newlyCreatedImportedPlayerDataCount - 1; i >= 0; i--)
            {
                CorePlayerData corePlayerData = newlyCreatedImportedPlayerData[i];
                PlayerData[] customPlayerData = corePlayerData.customPlayerData;
                bool doKeep = false;
                for (int j = 0; j < playerDataClassNamesCount; j++)
                    if (customPlayerData[j].PersistPlayerDataPostImportWhileOffline())
                    {
                        doKeep = true;
                        break;
                    }
                if (doKeep)
                    continue;
                UninitAllPlayerData(corePlayerData, force: false);
                DeleteCorePlayerData(corePlayerData, doRaiseEvent: false);
            }
        }

        public override uint GetPersistentIdFromImportedId(uint importedPersistentId)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  GetPersistentIdFromImportedId - importedPersistentId: {importedPersistentId}, result: {(importedPersistentId == 0u ? 0u : persistentIdByImportedPersistentId[importedPersistentId].UInt)}");
#endif
            return importedPersistentId == 0u
                ? 0u
                : persistentIdByImportedPersistentId[importedPersistentId].UInt;
        }

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  SerializeGameState");
#endif
            deSerializationSw.Reset();
            deSerializationSw.Start();
            if (isExport)
            {
                Export();
                return;
            }
            if (!lockstep.IsContinuationFromPrevFrame)
            {
                SerializeExpectedPlayerDataClassNames();
                lockstep.WriteSmallUInt(nextPersistentId);
            }
            SerializeAllCorePlayerData();
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  DeserializeGameState");
#endif
            deSerializationSw.Reset();
            deSerializationSw.Start();
            if (isImport)
            {
                Import(importedDataVersion);
                return null;
            }
            if (!lockstep.IsContinuationFromPrevFrame)
            {
                if (!ValidatePlayerDataClassNames(out string errorMessage))
                    return errorMessage;
                nextPersistentId = lockstep.ReadSmallUInt();
            }
            DeserializeAllCorePlayerData();
            isInitialized = true;
            return null;
        }

        private int OpenUnknownSizeScope()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  OpenUnknownSizeScope");
#endif
            int sizePosition = lockstep.WriteStreamPosition;
            lockstep.WriteStreamPosition += 4;
            return sizePosition;
        }

        private void CloseUnknownSizeScope(int sizePosition)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerDataDebug] Manager  CloseUnknownSizeScope");
#endif
            int stopPosition = lockstep.WriteStreamPosition;
            lockstep.WriteStreamPosition = sizePosition;
            lockstep.WriteInt(stopPosition - sizePosition - 4);
            lockstep.WriteStreamPosition = stopPosition;
        }

        #endregion

        #region EventDispatcher

        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onRegisterCustomPlayerDataListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onAllCustomPlayerDataRegisteredListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPrePlayerDataManagerInitListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPostPlayerDataManagerInitListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataCreatedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataDeletedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataWentOfflineListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataWentOnlineListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataStartedOvershadowingListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataStoppedOvershadowingListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataStartedBeingOvershadowedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataStoppedBeingOvershadowedListeners;
        [HideInInspector][SerializeField] private UdonSharpBehaviour[] onPlayerDataOvershadowingPlayerChangedListeners;

        private CorePlayerData playerDataForEvent;
        public override CorePlayerData PlayerDataForEvent => playerDataForEvent;

        private void RaiseOnRegisterCustomPlayerData()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onRegisterCustomPlayerDataListeners, nameof(PlayerDataEventType.OnRegisterCustomPlayerData));
        }

        private void RaiseOnAllCustomPlayerDataRegistered()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onAllCustomPlayerDataRegisteredListeners, nameof(PlayerDataEventType.OnAllCustomPlayerDataRegistered));
        }

        private void RaiseOnPrePlayerDataManagerInit()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPrePlayerDataManagerInitListeners, nameof(PlayerDataEventType.OnPrePlayerDataManagerInit));
        }

        private void RaiseOnPostPlayerDataManagerInit()
        {
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPostPlayerDataManagerInitListeners, nameof(PlayerDataEventType.OnPostPlayerDataManagerInit));
        }

        private void RaiseOnPlayerDataCreated(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataCreatedListeners, nameof(PlayerDataEventType.OnPlayerDataCreated));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataDeleted(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataDeletedListeners, nameof(PlayerDataEventType.OnPlayerDataDeleted));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataWentOffline(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataWentOfflineListeners, nameof(PlayerDataEventType.OnPlayerDataWentOffline));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataWentOnline(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataWentOnlineListeners, nameof(PlayerDataEventType.OnPlayerDataWentOnline));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataStartedOvershadowing(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataStartedOvershadowingListeners, nameof(PlayerDataEventType.OnPlayerDataStartedOvershadowing));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataStoppedOvershadowing(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataStoppedOvershadowingListeners, nameof(PlayerDataEventType.OnPlayerDataStoppedOvershadowing));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataStartedBeingOvershadowed(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataStartedBeingOvershadowedListeners, nameof(PlayerDataEventType.OnPlayerDataStartedBeingOvershadowed));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataStoppedBeingOvershadowed(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataStoppedBeingOvershadowedListeners, nameof(PlayerDataEventType.OnPlayerDataStoppedBeingOvershadowed));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        private void RaiseOnPlayerDataOvershadowingPlayerChanged(CorePlayerData corePlayerData)
        {
            playerDataForEvent = corePlayerData;
            // For some reason UdonSharp needs the 'JanSharp.' namespace name here to resolve the Raise function call.
            JanSharp.CustomRaisedEvents.Raise(ref onPlayerDataOvershadowingPlayerChangedListeners, nameof(PlayerDataEventType.OnPlayerDataOvershadowingPlayerChanged));
            playerDataForEvent = null; // To prevent misuse of the API.
        }

        #endregion
    }
}
