using UdonSharp;
using UnityEngine;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataSharedOptionsUILogic : UdonSharpBehaviour
    {
        [HideInInspector][SerializeField][SingletonReference] private WidgetManager widgetManager;

        private FoldOutWidgetData playerDataOptionsFoldout;
        private ButtonWidgetData selectAllButton;
        private ButtonWidgetData selectNoneButton;
        private ToggleFieldWidgetData[] playerDataOptionToggles = new ToggleFieldWidgetData[ArrList.MinCapacity];
        private int playerDataOptionTogglesCount = 0;

        public void InitWidgetData()
        {
            playerDataOptionsFoldout = widgetManager.NewFoldOutScope("Player Data Options", foldedOut: true);
            playerDataOptionsFoldout.IsVisible = false;
            selectAllButton = (ButtonWidgetData)widgetManager.NewButton("Select All").SetListener(this, nameof(OnSelectAllClick));
            selectNoneButton = (ButtonWidgetData)widgetManager.NewButton("Select None").SetListener(this, nameof(OnSelectNoneClick));
            playerDataOptionsFoldout.AddChildDynamic(selectAllButton);
            playerDataOptionsFoldout.AddChildDynamic(selectNoneButton);
        }

        public void OnOptionsEditorShow(LockstepOptionsEditorUI ui)
        {
            ui.Root.AddChildDynamic(playerDataOptionsFoldout);
        }

        public void OnOptionsEditorHide(LockstepOptionsEditorUI ui)
        {
            playerDataOptionsFoldout.IsVisible = false;
            playerDataOptionsFoldout.ClearChildren();
            playerDataOptionsFoldout.AddChildDynamic(selectAllButton);
            playerDataOptionsFoldout.AddChildDynamic(selectNoneButton);
            ArrList.Clear(ref playerDataOptionToggles, ref playerDataOptionTogglesCount);
        }

        public void AddPlayerDataOptionToggle(ToggleFieldWidgetData toggle)
        {
            playerDataOptionsFoldout.AddChildDynamic(toggle);
            playerDataOptionsFoldout.IsVisible = true;
            ArrList.Add(ref playerDataOptionToggles, ref playerDataOptionTogglesCount, toggle);
        }

        public void OnSelectAllClick()
        {
            for (int i = 0; i < playerDataOptionTogglesCount; i++)
            {
                ToggleFieldWidgetData toggle = playerDataOptionToggles[i];
                if (toggle.Interactable)
                    toggle.Value = true;
            }
        }

        public void OnSelectNoneClick()
        {
            for (int i = 0; i < playerDataOptionTogglesCount; i++)
            {
                ToggleFieldWidgetData toggle = playerDataOptionToggles[i];
                if (toggle.Interactable)
                    toggle.Value = false;
            }
        }
    }
}
