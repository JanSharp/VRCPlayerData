using UdonSharp;
using UnityEngine.UI;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TestPlayerHitBox : UdonSharpBehaviour
    {
        [System.NonSerialized] public uint playerId;
        [System.NonSerialized] public PlayerHealthAndExpData playerData;

        public void UpdateHealthBar()
        {
            GetComponentInChildren<Slider>().value = ((float)playerData.health / PlayerHealthAndExpData.MaxHealth);
        }
    }
}
