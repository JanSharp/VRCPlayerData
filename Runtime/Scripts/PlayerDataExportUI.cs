using System.Text;
using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataExportUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(PlayerDataExportOptions);
        [System.NonSerialized] public PlayerDataExportOptions currentOptions;

        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private FoldOutWidgetData infoFoldout;
        private LabelWidgetData infoLabel;
        private bool infoLabelIsInitialized = false;
        private bool hasAnyCustomPlayerData;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<PlayerDataExportOptions>(nameof(PlayerDataExportOptions));
        }

        [System.NonSerialized] public PlayerDataExportOptions optionsToValidate;
        protected override void ValidateOptionsImpl()
        {
        }

        protected override void InitWidgetData()
        {
            infoFoldout = widgetManager.NewFoldOutScope("Player Data", foldedOut: false);
            infoLabel = widgetManager.NewLabel("");
            infoFoldout.AddChildDynamic(infoLabel);
        }

        private void InitInfoLabel()
        {
            if (infoLabelIsInitialized)
                return;
            infoLabelIsInitialized = true;
            infoLabel.Label = BuildPlayerDataToExportMsg();
        }

        private string BuildPlayerDataToExportMsg()
        {
            PlayerData[] customPlayerData = playerDataManager.LocalPlayerData.customPlayerData;
            hasAnyCustomPlayerData = customPlayerData.Length != 0;
            if (!hasAnyCustomPlayerData)
                return "";

            StringBuilder sb = new StringBuilder();
            sb.Append("<size=80%>");
            bool isFirstLine = true;

            foreach (PlayerData playerData in customPlayerData)
            {
                if (isFirstLine)
                    isFirstLine = false;
                else
                    sb.Append('\n'); // AppendLine could add \r\n. \r is outdated. \n is the future.

                sb.Append(playerData.PlayerDataDisplayName);
                sb.Append(playerData.SupportsImportExport
                    ? " - <color=#99ccff>supports export</color>"
                    : " - <color=#888888>does not support export</color>");
            }

            return sb.ToString();
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            InitInfoLabel();
            if (hasAnyCustomPlayerData)
                ui.Info.AddChildDynamic(infoFoldout);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }
    }
}
