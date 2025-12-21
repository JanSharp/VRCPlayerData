using System.Text;
using UdonSharp;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataImportUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(PlayerDataImportOptions);
        [System.NonSerialized] public PlayerDataImportOptions currentOptions;

        private FoldOutWidgetData main;

        protected override void InitWidgetData()
        {
            main = widgetManager.NewFoldOutScope("Player Data", foldedOut: true);
        }

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<PlayerDataImportOptions>(nameof(PlayerDataImportOptions));
        }

        [System.NonSerialized] public PlayerDataImportOptions optionsToValidate;
        protected override void ValidateOptionsImpl()
        {
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            ui.Root.AddChildDynamic(main);
            int count = (int)lockstep.ReadSmallUInt();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Count: {count}");
            sb.AppendLine("persistent id - display name");
            for (int i = 0; i < count; i++)
            {
                uint importedPersistentId = lockstep.ReadSmallUInt();
                string displayName = lockstep.ReadString();
                sb.AppendLine($"{importedPersistentId} - {displayName}");
            }
            main.AddChildDynamic(widgetManager.NewLabel(sb.ToString()).StdMoveWidget());
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
            main.ClearChildren();
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
        }
    }
}
