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
        /// <summary>
        /// <para><c>0u</c> is an invalid id.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        [System.NonSerialized] public uint persistentId;
        /// <summary>
        /// <para><c>0u</c> is an invalid id.</para>
        /// </summary>
        [System.NonSerialized] public uint importedPersistentId;
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        [System.NonSerialized] public uint playerId;
        [System.NonSerialized] public VRCPlayerApi playerApi;
        /// <summary>
        /// <para>Never <see langword="null"/>.</para>
        /// <para>Game state safe.</para>
        /// </summary>
        [System.NonSerialized] public string displayName;
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        [System.NonSerialized] public bool isOffline;
        /// <summary>
        /// <para>Game state safe.</para>
        /// </summary>
        [System.NonSerialized] public bool isDeleted;
        /// <summary>
        /// <para>Not game state safe.</para>
        /// </summary>
        [System.NonSerialized] public bool isLocal;

        /// <summary>
        /// <para>The player overshadowing this player.</para>
        /// <para>Weak <see cref="WannaBeClass"/> reference.</para>
        /// </summary>
        [System.NonSerialized] public CorePlayerData overshadowingPlayerData;
        /// <summary>
        /// <para>Is this player overshadowed by other player data?</para>
        /// <para>Only one of <see cref="IsOvershadowed"/> and <see cref="IsOvershadowing"/> can be
        /// <see langword="true"/> at a time.</para>
        /// </summary>
        public bool IsOvershadowed => overshadowingPlayerData != null;
        /// <summary>
        /// <para>Used when <see cref="IsOvershadowed"/> is <see langword="true"/>.</para>
        /// <para>A non circular linked list of players overshadowed by
        /// <see cref="overshadowingPlayerData"/>.</para>
        /// <para>Weak <see cref="WannaBeClass"/> reference.</para>
        /// </summary>
        [System.NonSerialized] public CorePlayerData nextOvershadowedPlayerData;
        /// <inheritdoc cref="nextOvershadowedPlayerData"/>
        [System.NonSerialized] public CorePlayerData prevOvershadowedPlayerData;

        /// <summary>
        /// <para>The first player overshadowed by this player.</para>
        /// <para>Use <see cref="nextOvershadowedPlayerData"/> on that player to walk through the linked list
        /// of players overshadowed by this player data.</para>
        /// <para>Weak <see cref="WannaBeClass"/> reference.</para>
        /// </summary>
        [System.NonSerialized] public CorePlayerData firstOvershadowedPlayerData;
        /// <summary>
        /// <para>The last player overshadowed by this player.</para>
        /// <para>Use <see cref="prevOvershadowedPlayerData"/> on that player to walk through the linked list
        /// of players overshadowed by this player data.</para>
        /// <para>Weak <see cref="WannaBeClass"/> reference.</para>
        /// </summary>
        [System.NonSerialized] public CorePlayerData lastOvershadowedPlayerData;
        /// <summary>
        /// <para>Is this player overshadowing other player data?</para>
        /// <para>Only one of <see cref="IsOvershadowed"/> and <see cref="IsOvershadowing"/> can be
        /// <see langword="true"/> at a time.</para>
        /// </summary>
        public bool IsOvershadowing => firstOvershadowedPlayerData != null;

        /// <summary>
        /// <para>Strong <see cref="WannaBeClass"/> references.</para>
        /// </summary>
        [System.NonSerialized] public CustomPlayerData[] customPlayerData;

        public override bool WannaBeClassSupportsPooling => true;
        public override void ResetWannaBeClassToDefault()
        {
            index = default;
            persistentId = default;
            importedPersistentId = default;
            playerId = default;
            playerApi = default;
            displayName = default;
            isOffline = default;
            isDeleted = default;
            isLocal = default;
            overshadowingPlayerData = default;
            nextOvershadowedPlayerData = default;
            prevOvershadowedPlayerData = default;
            firstOvershadowedPlayerData = default;
            lastOvershadowedPlayerData = default;
            customPlayerData = default;
        }

        public override void WannaBeDestructor()
        {
            if (customPlayerData != null)
                foreach (CustomPlayerData playerData in customPlayerData)
                    playerData.DecrementRefsCount();
        }

        public CustomPlayerData GetPlayerDataDynamic(string playerDataClassName)
        {
            return manager.GetPlayerDataFromCoreDynamic(playerDataClassName, this);
        }
    }

    public static class CorePlayerDataExtensions
    {
        public static T GetPlayerData<T>(this CorePlayerData corePlayerData, string playerDataClassName)
            where T : CustomPlayerData
        {
            return (T)corePlayerData.manager.GetPlayerDataFromCoreDynamic(playerDataClassName, corePlayerData);
        }
    }
}
