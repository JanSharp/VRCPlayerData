namespace JanSharp
{
    [SingletonScript("28a5e083347ce2753aa92dfda01bef32")] // Runtime/Prefabs/PlayerDataManager.prefab
    public abstract class PlayerDataManagerAPI : LockstepGameState
    {
        public abstract void RegisterCustomPlayerDataDynamic(string playerDataClassName);
        public abstract CorePlayerData GetCorePlayerDataForPlayerId(uint playerId);
        public abstract CorePlayerData GetCorePlayerDataForPersistentId(uint persistentId);
        public abstract PlayerData GetPlayerDataForPlayerIdDynamic(string playerDataClassName, uint playerId);
        public abstract PlayerData GetPlayerDataForPersistentIdDynamic(string playerDataClassName, uint persistentId);
        public abstract PlayerData GetPlayerDataFromCoreDynamic(string playerDataClassName, CorePlayerData corePlayerData);
        public abstract PlayerData[] GetAllPlayerDataDynamic(string playerDataClassName);
        public abstract uint GetPersistentIdFromImportedId(uint importedPersistentId);
    }

    public static class PlayerDataManagerExtensions
    {
        public static void RegisterCustomPlayerData<T>(this PlayerDataManagerAPI manager, string playerDataClassName)
            where T : PlayerData
        {
            manager.RegisterCustomPlayerDataDynamic(playerDataClassName);
        }

        public static T GetPlayerDataForPlayerId<T>(this PlayerDataManagerAPI manager, string playerDataClassName, uint playerId)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataForPlayerIdDynamic(playerDataClassName, playerId);
        }

        public static T GetPlayerDataForPersistentId<T>(this PlayerDataManagerAPI manager, string playerDataClassName, uint persistentId)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataForPersistentIdDynamic(playerDataClassName, persistentId);
        }

        public static T GetPlayerDataFromCore<T>(this PlayerDataManagerAPI manager, string playerDataClassName, CorePlayerData corePlayerData)
            where T : PlayerData
        {
            return (T)manager.GetPlayerDataFromCoreDynamic(playerDataClassName, corePlayerData);
        }

        public static T[] GetAllPlayerData<T>(this PlayerDataManagerAPI manager, string playerDataClassName)
            where T : PlayerData
        {
            return (T[])manager.GetAllPlayerDataDynamic(playerDataClassName);
        }
    }
}
