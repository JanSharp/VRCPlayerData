using System.Text;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataImportUI : LockstepGameStateOptionsUI
    {
        public override string OptionsClassName => nameof(PlayerDataImportOptions);
        [System.NonSerialized] public PlayerDataImportOptions currentOptions;

        [HideInInspector][SerializeField][SingletonReference] private PlayerDataManagerAPI playerDataManager;

        private FoldOutWidgetData infoFoldout;
        private LabelWidgetData infoLabel;
        private FoldOutWidgetData playersInfoFoldout;
        private LabelWidgetData playersInfoLabel;

        protected override LockstepGameStateOptionsData NewOptionsImpl()
        {
            return wannaBeClasses.New<PlayerDataImportOptions>(nameof(PlayerDataImportOptions));
        }

        [System.NonSerialized] public PlayerDataImportOptions optionsToValidate;
        protected override void ValidateOptionsImpl()
        {
        }

        protected override void InitWidgetData()
        {
            infoFoldout = widgetManager.NewFoldOutScope("Player Data", foldedOut: false);
            infoLabel = widgetManager.NewLabel("");
            infoFoldout.AddChildDynamic(infoLabel);

            playersInfoFoldout = widgetManager.NewFoldOutScope("", foldedOut: false);
            playersInfoLabel = widgetManager.NewLabel("");
            playersInfoFoldout.AddChildDynamic(playersInfoLabel);
        }

        private void UpdatePlayersInfo()
        {
            int count = (int)lockstep.ReadSmallUInt();
            playersInfoFoldout.Label = $"Players To Import ({count})";

            StringBuilder sb = new StringBuilder();
            sb.Append("<size=80%>");
            for (int i = 0; i < count; i++)
            {
                if (i != 0)
                    sb.Append('\n'); // AppendLine could add \r\n. \r is outdated. \n is the future.
                lockstep.ReadSmallUInt(); // Discard persistentId.
                sb.Append(lockstep.ReadString()); // Player displayName.
            }
            playersInfoLabel.Label = sb.ToString();
        }

        private const int MetadataInternalNameIndex = 0;
        private const int MetadataDisplayNameIndex = 1;
        private const int MetadataDataVersionIndex = 2;
        private const int MetadataExistsInThiSWorldIndex = 3;
        private const int MetadataSize = 4;

        private object[][] ReadImportedMetadata(out DataDictionary importedMetadataByInternalName)
        {
            int importedCount = (int)lockstep.ReadSmallUInt();
            importedMetadataByInternalName = new DataDictionary();
            object[][] importedMetadata = new object[importedCount][];
            for (int i = 0; i < importedCount; i++)
            {
                string internalName = lockstep.ReadString();
                string displayName = lockstep.ReadString();
                uint dataVersion = lockstep.ReadSmallUInt();
                int contentSize = lockstep.ReadInt();
                lockstep.ReadBytes(contentSize, skip: true);
                object[] metadata = new object[MetadataSize];
                metadata[MetadataInternalNameIndex] = internalName;
                metadata[MetadataDisplayNameIndex] = displayName;
                metadata[MetadataDataVersionIndex] = dataVersion;
                metadata[MetadataExistsInThiSWorldIndex] = false;
                importedMetadata[i] = metadata;
                importedMetadataByInternalName.Add(internalName, new DataToken(metadata));
            }
            return importedMetadata;
        }

        private void UpdateCustomPlayerDataInfoLabel(out bool hasAnyCustomPlayerData, out bool anyWarnings)
        {
            anyWarnings = false;

            object[][] importedMetadata = ReadImportedMetadata(out DataDictionary importedMetadataByInternalName);
            PlayerData[] customPlayerData = playerDataManager.LocalPlayerData.customPlayerData;
            hasAnyCustomPlayerData = customPlayerData.Length != 0 || importedMetadata.Length != 0;
            if (!hasAnyCustomPlayerData)
                return;

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
                if (!importedMetadataByInternalName.TryGetValue(playerData.PlayerDataInternalName, out DataToken metadataToken))
                {
                    if (!playerData.SupportsImportExport)
                        sb.Append(" - <color=#888888>does not support import</color>");
                    else
                    {
                        sb.Append(" - <color=#ffaaaa>not in imported data</color>");
                        anyWarnings = true;
                    }
                }
                else
                {
                    object[] metadata = (object[])metadataToken.Reference;
                    metadata[MetadataExistsInThiSWorldIndex] = true;

                    string errorMsg = null;
                    uint dataVersion = (uint)metadata[MetadataDataVersionIndex];
                    if (!playerData.SupportsImportExport)
                        errorMsg = "no longer supports import";
                    else if (dataVersion > playerData.DataVersion)
                        errorMsg = "imported version too new";
                    else if (dataVersion < playerData.LowestSupportedDataVersion)
                        errorMsg = "imported version too old";

                    if (errorMsg != null)
                    {
                        sb.Append(" - <color=#ffaaaa>");
                        sb.Append(errorMsg);
                        sb.Append("</color>");
                        anyWarnings = true;
                    }
                    else
                        sb.Append(" - <color=#99ccff>supports import</color>");
                }
            }

            foreach (object[] metadata in importedMetadata)
            {
                if ((bool)metadata[MetadataExistsInThiSWorldIndex])
                    continue;

                if (isFirstLine)
                    isFirstLine = false;
                else
                    sb.Append('\n'); // AppendLine could add \r\n. \r is outdated. \n is the future.

                sb.Append((string)metadata[MetadataDisplayNameIndex]);
                sb.Append(" - <color=#ffaaaa>not in this world</color>");
                anyWarnings = true;
            }

            infoLabel.Label = sb.ToString();
        }

        protected override void OnOptionsEditorShow(LockstepOptionsEditorUI ui, uint importedDataVersion)
        {
            // Order of these calls matters as they are reading exported data.
            UpdatePlayersInfo();
            UpdateCustomPlayerDataInfoLabel(out bool hasAnyCustomPlayerData, out bool anyWarnings);

            if (hasAnyCustomPlayerData)
            {
                if (anyWarnings)
                    ui.Info.FoldedOut = true; // Else retain state, don't set to false.
                infoFoldout.FoldedOut = anyWarnings;
                ui.Info.AddChildDynamic(infoFoldout);
            }

            ui.Info.AddChildDynamic(playersInfoFoldout);
        }

        protected override void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
        }

        protected override void UpdateCurrentOptionsFromWidgetsImpl()
        {
        }
    }
}
