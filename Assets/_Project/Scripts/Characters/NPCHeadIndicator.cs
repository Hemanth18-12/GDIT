using UnityEngine;

namespace LegacyOfTemples.Characters
{
    public class NPCHeadIndicator : MonoBehaviour
    {
        [SerializeField] private string characterName = "Witness";
        [SerializeField] private Color tagColor = new Color(1.0f, 0.82f, 0.35f);
        [SerializeField] private float heightOffset = 2.05f;
        [SerializeField] private float maxVisibleDistance = 25.0f;

        private GUIStyle labelStyle;
        private GUIStyle subLabelStyle;
        private GUIStyle boxStyle;
        private Texture2D boxTex;
        private bool stylesInit = false;

        public void Setup(string name, Color color, float height = 2.05f)
        {
            characterName = name;
            tagColor = color;
            heightOffset = height;
        }

        private void InitStyles()
        {
            if (stylesInit) return;

            boxTex = new Texture2D(2, 2);
            Color[] pix = new Color[4];
            for (int i = 0; i < 4; i++) pix[i] = new Color(0.08f, 0.09f, 0.12f, 0.88f);
            boxTex.SetPixels(pix);
            boxTex.Apply();

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = boxTex;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            labelStyle.normal.textColor = tagColor;

            subLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            subLabelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.92f);

            stylesInit = true;
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = transform.position + Vector3.up * heightOffset;
            float dist = Vector3.Distance(cam.transform.position, worldPos);
            if (dist > maxVisibleDistance) return;

            // Check if in front of camera
            Vector3 viewPos = cam.WorldToViewportPoint(worldPos);
            if (viewPos.z <= 0 || viewPos.x < 0 || viewPos.x > 1 || viewPos.y < 0 || viewPos.y > 1) return;

            InitStyles();

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            float screenY = Screen.height - screenPos.y;

            // Gentle floating bob
            float bob = Mathf.Sin(Time.time * 3f) * 3f;

            float w = 150f;
            float h = 34f;
            Rect rect = new Rect(screenPos.x - w * 0.5f, screenY - h - 10f + bob, w, h);

            GUI.Box(rect, GUIContent.none, boxStyle);
            GUI.Label(new Rect(rect.x, rect.y + 2f, w, 16f), $"✦ {characterName} ✦", labelStyle);
            GUI.Label(new Rect(rect.x, rect.y + 17f, w, 14f), "[E] Talk", subLabelStyle);
        }
    }
}
