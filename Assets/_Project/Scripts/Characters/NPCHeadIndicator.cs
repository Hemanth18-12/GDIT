using UnityEngine;

namespace LegacyOfTemples.Characters
{
    public class NPCHeadIndicator : MonoBehaviour
    {
        [SerializeField] private string characterName = "Witness";
        [SerializeField] private Color tagColor = new Color(1.0f, 0.82f, 0.35f);
        [SerializeField] private float heightOffset = 2.05f;
        [SerializeField] private float maxVisibleDistance = 25.0f;


        public void Setup(string name, Color color, float height = 2.05f)
        {
            characterName = name;
            tagColor = color;
            heightOffset = height;
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 worldPos = transform.position + Vector3.up * heightOffset;
            float d = Vector3.Distance(cam.transform.position, worldPos);
            if (d > Mathf.Min(15f, maxVisibleDistance)) return;
            float fade = Mathf.Clamp01((15f - d) / 5f);
            // Drawn with TextMeshPro by LegacyOfTemples.UI.ScreenTextRenderer (no overlap, >= 30 px).
            LegacyOfTemples.Core.ScreenText.WorldLabel("npc_" + characterName, worldPos, characterName, new Color(1f, 0.957f, 0.839f, fade));
        }
    }
}

