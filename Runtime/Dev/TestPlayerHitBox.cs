using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

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
