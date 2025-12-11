
namespace JanSharp
{
    public enum PlayerDataEventType
    {
        /// <summary>
        /// <para>Raised inside of <see cref="LockstepEventType.OnInit"/> with an <c>Order</c> of
        /// <c>-10000</c>, right before the <see cref="PlayerDataManagerAPI"/> gets initialized.</para>
        /// <para>very first event raised by the player data system.</para>
        /// <para>Good event to call
        /// <see cref="PlayerDataManagerAPI.GetPlayerDataClassNameIndexDynamic(string)"/> in.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPrePlayerDataManagerInit,
        /// <summary>
        /// <para>Raised inside of <see cref="LockstepEventType.OnInit"/> with an <c>Order</c> of
        /// <c>-10000</c>, right after the <see cref="PlayerDataManagerAPI"/> has been initialized and the
        /// player data for the first player has been created.</para>
        /// <para></para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPostPlayerDataManagerInit,
        /// <summary>
        /// <para>Guaranteed to be raised exactly once for each <see cref="CorePlayerData"/> throughout the
        /// lifetime of the game state.</para>
        /// <para>Imports break this life cycle. This event does not get raised for imported player data, get
        /// all player player data post import inside of <see cref="OnPlayerDataImportFinished"/> or in
        /// <see cref="LockstepEventType.OnImportFinished"/> with <c>Order > -10000</c>.</para>
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
        /// <summary>
        /// <para>Gets raised inside of <see cref="LockstepEventType.OnImportFinished"/> with an <c>Order</c>
        /// of <c>-10000</c>, however only if the player data game state was part of the
        /// <see cref="LockstepAPI.GameStatesBeingImported"/>.</para>
        /// <para>Gets raised after the player data system itself has finished its whole import process,
        /// however <see cref="PlayerDataManagerAPI.GetPersistentIdFromImportedId(uint)"/> is still usable
        /// inside of this event.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        OnPlayerDataImportFinished,
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
}
