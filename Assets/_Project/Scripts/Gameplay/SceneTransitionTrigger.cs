using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class SceneTransitionTrigger : MonoBehaviour, IInteractable
    {
        [Header("Transition Target")]
        [SerializeField] private string targetSceneName = "Sanctum";
        [SerializeField] private string portalLabel = "Inner Sanctum";
        [SerializeField] private bool requireInteraction = true;
        [SerializeField] private float interactionRadius = 3.0f;

        [Header("Unlock Requirement")]
        [SerializeField] private string requiredPrefKey = "";

        public string Id => $"portal_to_{targetSceneName}";
        public string Prompt => $"Enter {portalLabel}";
        public Transform Anchor => transform;
        public float Radius => Mathf.Max(interactionRadius, 4f);

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        public void Setup(string sceneName, string label, bool needsInteract = true, string reqKey = "")
        {
            targetSceneName = sceneName;
            portalLabel = label;
            requireInteraction = needsInteract;
            requiredPrefKey = reqKey;
        }

        public void Interact()
        {
            ExecuteTransition();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!requireInteraction && other.CompareTag("Player"))
            {
                ExecuteTransition();
            }
        }

        private void ExecuteTransition()
        {
            if (!string.IsNullOrEmpty(requiredPrefKey) && PlayerPrefs.GetInt(requiredPrefKey, 0) == 0)
            {
                Debug.Log($"[SceneTransitionTrigger] Portal locked! Requires completion of '{requiredPrefKey}'.");
                return;
            }

            EventBus.RaiseSceneChanged(targetSceneName);
            Debug.Log($"[SceneTransitionTrigger] Transitioning to scene '{targetSceneName}'...");
            SceneLoader.Load(targetSceneName);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.4f);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}

