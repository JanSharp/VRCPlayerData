using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerHealthAndExpData : PlayerData
    {
        public override string PlayerDataInternalName => "jansharp.player-health-and-exp-data";
        public override string PlayerDataDisplayName => "Player Health And Experience Data";
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        [System.NonSerialized] public uint health = MaxHealth;
        [System.NonSerialized] public uint exp = 0u;
        [System.NonSerialized] public PlayerHealthAndExpManager manager;
        [System.NonSerialized] public TestPlayerHitBox hitBox;

        public const uint MaxHealth = 100u;

        public override bool PersistPlayerDataWhileOffline()
        {
            return health != MaxHealth || exp != 0u;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteSmallUInt(health);
            lockstep.WriteSmallUInt(exp);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            health = lockstep.ReadSmallUInt();
            exp = lockstep.ReadSmallUInt();
            if (isImport && hitBox != null)
                hitBox.UpdateHealthBar();
        }
    }
}
