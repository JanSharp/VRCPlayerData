using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class PlayerData : WannaBeClass
    {
        [System.NonSerialized] public CorePlayerData corePlayerData;
        [System.NonSerialized] public LockstepAPI lockstep;

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
        public abstract bool PlayerDataSupportsImportExport { get; }
        public abstract uint PlayerDataVersion { get; }
        public abstract uint PlayerDataLowestSupportedVersion { get; }

        public abstract bool PersistPlayerDataWhileOffline();
        public virtual bool PersistPlayerDataPostImportWhileOffline() => PersistPlayerDataWhileOffline();
        public abstract void SerializePlayerData(bool isExport);
        public abstract void DeserializePlayerData(bool isImport, uint importedDataVersion);

        public virtual void OnPlayerDataInit(bool isAboutToBeImported) { }
        public virtual void OnPlayerDataUninit() { }
        public virtual void OnPlayerDataLeft() { }
        public virtual void OnPlayerDataRejoin() { }
    }
}
