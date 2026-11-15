using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Gameplay.Puzzles
{
    /// <summary>A recovered treasure lying where the puzzle revealed it. Taking it is a physical act (E = take) and adds it to the score.</summary>
    public class TreasurePickup : MonoBehaviour, IInteractable
    {
        public string treasureName = "treasure";
        public string zoneId = "zone";
        public float radius = 2.2f;
        public bool Taken { get; private set; }
        public static event System.Action<TreasurePickup> OnTaken;

        public string Id => "treasure." + treasureName;
        public string Prompt => Taken ? "" : "Take " + treasureName;
        public Transform Anchor => transform;
        public float Radius => Taken ? 0f : radius;
        public void Interact()
        {
            if (Taken || !isActiveAndEnabled) return; Taken = true;
            PuzzleSave.Set(Id, 1); ScoreSystem.ItemRecovered(zoneId);
            ObjectiveBanner.Show("RECOVERED", treasureName, 4f);
            Debug.Log($"[Treasure] recovered {treasureName} in {zoneId}; zone points now {ScoreSystem.Points(ScoreSystem.Zone(zoneId))}");
            OnTaken?.Invoke(this);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        }
    }
}
