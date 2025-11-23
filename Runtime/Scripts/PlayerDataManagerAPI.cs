namespace JanSharp
{
    [SingletonScript("28a5e083347ce2753aa92dfda01bef32")] // Runtime/Prefabs/PlayerDataManager.prefab
    public abstract class PlayerDataManagerAPI : LockstepGameState
    {
        public abstract void RegisterCustomPlayerDataDynamic(string playerDataClassName);
        public abstract CorePlayerData GetCorePlayerDataForPlayerId(uint playerId);
        public abstract CorePlayerData GetCorePlayerDataForPersistentId(uint persistentId);
        public abstract PlayerData GetPlayerDataForPlayerIdDynamic(string playerDataClassName, uint playerId);
        public abstract PlayerData GetPlayerDataForPersistentIdDynamic(string playerDataClassName, uint persistentId);
        public abstract PlayerData GetPlayerDataFromCoreDynamic(string playerDataClassName, CorePlayerData corePlayerData);
        public abstract PlayerData[] GetAllPlayerDataDynamic(string playerDataClassName);
        /// <summary>
        /// <para>Raises events, be mindful of recursion.</para>
        /// </summary>
        /// <param name="corePlayerData">Must be non <see langword="null"/> and
        /// <see cref="CorePlayerData.isOffline"/> must be <see langword="true"/>. If either is not the case,
        /// an error is logged that should be treated as an exception.</param>
        public abstract void DeleteOfflinePlayerData(CorePlayerData corePlayerData);
        /// <summary>
        /// <para>Calls <see cref="DeleteOfflinePlayerData(CorePlayerData)"/> internally which raises events,
        /// be mindful of recursion.</para>
        /// </summary>
        public abstract void DeleteAllOfflinePlayerData();
        /// <summary>
        /// <para>Usable during the import process.</para>
        /// <para>Specifically starting from when the player data game state has been imported (use the
        /// <see cref="LockstepGameStateDependencyAttribute"/> targeting the
        /// <see cref="PlayerDataManagerAPI"/> class to ensure game states depending on this API import after
        /// the player data game state.)</para>
        /// <para>And this gets cleared in the <see cref="LockstepEventType.OnImportFinished"/> event with an
        /// <c>Order</c> of <c>10000</c>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        /// <param name="importedPersistentId"></param>
        /// <returns></returns>
        public abstract uint GetPersistentIdFromImportedId(uint importedPersistentId);

        /// <summary>
        /// <para>Usable inside of the <see cref="PlayerDataEventType"/> events which are tied to player
        /// data.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract CorePlayerData PlayerDataForEvent { get; }
    }

    public static class PlayerDataManagerExtensions
    {
        public static void RegisterCustomPlayerData<T>(this PlayerDataManagerAPI manager, string playerDataClassName)
            where T : PlayerData
        {
            manager.RegisterCustomPlayerDataDynamic(playerDataClassName);
        }

        public static T GetPlayerDataForPlayerId<T>(this PlayerDataManagerAPI manager, string playerDataClassName, uint playerId)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataForPlayerIdDynamic(playerDataClassName, playerId);
        }

        public static T GetPlayerDataForPersistentId<T>(this PlayerDataManagerAPI manager, string playerDataClassName, uint persistentId)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataForPersistentIdDynamic(playerDataClassName, persistentId);
        }

        public static T GetPlayerDataFromCore<T>(this PlayerDataManagerAPI manager, string playerDataClassName, CorePlayerData corePlayerData)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataFromCoreDynamic(playerDataClassName, corePlayerData);
        }

        public static T[] GetAllPlayerData<T>(this PlayerDataManagerAPI manager, string playerDataClassName)
            where T : PlayerData
        {
            return (T[])manager.GetAllPlayerDataDynamic(playerDataClassName);
        }
    }
}
