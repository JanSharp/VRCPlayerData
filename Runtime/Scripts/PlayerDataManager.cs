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

        private string[] playerDataClassNames = new string[ArrList.MinCapacity];
        private int playerDataClassNamesCount = 0;
        private DataDictionary classNameIndexesByInternalName = new DataDictionary();

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
            ArrList.Insert(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName, index);
        }

        public CorePlayerData GetCorePlayerDataByPlayerId(uint playerId)
        {
            return (CorePlayerData)playerDataByPlayerId[playerId].Reference;
        }

        public CorePlayerData GetCorePlayerDataByPersistentId(uint persistentId)
        {
            return (CorePlayerData)playerDataByPersistentId[persistentId].Reference;
        }

        public PlayerData GetPlayerDataForPlayerIdDynamic(string playerDataClassName, uint playerId)
        {
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPlayerId[playerId].Reference;
            int classIndex = ArrList.BinarySearch(ref playerDataClassNames, ref playerDataClassNamesCount, playerDataClassName);
            return corePlayerData.customPlayerData[classIndex];
        }

        public PlayerData GetPlayerDataForPersistentIdDynamic(string playerDataClassName, uint playerId)
        {
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPersistentId[playerId].Reference;
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

        private void InitInternalNameLut()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  InitInternalNameLut");
#endif
            CorePlayerData corePlayerData = allPlayerData[0];
            PlayerData[] customPlayerData = corePlayerData.customPlayerData;
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                string internalName = customPlayerData[i].PlayerDataInternalName;
                if (classNameIndexesByInternalName.TryGetValue(internalName, out DataToken classNameIndexToken))
                {
                    Debug.LogError($"[PlayerData] '{playerDataClassNames[classNameIndexToken.Int]}' and '{playerDataClassNames[i]}' are both "
                        + $"trying to use '{internalName}' as their PlayerDataInternalName. They must use different internal names.");
                    continue;
                }
                classNameIndexesByInternalName.Add(internalName, i);
            }
        }

        [LockstepEvent(LockstepEventType.OnInit, Order = -10000)]
        public void OnInit()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  OnInit");
#endif
            InitializePlayer(lockstep.MasterPlayerId);
            InitInternalNameLut();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp, Order = -10000)]
        public void OnClientBeginCatchUp()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  OnClientBeginCatchUp");
#endif
            InitInternalNameLut();
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

        private PlayerData DeserializeCustomPlayerData(int classNameIndex, CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  DeserializeCustomPlayerData");
#endif
            PlayerData playerData = NewPlayerData(playerDataClassNames[classNameIndex], corePlayerData);
            corePlayerData.customPlayerData[classNameIndex] = playerData;
            playerData.Deserialize(isImport: false, importedDataVersion: 0u);
            return playerData;
        }

        private void SerializeCorePlayerData(CorePlayerData corePlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  SerializeCorePlayerData");
#endif
            lockstep.WriteSmallUInt(corePlayerData.playerId);
            lockstep.WriteSmallUInt(corePlayerData.persistentId);

            lockstep.WriteFlags(corePlayerData.isOffline, corePlayerData.IsOvershadowed);
            if (corePlayerData.isOffline)
                lockstep.WriteString(corePlayerData.displayName);
            if (corePlayerData.IsOvershadowed)
                lockstep.WriteSmallUInt((uint)corePlayerData.overshadowingPlayerData.index);

            foreach (PlayerData playerData in corePlayerData.customPlayerData)
                SerializeCustomPlayerData(playerData);
        }

        private CorePlayerData DeserializeCorePlayerData(int index)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  DeserializeCorePlayerData");
#endif
            CorePlayerData corePlayerData = allPlayerData[index];
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
            for (int i = 0; i < playerDataClassNamesCount; i++)
                DeserializeCustomPlayerData(i, corePlayerData);
            return corePlayerData;
        }

        private void SerializeAllCorePlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  SerializeAllPlayerData");
#endif
            lockstep.WriteSmallUInt((uint)allPlayerDataCount);
            for (int i = 0; i < allPlayerDataCount; i++)
                SerializeCorePlayerData(allPlayerData[i]);
        }

        private void DeserializeAllCorePlayerData()
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  DeserializeAllPlayerData");
#endif
            allPlayerDataCount = (int)lockstep.ReadSmallUInt();
            ArrList.EnsureCapacity(ref allPlayerData, allPlayerDataCount);
            // Populate with empty instances so overshadowingPlayerData can be assigned immediately in the
            // deserialization pass.
            for (int i = 0; i < allPlayerDataCount; i++)
                allPlayerData[i] = wannaBeClasses.New<CorePlayerData>(nameof(CorePlayerData));
            for (int i = 0; i < allPlayerDataCount; i++)
                allPlayerData[i] = DeserializeCorePlayerData(i);
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

        private void ImportCustomPlayerDataMetadata(out string internalName, out string displayName, out uint dataVersion)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  ImportCustomPlayerDataMetadata");
#endif
            internalName = lockstep.ReadString();
            displayName = lockstep.ReadString();
            dataVersion = lockstep.ReadSmallUInt();
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
            for (int i = 0; i < allPlayerDataCount; i++)
            {
                CorePlayerData corePlayerData = allPlayerData[i];
                if (corePlayerData.IsOvershadowed)
                    continue;
                PlayerData playerData = corePlayerData.customPlayerData[classNameIndex];
                playerData.Serialize(isExport: true);
            }
        }

        private bool TryImportAllCustomPlayerData(string internalName, uint dataVersion, CorePlayerData[] allImportedPlayerData)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  TryImportAllCustomPlayerData");
#endif
            if (!classNameIndexesByInternalName.TryGetValue(internalName, out DataToken classNameIndexToken))
                return false;

            int classNameIndex = classNameIndexToken.Int;
            string className = playerDataClassNames[classNameIndex];

            PlayerData dummyPlayerData = GetDummyCustomPlayerData()[classNameIndex];
            if (!dummyPlayerData.SupportsImportExport || dummyPlayerData.LowestSupportedDataVersion > dataVersion)
                return false;

            int count = allImportedPlayerData.Length;
            for (int i = 0; i < count; i++)
            {
                CorePlayerData corePlayerData = allImportedPlayerData[i];
                PlayerData playerData = corePlayerData.customPlayerData[classNameIndex];
                if (playerData == null)
                {
                    playerData = NewPlayerData(className, corePlayerData);
                    corePlayerData.customPlayerData[classNameIndex] = playerData;
                    playerData.OnPlayerDataInit(isAboutToBeImported: true);
                }
                playerData.Deserialize(isImport: true, dataVersion);
                if (corePlayerData.isOffline && !playerData.PersistPlayerDataPostImportWhileOffline())
                {
                    playerData.Delete();
                    // The object has been deleted anyway, but this allows C#'s garbage collector to clean up the
                    // empty reference object.
                    corePlayerData.customPlayerData[classNameIndex] = null;
                }
            }

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
            lockstep.WriteSmallUInt(CountNonOvershadowedPlayerData());
            for (int i = 0; i < allPlayerDataCount; i++)
                ExportCorePlayerData(allPlayerData[i]);

            PlayerData[] customPlayerData = GetDummyCustomPlayerData();

            lockstep.WriteSmallUInt(CountPlayerDataSupportingExport(customPlayerData));
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                PlayerData playerData = customPlayerData[i];
                if (!playerData.SupportsImportExport)
                    continue;

                ExportCustomPlayerDataMetadata(playerData);

                int sizePosition = OpenUnknownSizeScope();
                ExportAllCustomPlayerData(i);
                CloseUnknownSizeScope(sizePosition);
            }
        }

        private void Import(uint importedDataVersion)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  Import");
#endif
            uint importedPlayerDataCount = lockstep.ReadSmallUInt();
            CorePlayerData[] allImportedPlayerData = new CorePlayerData[importedPlayerDataCount];
            for (int i = 0; i < importedPlayerDataCount; i++)
                allImportedPlayerData[i] = ImportCorePlayerData();

            uint playerDataCount = lockstep.ReadSmallUInt();
            for (int i = 0; i < playerDataCount; i++)
            {
                ImportCustomPlayerDataMetadata(out string internalName, out string displayName, out uint dataVersion);

                int customDataSize = lockstep.ReadInt();
                if (!TryImportAllCustomPlayerData(internalName, dataVersion, allImportedPlayerData))
                    lockstep.ReadBytes(customDataSize, skip: true);
            }

            CleanUpEmptyImportedCorePlayerData(allImportedPlayerData);
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
                foreach (PlayerData playerData in corePlayerData.customPlayerData)
                    if (playerData != null)
                        continue;
                playerDataByName.Remove(corePlayerData.displayName);
                DeleteCorePlayerData(corePlayerData);
            }
        }

        public uint GetPersistentIdFromImportedId(uint importedPersistentId)
        {
            return persistentIdByImportedPersistentId[importedPersistentId].UInt;
        }

        public override void SerializeGameState(bool isExport, LockstepGameStateOptionsData exportOptions)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  SerializeGameState");
#endif
            if (isExport)
                Export();
            else
            {
                SerializeExpectedPlayerDataClassNames();
                lockstep.WriteSmallUInt(nextPersistentId);
                SerializeAllCorePlayerData();
            }
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion, LockstepGameStateOptionsData importOptions)
        {
#if PLAYER_DATA_DEBUG
            Debug.Log($"[PlayerData] Manager  DeserializeGameState");
#endif
            if (isImport)
                Import(importedDataVersion);
            else
            {
                if (!ValidatePlayerDataClassNames(out string errorMessage))
                    return errorMessage;
                nextPersistentId = lockstep.ReadSmallUInt();
                DeserializeAllCorePlayerData();
            }
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
