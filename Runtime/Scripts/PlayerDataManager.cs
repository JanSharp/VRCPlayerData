using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonScript("28a5e083347ce2753aa92dfda01bef32")] // Runtime/Prefabs/PlayerDataManager.prefab
    public class PlayerDataManager : LockstepGameState
    {
        public override string GameStateInternalName => "jansharp.player-data";
        public override string GameStateDisplayName => "Player Data";
        public override bool GameStateSupportsImportExport => true;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;
        public override LockstepGameStateOptionsUI ExportUI => null;
        public override LockstepGameStateOptionsUI ImportUI => null;

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
        private int importedCustomPlayerDataCount;
        private string importSuspendedInternalName;
        private string importSuspendedDisplayName;
        private uint importSuspendedDataVersion;
        private int importSuspendedScopeByteSize;
        private int importSuspendedClassNameIndex;

        public void RegisterCustomPlayerDataDynamic(string playerDataClassName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  RegisterCustomPlayerDataInternal");
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
            ArrList.Insert(ref playerDataInternalNames, ref playerDataInternalNamesCount, playerDataClassName, index);
        }

        public CorePlayerData GetCorePlayerDataForPlayerId(uint playerId)
        {
            return (CorePlayerData)playerDataByPlayerId[playerId].Reference;
        }

        public CorePlayerData GetCorePlayerDataForPersistentId(uint persistentId)
        {
            return (CorePlayerData)playerDataByPersistentId[persistentId].Reference;
        }

        public PlayerData GetPlayerDataForPlayerIdDynamic(string playerDataClassName, uint playerId)
        {
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPlayerId[playerId].Reference;
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            return corePlayerData.customPlayerData[classIndex];
        }

        public PlayerData GetPlayerDataForPersistentIdDynamic(string playerDataClassName, uint persistentId)
        {
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPersistentId[persistentId].Reference;
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            return corePlayerData.customPlayerData[classIndex];
        }

        public PlayerData GetPlayerDataFromCoreDynamic(string playerDataClassName, CorePlayerData corePlayerData)
        {
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            return corePlayerData.customPlayerData[classIndex];
        }

        public PlayerData[] GetAllPlayerDataDynamic(string playerDataClassName)
        {
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            PlayerData[] result = new PlayerData[allPlayerDataCount];
            for (int i = 0; i < allPlayerDataCount; i++)
                result[i] = allPlayerData[i].customPlayerData[classIndex];
            return result;
        }

        private PlayerData NewPlayerData(string className, CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  NewPlayerData");
#endif
            PlayerData playerData = (PlayerData)wannaBeClasses.NewDynamic(className);
            playerData.corePlayerData = corePlayerData;
            return playerData;
        }

        private CorePlayerData CreateNewCorePlayerData(uint playerId, string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  CreateNewCorePlayerData");
#endif
            CorePlayerData corePlayerData = wannaBeClasses.New<CorePlayerData>(nameof(CorePlayerData));
            corePlayerData.manager = this;
            corePlayerData.persistentId = nextPersistentId++;
            corePlayerData.playerId = playerId;
            corePlayerData.playerApi = VRCPlayerApi.GetPlayerById((int)playerId);
            corePlayerData.displayName = displayName;
            PlayerData[] customPlayerData = new PlayerData[playerDataClassNamesCount];
            corePlayerData.customPlayerData = customPlayerData;
            corePlayerData.index = allPlayerDataCount;
            ArrList.Add(ref allPlayerData, ref allPlayerDataCount, corePlayerData);
            playerDataByPersistentId.Add(corePlayerData.persistentId, corePlayerData);
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                PlayerData playerData = NewPlayerData(playerDataClassNames[i], corePlayerData);
                customPlayerData[i] = playerData;
                playerData.OnPlayerDataInit(isAboutToBeImported: false);
            }
            return corePlayerData;
        }

        private void InitializeNewPlayer(uint playerId, string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  InitializeNewPlayer");
#endif
            CorePlayerData corePlayerData = CreateNewCorePlayerData(playerId, displayName);
            playerDataByPlayerId.Add(playerId, corePlayerData);
            playerDataByName.Add(displayName, corePlayerData);
        }

        private void InitializeNewOvershadowedPlayer(uint playerId, CorePlayerData overshadowingPlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  InitializeNewOvershadowedPlayer");
#endif
            CorePlayerData corePlayerData = CreateNewCorePlayerData(playerId, overshadowingPlayerData.displayName);
            playerDataByPlayerId.Add(playerId, corePlayerData);
            corePlayerData.overshadowingPlayerData = overshadowingPlayerData;
        }

        private void InitializeRejoiningPlayer(uint playerId, CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  InitializeRejoiningPlayer");
#endif
            corePlayerData.isOffline = false;
            corePlayerData.playerId = playerId;
            playerDataByPlayerId.Add(playerId, corePlayerData);
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                PlayerData playerData = customPlayerData[i];
                if (playerData != null)
                    playerData.OnPlayerDataRejoin();
                else
                {
                    playerData = NewPlayerData(playerDataClassNames[i], corePlayerData);
                    customPlayerData[i] = playerData;
                    playerData.OnPlayerDataInit(isAboutToBeImported: false);
                }
            }
        }

        private void InitializePlayer(uint playerId)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  InitializePlayer");
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
            Debug.Log($"[PlayerData] Manager  OnInit");
#endif
            InitializePlayer(lockstep.MasterPlayerId);
        }

        [LockstepEvent(LockstepEventType.OnPreClientJoined, Order = -10000)]
        public void OnPreClientJoined()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  OnPreClientJoined");
#endif
            InitializePlayer(lockstep.JoinedPlayerId);
        }

        [LockstepEvent(LockstepEventType.OnClientLeft, Order = 10000)]
        public void OnClientLeft()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  OnClientLeft");
#endif
            uint playerId = lockstep.LeftPlayerId;
            playerDataByPlayerId.Remove(playerId, out DataToken corePlayerDataToken);
            CorePlayerData corePlayerData = (CorePlayerData)corePlayerDataToken.Reference;
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            bool shouldPersist = false;
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                PlayerData playerData = customPlayerData[i];
                if (playerData.PersistPlayerDataWhileOffline())
                {
                    shouldPersist = true;
                    playerData.OnPlayerDataLeft();
                    continue;
                }
                playerData.OnPlayerDataUninit();
                playerData.Delete();
                // The object has been deleted anyway, but this allows C#'s garbage collector to clean up the
                // empty reference object.
                customPlayerData[i] = null;
            }
            if (shouldPersist)
            {
                corePlayerData.isOffline = true;
                return;
            }

            CorePlayerData newlyOvershadowingPlayerData = null;
            for (int i = 0; i < allPlayerDataCount; i++)
            {
                CorePlayerData other = allPlayerData[i];
                if (other.overshadowingPlayerData == corePlayerData)
                {
                    if (newlyOvershadowingPlayerData != null)
                    {
                        other.overshadowingPlayerData = newlyOvershadowingPlayerData;
                        continue;
                    }
                    newlyOvershadowingPlayerData = other;
                    playerDataByName[corePlayerData.displayName] = other;
                    other.overshadowingPlayerData = null;
                    // TODO: raise event?
                }
            }

            if (!corePlayerData.IsOvershadowed && newlyOvershadowingPlayerData == null)
                playerDataByName.Remove(corePlayerData.displayName);
            DeleteCorePlayerData(corePlayerData);
        }

        private void DeleteCorePlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  DeleteCorePlayerData");
#endif
            playerDataByPersistentId.Remove(corePlayerData.persistentId);
            int index = corePlayerData.index;
            allPlayerData[index] = allPlayerData[--allPlayerDataCount];
            allPlayerData[index].index = index;
            corePlayerData.Delete();
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
            Debug.Log($"[PlayerData] Manager  SerializeExpectedPlayerDataClassNames");
#endif
            lockstep.WriteSmallUInt((uint)playerDataClassNamesCount);
            for (int i = 0; i < playerDataClassNamesCount; i++)
                lockstep.WriteString(playerDataClassNames[i]);
        }

        private bool ValidatePlayerDataClassNames(out string errorMessage)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  ValidatePlayerDataClassNames");
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
            Debug.Log($"[PlayerData] Manager  SerializeCustomPlayerData");
#endif
            playerData.Serialize(isExport: false);
        }

        private void DeserializeCustomPlayerData(CorePlayerData corePlayerData, int classNameIndex)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  DeserializeCustomPlayerData");
#endif
            PlayerData playerData = corePlayerData.customPlayerData[classNameIndex];
            if (playerData == null)
            {
                // Doesn't unconditionally create a new one as this might be a continuation from prev frame.
                playerData = NewPlayerData(playerDataClassNames[classNameIndex], corePlayerData);
                corePlayerData.customPlayerData[classNameIndex] = playerData;
            }
            playerData.Deserialize(isImport: false, importedDataVersion: 0u);
        }

        private void SerializeCorePlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  SerializeCorePlayerData");
#endif
            if (suspendedInCustomPlayerData)
                suspendedInCustomPlayerData = false;
            else
            {
                lockstep.WriteSmallUInt(corePlayerData.playerId);
                lockstep.WriteSmallUInt(corePlayerData.persistentId);

                lockstep.WriteFlags(corePlayerData.isOffline, corePlayerData.IsOvershadowed);
                if (corePlayerData.isOffline)
                    lockstep.WriteString(corePlayerData.displayName);
                if (corePlayerData.IsOvershadowed)
                    lockstep.WriteSmallUInt((uint)corePlayerData.overshadowingPlayerData.index);
            }

            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            int length = customPlayerData.Length;
            while (suspendedIndexInCustomPlayerDataArray < length)
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
            Debug.Log($"[PlayerData] Manager  DeserializeCorePlayerData");
#endif
            CorePlayerData corePlayerData = allPlayerData[index];
            if (suspendedInCustomPlayerData)
                suspendedInCustomPlayerData = false;
            else
            {
                corePlayerData.manager = this;
                corePlayerData.index = index;
                uint playerId = lockstep.ReadSmallUInt();
                playerDataByPlayerId.Add(playerId, corePlayerData);
                corePlayerData.playerId = playerId;
                corePlayerData.persistentId = lockstep.ReadSmallUInt();

                lockstep.ReadFlags(out corePlayerData.isOffline, out bool isOvershadowed);
                corePlayerData.displayName = corePlayerData.isOffline ? lockstep.ReadString() : lockstep.GetDisplayName(playerId);
                if (isOvershadowed)
                    corePlayerData.overshadowingPlayerData = allPlayerData[lockstep.ReadSmallUInt()];
                else
                    playerDataByName.Add(corePlayerData.displayName, corePlayerData);

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
            Debug.Log($"[PlayerData] Manager  SerializeAllCorePlayerData");
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
            Debug.Log($"[PlayerData] Manager  DeserializeAllCorePlayerData");
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
            Debug.Log($"[PlayerData] Manager  CountNonOvershadowedPlayerData");
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
            Debug.Log($"[PlayerData] Manager  CountPlayerDataSupportingExport");
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
            Debug.Log($"[PlayerData] Manager  ExportCustomPlayerDataMetadata");
#endif
            lockstep.WriteString(playerData.PlayerDataInternalName);
            lockstep.WriteString(playerData.PlayerDataDisplayName);
            lockstep.WriteSmallUInt(playerData.DataVersion);
        }

        private void ImportCustomPlayerDataMetadata()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  ImportCustomPlayerDataMetadata");
#endif
            importSuspendedInternalName = lockstep.ReadString();
            importSuspendedDisplayName = lockstep.ReadString();
            importSuspendedDataVersion = lockstep.ReadSmallUInt();
        }

        private void ExportCorePlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  ExportCorePlayerData");
#endif
            if (corePlayerData.IsOvershadowed)
                return;
            lockstep.WriteSmallUInt(corePlayerData.persistentId);
            lockstep.WriteString(corePlayerData.displayName);
        }

        private CorePlayerData ImportCorePlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  ImportCorePlayerData");
#endif
            uint importedPersistentId = lockstep.ReadSmallUInt();
            string displayName = lockstep.ReadString();
            CorePlayerData corePlayerData = GetOrCreateCorePlayerDataForImport(displayName);
            corePlayerData.importedPersistentId = importedPersistentId;
            persistentIdByImportedPersistentId.Add(importedPersistentId, corePlayerData.persistentId);
            return corePlayerData;
        }

        private CorePlayerData GetOrCreateCorePlayerDataForImport(string displayName)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  GetOrCreateCorePlayerDataForImport");
#endif
            if (playerDataByName.TryGetValue(displayName, out DataToken playerDataToken))
                return (CorePlayerData)playerDataToken.Reference;

            CorePlayerData corePlayerData = wannaBeClasses.New<CorePlayerData>(nameof(CorePlayerData));
            corePlayerData.manager = this;
            corePlayerData.persistentId = nextPersistentId++;
            corePlayerData.displayName = displayName;
            corePlayerData.isOffline = true;
            PlayerData[] customPlayerData = new PlayerData[playerDataClassNamesCount];
            corePlayerData.customPlayerData = customPlayerData;
            corePlayerData.index = allPlayerDataCount;
            ArrList.Add(ref allPlayerData, ref allPlayerDataCount, corePlayerData);
            playerDataByPersistentId.Add(corePlayerData.persistentId, corePlayerData);
            playerDataByName.Add(displayName, corePlayerData);
            return corePlayerData;
        }

        private void ExportAllCustomPlayerData(int classNameIndex)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  ExportAllCustomPlayerData");
#endif
            while (suspendedIndexInCorePlayerDataArray < allPlayerDataCount)
            {
                if (DeSerializationIsRunningLong())
                {
                    suspendedInCustomPlayerData = true;
                    return;
                }
                CorePlayerData corePlayerData = allPlayerData[suspendedIndexInCorePlayerDataArray];
                if (corePlayerData.IsOvershadowed)
                    continue;
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
            Debug.Log($"[PlayerData] Manager  TryImportAllCustomPlayerData");
#endif
            if (suspendedInCustomPlayerData)
                suspendedInCustomPlayerData = false;
            else
            {
                importSuspendedClassNameIndex = ArrList.IndexOf(ref playerDataInternalNames, ref playerDataInternalNamesCount, importSuspendedInternalName);
                if (importSuspendedClassNameIndex == -1)
                    return false;

                PlayerData dummyPlayerData = GetDummyCustomPlayerData()[importSuspendedClassNameIndex];
                if (!dummyPlayerData.SupportsImportExport || dummyPlayerData.LowestSupportedDataVersion > importSuspendedDataVersion)
                    return false;
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
                PlayerData playerData = corePlayerData.customPlayerData[importSuspendedClassNameIndex];
                if (playerData == null)
                {
                    playerData = NewPlayerData(playerDataClassNames[importSuspendedClassNameIndex], corePlayerData);
                    corePlayerData.customPlayerData[importSuspendedClassNameIndex] = playerData;
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

        private PlayerData[] GetDummyCustomPlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  GetDummyCustomPlayerData");
#endif
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPlayerId[lockstep.MasterPlayerId].Reference;
            return corePlayerData.customPlayerData;
        }

        private void Export()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  Export");
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
                        continue;
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
            Debug.Log($"[PlayerData] Manager  Import");
#endif
            if (importStage == 0)
            {
                allImportedPlayerData = new CorePlayerData[lockstep.ReadSmallUInt()];
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
                        lockstep.ReadBytes(importSuspendedScopeByteSize, skip: true);
                    suspendedIndexInCustomPlayerDataArray++;
                }
                suspendedIndexInCustomPlayerDataArray = 0;
                importStage = 0;
            }
        }

        [LockstepEvent(LockstepEventType.OnImportFinished)]
        public void OnImportFinished()
        {
            if (allImportedPlayerData == null) // The imported data did not contain the player data game state.
                return;
            CleanUpEmptyImportedCorePlayerData(allImportedPlayerData);
            allImportedPlayerData = null; // Free memory.
            persistentIdByImportedPersistentId.Clear();
        }

        private void CleanUpEmptyImportedCorePlayerData(CorePlayerData[] allImportedPlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  CleanUpEmptyImportedCorePlayerData");
#endif
            int count = allImportedPlayerData.Length;
            for (int i = count - 1; i >= 0; i--)
            {
                CorePlayerData corePlayerData = allImportedPlayerData[i];
                if (!corePlayerData.isOffline) // Non offline players always have all custom player data.
                    continue;
                PlayerData[] customPlayerData = corePlayerData.customPlayerData;
                bool doKeep = false;
                for (int j = 0; j < playerDataClassNamesCount; j++)
                {
                    PlayerData playerData = customPlayerData[j];
                    if (playerData == null)
                        continue;
                    if (playerData.PersistPlayerDataPostImportWhileOffline())
                        doKeep = true;
                    else
                    {
                        playerData.OnPlayerDataUninit();
                        playerData.Delete();
                        // The object has been deleted anyway, but this allows C#'s garbage collector
                        // to clean up the empty reference object.
                        customPlayerData[j] = null;
                    }
                }
                if (doKeep)
                    continue;
                playerDataByName.Remove(corePlayerData.displayName);
                DeleteCorePlayerData(corePlayerData);
            }
        }

        public uint GetPersistentIdFromImportedId(uint importedPersistentId)
        {
            return importedPersistentId == 0u
                ? 0u
                : persistentIdByImportedPersistentId[importedPersistentId].UInt;
        }

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  SerializeGameState");
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
            Debug.Log($"[PlayerData] Manager  DeserializeGameState");
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
            return null;
        }

        private int OpenUnknownSizeScope()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  OpenUnknownSizeScope");
#endif
            int sizePosition = lockstep.WriteStreamPosition;
            lockstep.WriteStreamPosition += 4;
            return sizePosition;
        }

        private void CloseUnknownSizeScope(int sizePosition)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  CloseUnknownSizeScope");
#endif
            int stopPosition = lockstep.WriteStreamPosition;
            lockstep.WriteStreamPosition = sizePosition;
            lockstep.WriteInt(stopPosition - sizePosition - 4);
            lockstep.WriteStreamPosition = stopPosition;
        }

        #endregion
    }

    public static class PlayerDataManagerExtensions
    {
        public static void RegisterCustomPlayerData<T>(this PlayerDataManager manager, string playerDataClassName)
            where T : PlayerData
        {
            manager.RegisterCustomPlayerDataDynamic(playerDataClassName);
        }

        public static T GetPlayerDataForPlayerId<T>(this PlayerDataManager manager, string playerDataClassName, uint playerId)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataForPlayerIdDynamic(playerDataClassName, playerId);
        }

        public static T GetPlayerDataForPersistentId<T>(this PlayerDataManager manager, string playerDataClassName, uint persistentId)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataForPersistentIdDynamic(playerDataClassName, persistentId);
        }

        public static T GetPlayerDataFromCore<T>(this PlayerDataManager manager, string playerDataClassName, CorePlayerData corePlayerData)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataFromCoreDynamic(playerDataClassName, corePlayerData);
        }

        public static T[] GetAllPlayerData<T>(this PlayerDataManager manager, string playerDataClassName)
            where T : PlayerData
        {
            return (T[])manager.GetAllPlayerDataDynamic(playerDataClassName);
        }
    }
}
