using UnityEngine;

namespace LegacyOfTemples.Characters
{
    public class NPCFacePlayer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float detectionDistance = 6.0f;
        [SerializeField] private float turnSpeed = 4.0f;
        [SerializeField] private bool returnToDefault = true;

        private Transform playerTransform;
        private Quaternion initialRotation;

        private void Start()
        {
            initialRotation = transform.rotation;
            FindPlayer();
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            Vector3 diff = playerTransform.position - transform.position;
            diff.y = 0f; // Stay horizontal
            float distance = diff.magnitude;

            if (distance <= detectionDistance && distance > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(diff.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
            else if (returnToDefault)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, (turnSpeed * 0.5f) * Time.deltaTime);
            }
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionDistance);
        }
    }
}

