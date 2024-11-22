using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CorePlayerData : WannaBeClass
    {
        [System.NonSerialized] public int index;
        [System.NonSerialized] public uint playerId;
        [System.NonSerialized] public string displayName;
        [System.NonSerialized] public bool isOffline;
        [System.NonSerialized] public CorePlayerData overshadowingPlayerData;
        public bool IsOvershadowed => overshadowingPlayerData != null;
        [System.NonSerialized] public PlayerData[] customPlayerData;
    }
}
