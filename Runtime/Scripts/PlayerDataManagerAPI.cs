namespace JanSharp
{
    [SingletonScript("28a5e083347ce2753aa92dfda01bef32")] // Runtime/Prefabs/PlayerDataManager.prefab
    public abstract class PlayerDataManagerAPI : LockstepGameState
    {
        public abstract void RegisterCustomPlayerDataDynamic(string playerDataClassName);
        /// <summary>
        /// <para>Usable once <see cref="LockstepAPI.IsInitialized"/> is <see langword="true"/>.</para>
        /// <para>Likely good to call inside of
        /// <see cref="PlayerDataEventType.OnPrePlayerDataManagerInit"/>.</para>
        /// </summary>
        /// <param name="playerDataClassName"></param>
        /// <returns></returns>
        public abstract int GetPlayerDataClassNameIndexDynamic(string playerDataClassName);
        /// <summary>
        /// <para>A direct reference to the internal array, which is an <see cref="ArrList"/>, which is to say
        /// that the <see cref="System.Array.Length"/> of this array cannot be trusted.</para>
        /// <para>It being an <see cref="ArrList"/> also implies that fetching this property and keeping a
        /// reference to the returned value can end up referring to a stale no longer used array in the
        /// future, if the arrays has been grown internally since fetching it.</para>
        /// <para>The actual amount of elements used of this array is defined via
        /// <see cref="AllCorePlayerDataCount"/>.</para>
        /// </summary>
        public abstract CorePlayerData[] AllCorePlayerDataRaw { get; }
        public abstract int AllCorePlayerDataCount { get; }
        /// <summary>
        /// <para>Creates a new array with the length of <see cref="AllCorePlayerDataCount"/> every time the
        /// property is accessed.</para>
        /// </summary>
        public abstract CorePlayerData[] AllCorePlayerData { get; }
        public abstract CorePlayerData GetCorePlayerDataAt(int index);
        public abstract CorePlayerData GetCorePlayerDataForPlayerId(uint playerId);
        public abstract CorePlayerData GetCorePlayerDataForPersistentId(uint persistentId);
        public abstract PlayerData GetPlayerDataForPlayerIdDynamic(string playerDataClassName, uint playerId);
        public abstract PlayerData GetPlayerDataForPersistentIdDynamic(string playerDataClassName, uint persistentId);
        public abstract PlayerData GetPlayerDataFromCoreDynamic(string playerDataClassName, CorePlayerData corePlayerData);
        public abstract PlayerData[] GetAllPlayerDataDynamic(string playerDataClassName);
        public abstract bool TryGetCorePlayerDataForPlayerId(uint playerId, out CorePlayerData corePlayerData);
        public abstract bool TryGetCorePlayerDataForPersistentId(uint persistentId, out CorePlayerData corePlayerData);
        /// <summary>
        /// <para>Sends an input action which ends up running
        /// <see cref="DeleteOfflinePlayerDataInGS(CorePlayerData)"/>.</para>
        /// </summary>
        /// <param name="corePlayerData">Must be non <see langword="null"/>. Sends the input action even if
        /// <see cref="CorePlayerData.isOffline"/> is <see langword="false"/>. By the time the IA runs it
        /// might have turned <see langword="true"/>.</param>
        public abstract void SendDeleteOfflinePlayerDataIA(CorePlayerData corePlayerData);
        /// <summary>
        /// <para>Raises events, be mindful of recursion.</para>
        /// </summary>
        /// <param name="corePlayerData">Must be non <see langword="null"/>. Does nothing if
        /// <see cref="CorePlayerData.isOffline"/> is <see langword="false"/>.</param>
        public abstract void DeleteOfflinePlayerDataInGS(CorePlayerData corePlayerData);
        /// <summary>
        /// <para>Sends an input action which ends up running
        /// <see cref="DeleteAllOfflinePlayerDataInGS"/>.</para>
        /// </summary>
        public abstract void SendDeleteAllOfflinePlayerDataIA();
        /// <summary>
        /// <para>Calls <see cref="DeleteOfflinePlayerDataInGS(CorePlayerData)"/> internally which raises events,
        /// be mindful of recursion.</para>
        /// </summary>
        public abstract void DeleteAllOfflinePlayerDataInGS();
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
        /// <inheritdoc cref="PlayerDataManagerAPI.GetPlayerDataClassNameIndexDynamic(string)"/>
        public static int GetPlayerDataClassNameIndex<T>(this PlayerDataManagerAPI manager, string playerDataClassName)
            where T : PlayerData
        {
            return manager.GetPlayerDataClassNameIndexDynamic(playerDataClassName);
        }

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
