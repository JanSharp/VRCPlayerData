using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerHealthAndExpData : PlayerData
    {
        [System.NonSerialized] public uint health = MaxHealth;
        [System.NonSerialized] public uint exp = 0u;

        public const uint MaxHealth = 100u;

        public override bool PersistForOfflinePlayer()
        {
            return health != MaxHealth || exp != 0u;
        }

        public override void Serialize()
        {
            lockstep.WriteSmallUInt(health);
            lockstep.WriteSmallUInt(exp);
        }

        public override void Deserialize()
        {
            health = lockstep.ReadSmallUInt();
            exp = lockstep.ReadSmallUInt();
        }
    }
}
