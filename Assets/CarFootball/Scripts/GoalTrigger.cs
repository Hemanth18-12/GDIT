using UnityEngine;

/// <summary>Invisible trigger box inside the goal. Adds a goal when the ball (tag "Ball") enters.</summary>
[RequireComponent(typeof(BoxCollider))]
public class GoalTrigger : MonoBehaviour
{
    void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        if (GameManager.Instance != null) GameManager.Instance.GoalScored();
    }
}
