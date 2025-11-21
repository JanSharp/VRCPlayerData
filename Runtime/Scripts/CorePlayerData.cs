using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CorePlayerData : WannaBeClass
    {
        [HideInInspector][SingletonReference] public PlayerDataManagerAPI manager;
        [System.NonSerialized] public int index;
        [System.NonSerialized] public uint persistentId;
        [System.NonSerialized] public uint importedPersistentId;
        [System.NonSerialized] public uint playerId;
        [System.NonSerialized] public VRCPlayerApi playerApi;
        // TODO: probably just remove this entirely and make people use Utilities.IsValid, much to my dismay.
        public VRCPlayerApi PlayerApi
        {
            get
            {
                VRCPlayerApi player = playerApi;
                if (!Utilities.IsValid(player))
                {
                    playerApi = null;
                    return null;
                }
                return player;
            }
        }
        [System.NonSerialized] public string displayName;
        [System.NonSerialized] public bool isOffline;
        [System.NonSerialized] public CorePlayerData overshadowingPlayerData;
        public bool IsOvershadowed => overshadowingPlayerData != null;
        [System.NonSerialized] public PlayerData[] customPlayerData;

        public PlayerData GetPlayerDataDynamic(string playerDataClassName)
        {
            return manager.GetPlayerDataFromCoreDynamic(playerDataClassName, this);
        }
    }

    public static class CorePlayerDataExtensions
    {
        public static T GetPlayerData<T>(this CorePlayerData corePlayerData, string playerDataClassName)
            where T : PlayerData
        {
            return (T)corePlayerData.manager.GetPlayerDataFromCoreDynamic(playerDataClassName, corePlayerData);
        }
    }
}
