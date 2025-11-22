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
        /// <para>Called in <see cref="LockstepEventType.OnImportFinished"/>.</para>
        /// </summary>
        /// <returns></returns>
        public virtual bool PersistPlayerDataPostImportWhileOffline() => PersistPlayerDataWhileOffline();

        public virtual void OnPlayerDataInit(bool isAboutToBeImported) { }
        /// <summary>
        /// <para>Raised when this player leaves the world instance while this player is either
        /// <see cref="CorePlayerData.IsOvershadowed"/> or <see cref="CorePlayerData.IsOvershadowing"/>, as
        /// in both of these cases the <see cref="PersistPlayerDataWhileOffline"/> check gets bypassed and the
        /// player data gets deleted unconditionally.</para>
        /// <para>When not overridden this event gets passed through to <see cref="OnPlayerDataUninit"/> by
        /// default.</para>
        /// </summary>
        public virtual void OnPlayerDataForceUninit() => OnPlayerDataUninit();
        public virtual void OnPlayerDataUninit() { }
        public virtual void OnPlayerDataLeft() { }
        public virtual void OnPlayerDataRejoin() { }
    }
}
