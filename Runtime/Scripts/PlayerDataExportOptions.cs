using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataExportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        /// <summary>
        /// <para>This refers to players (offline and online) where all custom player data reports that there
        /// is no need to export it.</para>
        /// </summary>
        [System.NonSerialized] public bool includeUnnecessaryPlayers = false;

        public override LockstepGameStateOptionsData Clone()
        {
            PlayerDataExportOptions clone = WannaBeClasses.New<PlayerDataExportOptions>(nameof(PlayerDataExportOptions));
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
