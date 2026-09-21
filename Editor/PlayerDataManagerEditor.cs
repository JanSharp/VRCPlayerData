using System.Collections.Generic;
using System.Linq;
using JanSharp.Internal;
using UnityEditor;

namespace JanSharp
{
    public static class PlayerDataManagerOnBuild
    {
        private static List<CustomPlayerData> allPlayerData = new();

        [OrderedInitializeOnLoad]
        private static void OnAssemblyLoad()
        {
            OnBuildUtil.RegisterTypeCumulative<CustomPlayerData>(OnPlayerDataBuild, order: -10);
            OnBuildUtil.RegisterType<PlayerDataManager>(OnBuild, order: -5);
        }

        private static bool OnPlayerDataBuild(IEnumerable<CustomPlayerData> allPlayerData)
        {
            // There is always an instance of each PlayerData class in the scene due to how WannaBeClasses work.
            PlayerDataManagerOnBuild.allPlayerData.Clear();
            PlayerDataManagerOnBuild.allPlayerData.AddRange(allPlayerData);
            return true;
        }

        private static bool OnBuild(PlayerDataManager manager)
        {
            SerializedObject so = new SerializedObject(manager);
            var allAssociations = allPlayerData
                .Select(d => (className: d.GetType().Name, internalName: d.PlayerDataInternalName))
                .Distinct()
                .OrderBy(d => d.className)
                .ToList();
            EditorUtil.SetArrayProperty(
                so.FindProperty(PlayerDataManager.InternalNameByClassNameKeysPropName),
                allAssociations,
                (p, v) => p.stringValue = v.className);
            EditorUtil.SetArrayProperty(
                so.FindProperty(PlayerDataManager.InternalNameByClassNameValuesPropName),
                allAssociations,
                (p, v) => p.stringValue = v.internalName);
            so.ApplyModifiedProperties();
            return true;
        }
    }
}
