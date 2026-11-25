using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A recovered treasure lying where the puzzle revealed it. Taking it is a physical act (E = take) and adds it to the score.</summary>
    public class TreasurePickup : MonoBehaviour, IInteractable
    {
        public string treasureName = "treasure";
        public string zoneId = "zone";
        public string saveId; // Stable identity when the displayed heritage label changes.
        public float radius = 2.2f;
        public bool Taken { get; private set; }
        public static event System.Action<TreasurePickup> OnTaken;

        public string Id => string.IsNullOrEmpty(saveId) ? "treasure." + treasureName : saveId;
        public string Prompt => Taken ? "" : "Take " + treasureName;
        public Transform Anchor => transform;
        public float Radius => Taken ? 0f : radius;
        void Start() { StoryDialogueSystem.Instance?.RegisterMinigame(StoryId); if (PuzzleSave.Get(Id) == 1) { Hide(); if (PujariLoreDirector.Instance == null || !PujariLoreDirector.Instance.DeferStory(this)) StoryDialogueSystem.Instance?.CompleteMinigame(StoryId); } }
        public string StoryId => "v3_" + zoneId;   // already recovered in this save
        void Hide() { Taken = true; foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false; foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false; }
        public void Interact()
        {
            if (Taken || !isActiveAndEnabled) return; Taken = true;
            PuzzleSave.Set(Id, 1); ScoreSystem.ItemRecovered(zoneId);
            ObjectiveBanner.Show("RECOVERED", treasureName, 4f); AudioManager.Instance?.PlaySFX("sfx_clue_found", 0.9f);
            Debug.Log($"[Treasure] recovered {treasureName} in {zoneId}; zone points now {ScoreSystem.Points(ScoreSystem.Zone(zoneId))}");
            OnTaken?.Invoke(this);
            if (PujariLoreDirector.Instance == null || !PujariLoreDirector.Instance.DeferStory(this)) StoryDialogueSystem.Instance?.CompleteMinigame(StoryId);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        }
    }
}
