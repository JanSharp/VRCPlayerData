using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class CustomPlayerData : SerializableWannaBeClass
    {
        [System.NonSerialized] public CorePlayerData core;

        /// <summary>
        /// <para>Must be a completely unique name for this player data. Anything can be used however it is
        /// recommended to use <c>author-name.package-name</c>, everything lower case and using dashes instead
        /// of white spaces. <c>author-name</c> should be self explanatory. <c>package-name</c> could be a
        /// descriptive name for the system this player data is for.</para>
        /// <para>When creating a package (vpm package most likely) this is effectively simply the package
        /// name but without the <c>com.</c> prefix.</para>
        /// </summary>
        public abstract string PlayerDataInternalName { get; }
        /// <summary>
        /// <para>A user readable and identifiable name for this player data.</para>
        /// </summary>
        public abstract string PlayerDataDisplayName { get; }

        // public override bool WannaBeClassSupportsPooling => true; // Up to the deriving class.
        public override void ResetWannaBeClassToDefault()
        {
            base.ResetWannaBeClassToDefault();
            core = default;
        }

        /// <summary>
        /// <para>When <see cref="PersistPlayerDataInExport"/> is not overridden separately,
        /// <see cref="CorePlayerData.isOffline"/> can actually be <see langword="false"/> inside of this
        /// function. Semantically this still kind of makes sense as when importing only newly imported
        /// offline player data would potentially be deleted, regardless of the offline state at export
        /// time.</para>
        /// <para>Even if this returns <see langword="false"/> it may persist anyway. All custom
        /// <see cref="CustomPlayerData"/> must agree that some player's data does not need to persist for it
        /// to actually not persist.</para>
        /// <para>Only game state safe if <see cref="PersistPlayerDataInExport"/> gets overridden separately,
        /// as said function is not game state safe and calls <see cref="PersistPlayerDataWhileOffline"/> in
        /// its default implementation.</para>
        /// </summary>
        /// <returns></returns>
        public abstract bool PersistPlayerDataWhileOffline();
        /// <summary>
        /// <para>Called in <see cref="LockstepEventType.OnImportFinishingUp"/> with an <c>Order</c> of
        /// <c>-10000</c>.</para>
        /// <para>Only called on player data that has been newly created through the import that has just
        /// happened. Any player data which has already existed beforehand is not going to get deleted.</para>
        /// <para>Even if this returns <see langword="false"/> it may persist anyway. All custom
        /// <see cref="CustomPlayerData"/> must agree that some player's data does not need to persist for it
        /// to actually not persist.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        /// <returns></returns>
        public virtual bool PersistPlayerDataPostImportWhileOffline() => PersistPlayerDataWhileOffline();
        /// <summary>
        /// <para>Called in player data game state serialization for exports, only on
        /// <see cref="CustomPlayerData"/> where <see cref="SerializableWannaBeClass.SupportsImportExport"/>
        /// is <see langword="true"/>.</para>
        /// <para>Called regardless of <see cref="CorePlayerData.isOffline"/>.</para>
        /// <para>Even if this returns <see langword="false"/> it may persist anyway. All custom
        /// <see cref="CustomPlayerData"/> must agree that some player's data does not need to persist for it
        /// to actually not persist.</para>
        /// <para>Not game state safe.</para>
        /// </summary>
        /// <returns></returns>
        public virtual bool PersistPlayerDataInExport() => PersistPlayerDataWhileOffline();

        /// <summary>
        /// <para>Called inside of player data game state deserialization after all other player data has been
        /// imported.</para>
        /// <para>Called on all player data which existed before the import already but was not part of the
        /// imported data.</para>
        /// <para>For context said player data is all player data on any <see cref="CorePlayerData"/> which
        /// was not part of the imported data at all, as well as any custom <see cref="CustomPlayerData"/>
        /// where said custom data was not included or could not be imported due to data version in the
        /// imported data.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public virtual void OnNotPartOfImportedData() { }
        /// <summary>
        /// <para><see cref="CorePlayerData.isOffline"/> can be <see langword="true"/> already.</para>
        /// <para>The order in which <see cref="CustomPlayerData"/> for this player gets created and
        /// initialized is undefined, assume all of them to be <see langword="null"/>.</para>
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
        /// <param name="forced">
        /// <para><see langword="true"/> when the deletion is bypassing the
        /// <see cref="PersistPlayerDataWhileOffline"/> check, therefore happening unconditionally.</para>
        /// <para>This happens when this player left the world instance while this player is either
        /// <see cref="CorePlayerData.IsOvershadowed"/> or
        /// <see cref="CorePlayerData.IsOvershadowing"/>.</para>
        /// <para>Also <see langword="true"/> when this data is getting deleted through an API call to delete
        /// offline player data, which is to say that <see cref="CorePlayerData.isOffline"/> is
        /// <see langword="true"/>.</para>
        /// </param>
        public virtual void OnPlayerDataUninit(bool forced) { }
        /// <summary>
        /// <para>Raised inside of <see cref="LockstepEventType.OnClientLeft"/> with an <c>Order</c> of
        /// <c>10000</c>.</para>
        /// <para>This runs when a player left and their player data is being kept while they are
        /// offline.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public virtual void OnPlayerDataLeft() { }
        /// <summary>
        /// <para>Raised inside of <see cref="LockstepEventType.OnPreClientJoined"/> with an <c>Order</c> of
        /// <c>-10000</c>.</para>
        /// <para>This runs when a player joined and there was already offline player data for this joining
        /// player present.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        public virtual void OnPlayerDataRejoin() { }
    }
}
