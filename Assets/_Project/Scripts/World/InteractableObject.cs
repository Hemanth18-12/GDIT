using UnityEngine;
using UnityEngine.Events;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.World
{
    [SelectionBase]
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [Header("Interactable Configuration")]
        [SerializeField] private string id = "interactable_default";
        [SerializeField] private string prompt = "Press E to Interact";
        [SerializeField] private Transform anchor;
        [SerializeField] private float radius = 2.5f;

        [Header("Events")]
        [SerializeField] private UnityEvent onInteracted;

        public string Id => id;
        public string Prompt => prompt;
        public Transform Anchor => anchor != null ? anchor : transform;
        public float Radius => radius;

        public void Setup(string newId, string newPrompt, float newRadius = 2.5f)
        {
            id = newId;
            prompt = newPrompt;
            radius = newRadius;
        }

        public virtual void Interact()
        {
            EventBus.RaisePlayerInteract(this);
            onInteracted?.Invoke();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Transform t = Anchor;
            Gizmos.DrawWireSphere(t.position, radius);
        }
    }
}

