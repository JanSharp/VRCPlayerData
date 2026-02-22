using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataExportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => true;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        public override LockstepGameStateOptionsData Clone()
        {
            PlayerDataExportOptions clone = WannaBeClasses.New<PlayerDataExportOptions>(nameof(PlayerDataExportOptions));
            return clone;
        }

        public override void Deserialize(bool isImport, uint importedDataVersion)
        {
        }

        public override void Serialize(bool isExport)
        {
        }
    }
}
