using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataImportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        /// <summary>
        /// <para>Unnecessary refers to offline players where all custom player data reports that it does not
        /// need to persist.</para>
        /// </summary>
        [System.NonSerialized] public bool includeUnnecessaryPlayers = false;

        public override LockstepGameStateOptionsData Clone()
        {
            var clone = WannaBeClasses.New<PlayerDataImportOptions>(nameof(PlayerDataImportOptions));
            clone.includeUnnecessaryPlayers = includeUnnecessaryPlayers;
            return clone;
        }

        public override void Serialize(bool isExport)
        {
            lockstep.WriteFlags(includeUnnecessaryPlayers);
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
            lockstep.ReadFlags(out includeUnnecessaryPlayers);
        }
    }
}
