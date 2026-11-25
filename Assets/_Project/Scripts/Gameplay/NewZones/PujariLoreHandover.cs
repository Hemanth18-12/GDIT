using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    public sealed class PujariLoreHandover : MonoBehaviour, IInteractable
    {
        public PujariLoreDirector director;
        public string Id => "pujari_lore_handover";
        public string Prompt => "Speak with Pujari Ananda";
        public Transform Anchor => transform;
        public float Radius => 3f;
        public void Interact() { director?.HandOver(); }
    }
}
