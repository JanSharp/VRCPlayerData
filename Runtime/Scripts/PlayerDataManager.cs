﻿using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

// TODO: add persistentId

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonScript]
    public class PlayerDataManager : LockstepGameState
    {
        public override string GameStateInternalName => "jansharp.player-data";
        public override string GameStateDisplayName => "Player Data";
        public override bool GameStateSupportsImportExport => true;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;

        [HideInInspector] [SerializeField] [SingletonReference] private LockstepAPI lockstep;
        [HideInInspector] [SerializeField] [SingletonReference] private WannaBeClassesManager wannaBeClasses;

        private string[] playerDataClassNames = new string[ArrList.MinCapacity];
        private int playerDataClassNamesCount = 0;
        private DataDictionary classNameIndexesByInternalName = new DataDictionary();

        /// <summary>
        /// <para>All players.</para>
        /// </summary>
        private CorePlayerData[] allPlayerData = new CorePlayerData[ArrList.MinCapacity];
        private int allPlayerDataCount = 0;
        /// <summary>
        /// <para>All non overshadowed players.</para>
        /// </summary>
        private DataDictionary playerDataByName = new DataDictionary();
        /// <summary>
        /// <para>All online players.</para>
        /// </summary>
        private DataDictionary playerDataByPlayerId = new DataDictionary();

        public void RegisterCustomPlayerDataDynamic(string playerDataClassName)
        {
            #if PlayerDataDebug
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

        public PlayerData GetPlayerDataDynamic(string playerDataClassName, uint playerId)
        {
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPlayerId[playerId].Reference;
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  NewPlayerData");
            #endif
            PlayerData playerData = (PlayerData)wannaBeClasses.NewDynamic(className);
            playerData.corePlayerData = corePlayerData;
            playerData.lockstep = lockstep;
            return playerData;
        }

        private CorePlayerData CreateNewCorePlayerData(uint playerId, string displayName)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  CreateNewCorePlayerData");
            #endif
            CorePlayerData corePlayerData = wannaBeClasses.New<CorePlayerData>(nameof(CorePlayerData));
            corePlayerData.playerId = playerId;
            corePlayerData.playerApi = VRCPlayerApi.GetPlayerById((int)playerId);
            corePlayerData.displayName = displayName;
            PlayerData[] customPlayerData = new PlayerData[playerDataClassNamesCount];
            corePlayerData.customPlayerData = customPlayerData;
            corePlayerData.index = allPlayerDataCount;
            ArrList.Add(ref allPlayerData, ref allPlayerDataCount, corePlayerData);
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  InitializeNewPlayer");
            #endif
            CorePlayerData corePlayerData = CreateNewCorePlayerData(playerId, displayName);
            playerDataByPlayerId.Add(playerId, corePlayerData);
            playerDataByName.Add(displayName, corePlayerData);
        }

        private void InitializeNewOvershadowedPlayer(uint playerId, CorePlayerData overshadowingPlayerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  InitializeNewOvershadowedPlayer");
            #endif
            CorePlayerData corePlayerData = CreateNewCorePlayerData(playerId, overshadowingPlayerData.displayName);
            playerDataByPlayerId.Add(playerId, corePlayerData);
            corePlayerData.overshadowingPlayerData = overshadowingPlayerData;
        }

        private void InitializeRejoiningPlayer(uint playerId, CorePlayerData corePlayerData)
        {
            #if PlayerDataDebug
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
            #if PlayerDataDebug
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
            #if PlayerDataDebug
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  OnInit");
            #endif
            InitializePlayer(lockstep.MasterPlayerId);
            InitInternalNameLut();
        }

        [LockstepEvent(LockstepEventType.OnClientBeginCatchUp, Order = -10000)]
        public void OnClientBeginCatchUp()
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  OnClientBeginCatchUp");
            #endif
            InitInternalNameLut();
        }

        [LockstepEvent(LockstepEventType.OnPreClientJoined, Order = -10000)]
        public void OnPreClientJoined()
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  OnPreClientJoined");
            #endif
            InitializePlayer(lockstep.JoinedPlayerId);
        }

        [LockstepEvent(LockstepEventType.OnClientLeft, Order = 10000)]
        public void OnClientLeft()
        {
            #if PlayerDataDebug
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  DeleteCorePlayerData");
            #endif
            int index = corePlayerData.index;
            allPlayerData[index] = allPlayerData[--allPlayerDataCount];
            allPlayerData[index].index = index;
            corePlayerData.Delete();
        }

        #region Serialization

        private void SerializeExpectedPlayerDataClassNames()
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  SerializeExpectedPlayerDataClassNames");
            #endif
            lockstep.WriteSmallUInt((uint)playerDataClassNamesCount);
            for (int i = 0; i < playerDataClassNamesCount; i++)
                lockstep.WriteString(playerDataClassNames[i]);
        }

        private bool ValidatePlayerDataClassNames(out string errorMessage)
        {
            #if PlayerDataDebug
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  SerializeCustomPlayerData");
            #endif
            playerData.SerializePlayerData(isExport: false);
        }

        private PlayerData DeserializeCustomPlayerData(int classNameIndex, CorePlayerData corePlayerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  DeserializeCustomPlayerData");
            #endif
            PlayerData playerData = NewPlayerData(playerDataClassNames[classNameIndex], corePlayerData);
            corePlayerData.customPlayerData[classNameIndex] = playerData;
            playerData.DeserializePlayerData(isImport: false, importedDataVersion: 0u);
            return playerData;
        }

        private void SerializeCorePlayerData(CorePlayerData corePlayerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  SerializeCorePlayerData");
            #endif
            lockstep.WriteSmallUInt(corePlayerData.playerId);

            int flags = (corePlayerData.isOffline ? 1 : 0)
                | (corePlayerData.IsOvershadowed ? 2 : 0);
            lockstep.WriteByte((byte)flags);
            if (corePlayerData.isOffline)
                lockstep.WriteString(corePlayerData.displayName);
            if (corePlayerData.IsOvershadowed)
                lockstep.WriteSmallUInt((uint)corePlayerData.overshadowingPlayerData.index);

            foreach (PlayerData playerData in corePlayerData.customPlayerData)
                SerializeCustomPlayerData(playerData);
        }

        private CorePlayerData DeserializeCorePlayerData(int index)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  DeserializeCorePlayerData");
            #endif
            CorePlayerData corePlayerData = allPlayerData[index];
            corePlayerData.index = index;
            uint playerId = lockstep.ReadSmallUInt();
            playerDataByPlayerId.Add(playerId, corePlayerData);
            corePlayerData.playerId = playerId;

            int flags = lockstep.ReadByte();
            corePlayerData.isOffline = (flags & 1) != 0;
            corePlayerData.displayName = corePlayerData.isOffline ? lockstep.ReadString() : lockstep.GetDisplayName(playerId);
            if ((flags & 2) != 0)
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  SerializeAllPlayerData");
            #endif
            lockstep.WriteSmallUInt((uint)allPlayerDataCount);
            for (int i = 0; i < allPlayerDataCount; i++)
                SerializeCorePlayerData(allPlayerData[i]);
        }

        private void DeserializeAllCorePlayerData()
        {
            #if PlayerDataDebug
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
            #if PlayerDataDebug
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  CountPlayerDataSupportingExport");
            #endif
            uint toExportCount = 0;
            for (int i = 0; i < playerDataClassNamesCount; i++)
                if (customPlayerData[i].PlayerDataSupportsImportExport)
                    toExportCount++;
            return toExportCount;
        }

        private void ExportCustomPlayerDataMetadata(PlayerData playerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  ExportCustomPlayerDataMetadata");
            #endif
            lockstep.WriteString(playerData.PlayerDataInternalName);
            lockstep.WriteString(playerData.PlayerDataDisplayName);
            lockstep.WriteSmallUInt(playerData.PlayerDataVersion);
        }

        private void ImportCustomPlayerDataMetadata(out string internalName, out string displayName, out uint dataVersion)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  ImportCustomPlayerDataMetadata");
            #endif
            internalName = lockstep.ReadString();
            displayName = lockstep.ReadString();
            dataVersion = lockstep.ReadSmallUInt();
        }

        private void ExportCorePlayerData(CorePlayerData corePlayerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  ExportCorePlayerData");
            #endif
            if (corePlayerData.IsOvershadowed)
                return;
            lockstep.WriteString(corePlayerData.displayName);
        }

        private CorePlayerData ImportCorePlayerData()
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  ImportCorePlayerData");
            #endif
            string displayName = lockstep.ReadString();
            if (playerDataByName.TryGetValue(displayName, out DataToken playerDataToken))
                return (CorePlayerData)playerDataToken.Reference;
            CorePlayerData corePlayerData = wannaBeClasses.New<CorePlayerData>(nameof(CorePlayerData));
            corePlayerData.displayName = displayName;
            corePlayerData.isOffline = true;
            PlayerData[] customPlayerData = new PlayerData[playerDataClassNamesCount];
            corePlayerData.customPlayerData = customPlayerData;
            corePlayerData.index = allPlayerDataCount;
            ArrList.Add(ref allPlayerData, ref allPlayerDataCount, corePlayerData);
            playerDataByName.Add(displayName, corePlayerData);
            return corePlayerData;
        }

        private void ExportAllCustomPlayerData(int classNameIndex)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  ExportAllCustomPlayerData");
            #endif
            for (int i = 0; i < allPlayerDataCount; i++)
            {
                CorePlayerData corePlayerData = allPlayerData[i];
                if (corePlayerData.IsOvershadowed)
                    continue;
                PlayerData playerData = corePlayerData.customPlayerData[classNameIndex];
                playerData.SerializePlayerData(isExport: true);
            }
        }

        private bool TryImportAllCustomPlayerData(string internalName, uint dataVersion, CorePlayerData[] allImportedPlayerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  TryImportAllCustomPlayerData");
            #endif
            if (!classNameIndexesByInternalName.TryGetValue(internalName, out DataToken classNameIndexToken))
                return false;

            int classNameIndex = classNameIndexToken.Int;
            string className = playerDataClassNames[classNameIndex];

            PlayerData dummyPlayerData = GetDummyCustomPlayerData()[classNameIndex];
            if (!dummyPlayerData.PlayerDataSupportsImportExport || dummyPlayerData.PlayerDataLowestSupportedVersion > dataVersion)
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
                playerData.DeserializePlayerData(isImport: true, dataVersion);
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
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  GetDummyCustomPlayerData");
            #endif
            CorePlayerData corePlayerData = (CorePlayerData)playerDataByPlayerId[lockstep.MasterPlayerId].Reference;
            return corePlayerData.customPlayerData;
        }

        private void Export()
        {
            #if PlayerDataDebug
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
                if (!playerData.PlayerDataSupportsImportExport)
                    continue;

                ExportCustomPlayerDataMetadata(playerData);

                int sizePosition = OpenUnknownSizeScope();
                ExportAllCustomPlayerData(i);
                CloseUnknownSizeScope(sizePosition);
            }
        }

        private void Import(uint importedDataVersion)
        {
            #if PlayerDataDebug
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
        }

        private void CleanUpEmptyImportedCorePlayerData(CorePlayerData[] allImportedPlayerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  CleanUpEmptyImportedCorePlayerData");
            #endif
            int count = allImportedPlayerData.Length;
            for (int i = count - 1; i >= 0 ; i--)
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

        public override void SerializeGameState(bool isExport)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  SerializeGameState");
            #endif
            if (isExport)
                Export();
            else
            {
                SerializeExpectedPlayerDataClassNames();
                SerializeAllCorePlayerData();
            }
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  DeserializeGameState");
            #endif
            if (isImport)
                Import(importedDataVersion);
            else
            {
                if (!ValidatePlayerDataClassNames(out string errorMessage))
                    return errorMessage;
                DeserializeAllCorePlayerData();
            }
            return null;
        }

        private int OpenUnknownSizeScope()
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  OpenUnknownSizeScope");
            #endif
            int sizePosition = lockstep.WriteStreamPosition;
            lockstep.WriteStreamPosition += 4;
            return sizePosition;
        }

        private void CloseUnknownSizeScope(int sizePosition)
        {
            #if PlayerDataDebug
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

        public static T GetPlayerData<T>(this PlayerDataManager manager, string playerDataClassName, uint playerId)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataDynamic(playerDataClassName, playerId);
        }

        public static T[] GetAllPlayerData<T>(this PlayerDataManager manager, string playerDataClassName)
            where T : PlayerData
        {
            return (T[])manager.GetAllPlayerDataDynamic(playerDataClassName);
        }
    }
}
