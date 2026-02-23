using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class PlayerData : SerializableWannaBeClass
    {
        [System.NonSerialized] public CorePlayerData core;

        public abstract string PlayerDataInternalName { get; }
        public abstract string PlayerDataDisplayName { get; }

        public abstract bool PersistPlayerDataWhileOffline();
        /// <summary>
        /// <para>Called in <see cref="LockstepEventType.OnImportFinishingUp"/>.</para>
        /// <para>Only called on player data that has been newly created through the import that has just
        /// happened. Any player data which has already existed beforehand is not going to get deleted.</para>
        /// </summary>
        /// <returns></returns>
        public virtual bool PersistPlayerDataPostImportWhileOffline() => PersistPlayerDataWhileOffline();

        /// <summary>
        /// <para>Called inside of player data game state deserialization after all other player data has been
        /// imported on all player data which was not part of the imported data.</para>s
        /// </summary>
        public virtual void OnNotPartOfImportedData() { }
        /// <summary>
        /// <para><see cref="CorePlayerData.isOffline"/> can be <see langword="true"/> already.</para>
        /// <para>Which other <see cref="PlayerData"/> for this player has already been created and
        /// initialized is undefined, assume all of it to be <see langword="null"/>.</para>
        /// <para>For systems requiring cross player data interaction, use the
        /// <see cref="PlayerDataEventType.OnPlayerDataCreated"/>,
        /// <see cref="LockstepEventType.OnImportFinishingUp"/> or
        /// <see cref="LockstepEventType.OnImportFinished"/> events to resolve cross references and
        /// initialization.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        /// <param name="isAboutToBeImported"></param>
        public virtual void OnPlayerDataInit(bool isAboutToBeImported) { }
        /// <summary>
        /// <para>Raised when this player data gets deleted due to the player leaving the world instance, or
        /// due to an API call to delete offline player data.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        /// <param name="force">
        /// <para><see langword="true"/> when the deletion is bypassing the
        /// <see cref="PersistPlayerDataWhileOffline"/> check, therefore happening unconditionally.</para>
        /// <para>This happens when this player left the world instance while this player is either
        /// <see cref="CorePlayerData.IsOvershadowed"/> or <see cref="CorePlayerData.IsOvershadowing"/>, </para>
        /// <para>Also <see langword="true"/> when this data is getting deleted through an API call to delete
        /// offline player data, which is to say that this player data
        /// <see cref="CorePlayerData.isOffline"/>.</para>
        /// </param>
        public virtual void OnPlayerDataUninit(bool force) { }
        public virtual void OnPlayerDataLeft() { }
        public virtual void OnPlayerDataRejoin() { }
    }
}
