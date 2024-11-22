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

        public abstract bool PersistForOfflinePlayer();
        public virtual void Init() { }
        public virtual void Uninit() { }
        public virtual void OnLeft() { }
        public virtual void OnRejoin() { }
        public abstract void Serialize();
        public abstract void Deserialize();
    }
}
