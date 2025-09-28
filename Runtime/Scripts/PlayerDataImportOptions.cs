using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataImportOptions : LockstepGameStateOptionsData
    {
        public override bool SupportsImportExport => false;
        public override uint DataVersion => 0u;
        public override uint LowestSupportedDataVersion => 0u;

        public override LockstepGameStateOptionsData Clone()
        {
            PlayerDataImportOptions clone = WannaBeClasses.New<PlayerDataImportOptions>(nameof(PlayerDataImportOptions));
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
