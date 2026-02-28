namespace JanSharp
{
    public enum PlayerDataEventType
    {
        /// <summary>
        /// <para>Raised inside of unity's <c>Start</c> event, this is the event to call
        /// <see cref="PlayerDataManagerAPI.RegisterCustomPlayerDataDynamic(string)"/> or
        /// <see cref="PlayerDataManagerExtensions.RegisterCustomPlayerData{T}(PlayerDataManagerAPI, string)"/>
        /// in.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnRegisterCustomPlayerData,
        /// <summary>
        /// <para>Raised inside of unity's <c>Start</c> event, immediately after
        /// <see cref="OnRegisterCustomPlayerData"/>, therefore by the time this event runs all
        /// <see cref="PlayerDataManagerAPI.RegisterCustomPlayerDataDynamic(string)"/> and
        /// <see cref="PlayerDataManagerExtensions.RegisterCustomPlayerData{T}(PlayerDataManagerAPI, string)"/>
        /// calls have already happened.</para>
        /// <para>This is the event to call
        /// <see cref="PlayerDataManagerAPI.GetPlayerDataClassNameIndexDynamic(string)"/> or
        /// <see cref="PlayerDataManagerExtensions.GetPlayerDataClassNameIndex{T}(PlayerDataManagerAPI, string)"/>
        /// in.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnAllCustomPlayerDataRegistered,
        /// <summary>
        /// <para>Raised once <see cref="PlayerDataManagerAPI.LocalPlayerData"/> has been set. Which happens
        /// inside of <see cref="LockstepEventType.OnInit"/> with an <c>Order</c> of <c>-10000</c> and in game
        /// state deserialization for late joiners.</para>
        /// <para>This event should only be used to fetch the local player data or any of its custom player
        /// data into local variables. Anything outside of that is not supported and would likely just
        /// complicate cross system interactions.</para>
        /// <para><see cref="LockstepAPI.IsInitialized"/> is <see langword="false"/> at the time of this event
        /// being raised.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        OnLocalPlayerDataAvailable,
        /// <summary>
        /// <para>Raised inside of <see cref="LockstepEventType.OnInit"/> with an <c>Order</c> of
        /// <c>-10000</c>, right before the <see cref="PlayerDataManagerAPI"/> gets initialized.</para>
        /// <para>very first event raised by the player data system.</para>
        /// <para>Unlike <see cref="LockstepEventType.OnInit"/>,
        /// <see cref="LockstepAPI.FlagToContinueNextFrame"/> cannot be used inside of this event.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPrePlayerDataManagerInit,
        /// <summary>
        /// <para>Raised inside of <see cref="LockstepEventType.OnInit"/> with an <c>Order</c> of
        /// <c>-10000</c>, right after the <see cref="PlayerDataManagerAPI"/> has been initialized and the
        /// player data for the first player has been created.</para>
        /// <para>Unlike <see cref="LockstepEventType.OnInit"/>,
        /// <see cref="LockstepAPI.FlagToContinueNextFrame"/> cannot be used inside of this event.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPostPlayerDataManagerInit,
        /// <summary>
        /// <para>Guaranteed to be raised exactly once for each <see cref="CorePlayerData"/> throughout the
        /// lifetime of the game state.</para>
        /// <para>Imports break this life cycle. This event does not get raised for imported player data. Get
        /// all player player data post import inside of <see cref="LockstepEventType.OnImportFinishingUp"/>
        /// with <c>Order</c> greater than <c>-10000</c>, or in
        /// <see cref="LockstepEventType.OnImportFinished"/> (with any <c>Order</c>).</para>
        /// <para>Can be created already being in an overshadowed state. When that is the case
        /// <see cref="OnPlayerDataStartedBeingOvershadowed"/> gets raised immediately after this
        /// event.</para>
        /// <para>That is also the only way for <see cref="OnPlayerDataStartedBeingOvershadowed"/> to get
        /// raised.</para>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// been created.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataCreated,
        /// <summary>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// been deleted.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataDeleted,
        /// <summary>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// left the world instance, leaving their player data in an offline state.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataWentOffline,
        /// <summary>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// rejoined the world instance, where said player had existing player data which was in the offline
        /// state.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataWentOnline,
        /// <summary>
        /// <para>Raised after the <see cref="OnPlayerDataCreated"/> (and
        /// <see cref="OnPlayerDataStartedBeingOvershadowed"/>) events for the player which has joined which
        /// shared the same display name as an existing player.</para>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// started overshadowing that new player data.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataStartedOvershadowing,
        /// <summary>
        /// <para>Gets raised before the <see cref="OnPlayerDataDeleted"/> event of the player that got
        /// deleted, which was the last player overshadowed by this player data.</para>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// started overshadowing any other player data.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataStoppedOvershadowing,
        /// <summary>
        /// <para>The only time this event gets raised for a player is immediately after the
        /// <see cref="OnPlayerDataCreated"/> for the same player. There is no other way for a player to start
        /// being overshadowed by another.</para>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// started being overshadowed by another player data.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataStartedBeingOvershadowed,
        /// <summary>
        /// <para>Gets raised before the <see cref="OnPlayerDataDeleted"/> event for the player that left
        /// which was the overshadowing player.</para>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data which has
        /// started being overshadowed by another player data.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataStoppedBeingOvershadowed,
        /// <summary>
        /// <para>Gets raised after the <see cref="OnPlayerDataStartedOvershadowing"/> event for the player
        /// which is the newly overshadowing player.</para>
        /// <para>Use <see cref="PlayerDataManagerAPI.PlayerDataForEvent"/> to get the player data for which
        /// the player overshadowing it has changed.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataOvershadowingPlayerChanged,
    }

    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class PlayerDataEventAttribute : CustomRaisedEventBaseAttribute
    {
        /// <summary>
        /// <para>The method this attribute gets applied to must be public.</para>
        /// <para>The name of the function this attribute is applied to must have the exact same name as the
        /// name of the <paramref name="eventType"/>.</para>
        /// <para>Event registration is performed at OnBuild, which is to say that scripts with these kinds of
        /// event handlers must exist in the scene at build time, any runtime instantiated objects with these
        /// scripts on them will not receive these events.</para>
        /// <para>Disabled scripts still receive events.</para>
        /// </summary>
        /// <param name="eventType">The event to register this function as a listener to.</param>
        public PlayerDataEventAttribute(PlayerDataEventType eventType)
            : base((int)eventType)
        { }
    }

    [SingletonScript("28a5e083347ce2753aa92dfda01bef32")] // Runtime/Prefabs/PlayerDataManager.prefab
    public abstract class PlayerDataManagerAPI : LockstepGameState
    {
        /// <summary>
        /// <para>Gets set to true immediately after
        /// <see cref="PlayerDataEventType.OnPrePlayerDataManagerInit"/> got raised.</para>
        /// <para>This is therefore <see langword="true"/> inside of every other event raised by the player
        /// data system, including the <see cref="PlayerDataEventType.OnPlayerDataCreated"/> for the very
        /// first client.</para>
        /// <para>Usable any time.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract bool IsInitialized { get; }
        public abstract void RegisterCustomPlayerDataDynamic(string playerDataClassName);
        /// <summary>
        /// <para>Usable inside of <see cref="PlayerDataEventType.OnAllCustomPlayerDataRegistered"/> and
        /// onwards.</para>
        /// <para>Game state safe.</para>
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
        /// <para>Game state safe, including order.</para>
        /// </summary>
        public abstract CorePlayerData[] AllCorePlayerDataRaw { get; }
        public abstract int AllCorePlayerDataCount { get; }
        /// <summary>
        /// <para>Creates a new array with the length of <see cref="AllCorePlayerDataCount"/> every time the
        /// property is accessed.</para>
        /// <para>Game state safe, including order.</para>
        /// </summary>
        public abstract CorePlayerData[] AllCorePlayerData { get; }
        public abstract CorePlayerData GetCorePlayerDataAt(int index);
        public abstract CorePlayerData GetCorePlayerDataForPlayerId(uint playerId);
        public abstract CorePlayerData GetCorePlayerDataForPersistentId(uint persistentId);
        public abstract PlayerData GetPlayerDataForPlayerIdDynamic(string playerDataClassName, uint playerId);
        public abstract PlayerData GetPlayerDataForPersistentIdDynamic(string playerDataClassName, uint persistentId);
        public abstract PlayerData GetPlayerDataFromCoreDynamic(string playerDataClassName, CorePlayerData corePlayerData);
        public abstract PlayerData[] GetAllPlayerDataDynamic(string playerDataClassName);
        /// <summary>
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="corePlayerData">When returning <see langword="true"/> this is not
        /// <see langword="null"/> and <see cref="CorePlayerData.isDeleted"/> is guaranteed to be
        /// <see langword="false"/></param>
        /// <returns></returns>
        public abstract bool TryGetCorePlayerDataForPlayerId(uint playerId, out CorePlayerData corePlayerData);
        /// <summary>
        /// </summary>
        /// <param name="persistentId"></param>
        /// <param name="corePlayerData">When returning <see langword="true"/> this is not
        /// <see langword="null"/> and <see cref="CorePlayerData.isDeleted"/> is guaranteed to be
        /// <see langword="false"/></param>
        /// <returns></returns>
        public abstract bool TryGetCorePlayerDataForPersistentId(uint persistentId, out CorePlayerData corePlayerData);
        /// <summary>
        /// <para>Write a reference to a core player data instance to the lockstep write stream.</para>
        /// <para>Retrieve it using <see cref="ReadCorePlayerDataRef"/> on the receiving end.</para>
        /// </summary>
        /// <param name="corePlayerData">Can be <see langword="null"/>.</param>
        public abstract void WriteCorePlayerDataRef(CorePlayerData corePlayerData);
        /// <summary>
        /// <para>Read a reference to a core player data instance from the lockstep read string.</para>
        /// <para>Can return <see langword="null"/> even if it is guaranteed that the reference passed to
        /// <see cref="WriteCorePlayerDataRef(CorePlayerData)"/> was not <see langword="null"/> as the player
        /// data could have been deleted in the meantime.</para>
        /// <para>Can be used inside of player data deserialization, both imports and not, to resolve
        /// references to <see cref="CorePlayerData"/> which has yet to have its custom player data get
        /// deserialized. The <see cref="CorePlayerData"/> itself will be fully populated already.</para>
        /// </summary>
        /// <returns></returns>
        public abstract CorePlayerData ReadCorePlayerDataRef();
        /// <inheritdoc cref="ReadCorePlayerDataRef()"/>
        /// <param name="isImport">When <see langword="true"/>
        /// <see cref="GetPersistentIdFromImportedId(uint)"/> will be used to resolve the reference.</param>
        public abstract CorePlayerData ReadCorePlayerDataRef(bool isImport);
        /// <summary>
        /// <para>Sends an input action which ends up running
        /// <see cref="CreateOfflinePlayerDataInGS(string)"/>.</para>
        /// </summary>
        /// <param name="displayName">Does nothing if this is <see langword="null"/>.</param>
        public abstract void SendCreateOfflinePlayerDataIA(string displayName);
        /// <summary>
        /// <para>Raises events, be mindful of recursion.</para>
        /// <para>Usable once <see cref="IsInitialized"/> is <see langword="true"/>.</para>
        /// </summary>
        /// <param name="displayName">Does nothing if this is <see langword="null"/> or a player data with the
        /// same display name already exists. Offline player data cannot be overshadowed, in other words
        /// offline player data display names are and must always be unique.</param>
        /// <returns>The created player data, or <see langword="null"/> if <paramref name="displayName"/> was
        /// invalid.</returns>
        public abstract CorePlayerData CreateOfflinePlayerDataInGS(string displayName);
        /// <summary>
        /// <para>Sends an input action which ends up running
        /// <see cref="DeleteOfflinePlayerDataInGS(CorePlayerData)"/>.</para>
        /// </summary>
        /// <param name="corePlayerData">Must not be <see langword="null"/>. Sends the input action even if
        /// <see cref="CorePlayerData.isOffline"/> is <see langword="false"/>. By the time the IA runs it
        /// might have turned <see langword="true"/>.</param>
        public abstract void SendDeleteOfflinePlayerDataIA(CorePlayerData corePlayerData);
        /// <summary>
        /// <para>Raises events, be mindful of recursion.</para>
        /// <para>Usable once <see cref="IsInitialized"/> is <see langword="true"/>.</para>
        /// </summary>
        /// <param name="corePlayerData">Must not be <see langword="null"/>. Does nothing if
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
        /// <para>Usable once <see cref="IsInitialized"/> is <see langword="true"/>.</para>
        /// </summary>
        public abstract void DeleteAllOfflinePlayerDataInGS();
        /// <summary>
        /// <para>Usable during the import process.</para>
        /// <para>Specifically starting from when the player data game state has been imported (use the
        /// <see cref="LockstepGameStateDependencyAttribute"/> targeting the
        /// <see cref="PlayerDataManagerAPI"/> class to ensure game states depending on this API import after
        /// the player data game state.)</para>
        /// <para>And the underlying dictionary gets cleared in the
        /// <see cref="LockstepEventType.OnImportFinished"/> event with an <c>Order</c> of
        /// <c>10000</c>.</para>
        /// <para>Note how this is available throughout the entirety of
        /// <see cref="LockstepEventType.OnImportFinishingUp"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        /// <param name="importedPersistentId"></param>
        /// <returns></returns>
        public abstract uint GetPersistentIdFromImportedId(uint importedPersistentId);

        /// <summary>
        /// <para>Used in imports.</para>
        /// </summary>
        public abstract PlayerDataImportOptions ImportOptions { get; }

        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        public abstract CorePlayerData LocalPlayerData { get; }

        /// <summary>
        /// <para>A helper property using <see cref="LockstepAPI.SendingPlayerId"/> to
        /// <see cref="GetCorePlayerDataForPlayerId(uint)"/> inside of input actions.</para>
        /// <para>Usable inside of input actions, same as <see cref="LockstepAPI.SendingPlayerId"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public abstract CorePlayerData SendingPlayerData { get; }

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
