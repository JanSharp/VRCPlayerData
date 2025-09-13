using UdonSharp;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class PlayerData : SerializableWannaBeClass
    {
        [System.NonSerialized] public CorePlayerData corePlayerData;

        public uint PersistentId => corePlayerData.persistentId;
        public uint ImportedPersistentId => corePlayerData.importedPersistentId;
        public uint PlayerId => corePlayerData.playerId;
        public VRCPlayerApi PlayerApi
        {
            get
            {
                VRCPlayerApi player = corePlayerData.playerApi;
                if (player != null && !player.IsValid())
                {
                    corePlayerData.playerApi = null;
                    return null;
                }
                return player;
            }
        }
        public string DisplayName => corePlayerData.displayName;
        public bool IsOffline => corePlayerData.isOffline;
        public CorePlayerData OvershadowingPlayerData => corePlayerData.overshadowingPlayerData;
        public bool IsOvershadowed => corePlayerData.IsOvershadowed;

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
