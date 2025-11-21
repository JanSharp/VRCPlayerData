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
        public virtual void OnPlayerDataUninit() { }
        public virtual void OnPlayerDataLeft() { }
        public virtual void OnPlayerDataRejoin() { }
    }
}
