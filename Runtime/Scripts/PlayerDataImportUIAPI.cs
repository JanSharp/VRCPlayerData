namespace JanSharp
{
    [SingletonScript("28a5e083347ce2753aa92dfda01bef32")] // Runtime/Prefabs/PlayerDataManager.prefab
    public abstract class PlayerDataImportUIAPI : LockstepGameStateOptionsUI
    {
        /// <summary>
        /// <para>Only valid to call inside of
        /// <see cref="LockstepGameStateOptionsUI.OnOptionsEditorShow(LockstepOptionsEditorUI, uint)"/>,
        /// before or after <see cref="PlayerDataManagerAPI"/> in terms of load order does not matter.</para>
        /// </summary>
        /// <param name="toggle"></param>
        public abstract void AddPlayerDataOptionToggle(ToggleFieldWidgetData toggle);
    }
}
