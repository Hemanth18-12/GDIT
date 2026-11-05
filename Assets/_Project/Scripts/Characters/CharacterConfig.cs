using UnityEngine;
using LegacyOfTemples.Core;

namespace LegacyOfTemples.Characters
{
    [CreateAssetMenu(fileName = "CharacterConfig_", menuName = "Legacy of Temples/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterId = Constants.Investigator;
        [SerializeField] private string displayName = "Investigator";
        [SerializeField] private bool isPlayer = false;

        [Header("Prefab")]
        [SerializeField] private GameObject prefab;

        [Header("Locomotion Attributes")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float runSpeed = 7.0f;
        [SerializeField] private float rotationSpeed = 12.0f;

        [Header("Aesthetics")]
        [SerializeField] private Color primaryColor = new Color(0.78f, 0.68f, 0.52f);
        [SerializeField] private Color accentColor = new Color(0.24f, 0.18f, 0.14f);

        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public bool IsPlayer => isPlayer;
        public GameObject Prefab => prefab;
        public float MoveSpeed => moveSpeed;
        public float RunSpeed => runSpeed;
        public float RotationSpeed => rotationSpeed;
        public Color PrimaryColor => primaryColor;
        public Color AccentColor => accentColor;

        public void SetConfig(string id, string name, bool player, GameObject charPrefab, float speed = 4.5f)
        {
            characterId = id;
            displayName = name;
            isPlayer = player;
            prefab = charPrefab;
            moveSpeed = speed;
        }
    }
}

