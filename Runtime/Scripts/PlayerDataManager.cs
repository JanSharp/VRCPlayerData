using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [SingletonScript]
    public class PlayerDataManager : LockstepGameState
    {
        public override string GameStateInternalName => "jansharp.player-data";
        public override string GameStateDisplayName => "Player Data";
        public override bool GameStateSupportsImportExport => false;
        public override uint GameStateDataVersion => 0u;
        public override uint GameStateLowestSupportedDataVersion => 0u;

        [HideInInspector] [SerializeField] [SingletonReference] private LockstepAPI lockstep;
        [HideInInspector] [SerializeField] [SingletonReference] private WannaBeClassesManager wannaBeClasses;

        private string[] playerDataClassNames = new string[ArrList.MinCapacity];
        private int playerDataClassNamesCount = 0;

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
            corePlayerData.displayName = displayName;
            PlayerData[] customPlayerData = new PlayerData[playerDataClassNamesCount];
            corePlayerData.customPlayerData = customPlayerData;
            corePlayerData.index = allPlayerDataCount;
            ArrList.Add(ref allPlayerData, ref allPlayerDataCount, corePlayerData);
            for (int i = 0; i < playerDataClassNamesCount; i++)
            {
                PlayerData playerData = NewPlayerData(playerDataClassNames[i], corePlayerData);
                customPlayerData[i] = playerData;
                playerData.Init();
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
                    playerData.OnRejoin();
                else
                {
                    playerData = NewPlayerData(playerDataClassNames[i], corePlayerData);
                    customPlayerData[i] = playerData;
                    playerData.Init();
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

        [LockstepEvent(LockstepEventType.OnInit, Order = -10000)]
        public void OnInit()
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  OnInit");
            #endif
            InitializePlayer(lockstep.MasterPlayerId);
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
                if (playerData.PersistForOfflinePlayer())
                {
                    shouldPersist = true;
                    playerData.OnLeft();
                    continue;
                }
                playerData.Uninit();
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
            playerData.Serialize();
        }

        private PlayerData DeserializeCustomPlayerData(string className, CorePlayerData corePlayerData)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  DeserializeCustomPlayerData");
            #endif
            PlayerData playerData = NewPlayerData(className, corePlayerData);
            playerData.Deserialize();
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
                corePlayerData.customPlayerData[i] = DeserializeCustomPlayerData(playerDataClassNames[i], corePlayerData);
            return corePlayerData;
        }

        private void SerializeAllPlayerData()
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  SerializeAllPlayerData");
            #endif
            lockstep.WriteSmallUInt((uint)allPlayerDataCount);
            for (int i = 0; i < allPlayerDataCount; i++)
                SerializeCorePlayerData(allPlayerData[i]);
        }

        private void DeserializeAllPlayerData()
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

        public override void SerializeGameState(bool isExport)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  SerializeGameState");
            #endif
            SerializeExpectedPlayerDataClassNames();
            SerializeAllPlayerData();
        }

        public override string DeserializeGameState(bool isImport, uint importedDataVersion)
        {
            #if PlayerDataDebug
            Debug.Log($"[PlayerData] Manager  DeserializeGameState");
            #endif
            if (!ValidatePlayerDataClassNames(out string errorMessage))
                return errorMessage;
            DeserializeAllPlayerData();
            return null;
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
