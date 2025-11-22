using UdonSharp;

namespace JanSharp.Internal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerDataManagerInternalHelper : UdonSharpBehaviour
    {
        public PlayerDataManager playerDataManager;

        [LockstepEvent(LockstepEventType.OnImportFinished, Order = 10000)]
        public void OnImportFinished()
        {
            playerDataManager.OnLateImportFinished();
        }
    }
}
