using UnityEngine;

namespace LegacyOfTemples.Core
{
    public interface IInteractable
    {
        string Id { get; }
        string Prompt { get; }
        Transform Anchor { get; }
        float Radius { get; }
        void Interact();
    }
}
