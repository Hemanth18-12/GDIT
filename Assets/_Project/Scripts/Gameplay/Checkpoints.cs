using UnityEngine;

namespace LegacyOfTemples.Gameplay
{
    /// <summary>
    /// One saved checkpoint: where the player stands, which zone, and how much Dawn Clock is left.
    /// Saved on entering a zone and just before a fight; a defeat puts the player back here without a loading screen.
    /// </summary>
    public static class Checkpoints
    {
        [System.Serializable]
        public class Data { public string zone = ""; public string label = ""; public string scene = ""; public Vector3 position; public float yaw; public float clockSeconds; public string savedAt = ""; public bool valid; }
        const string Key = "lot_v3_checkpoint";
        static Data current;

        public static Data Current { get { if (current == null) current = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "{}")) ?? new Data(); return current; } }
        public static bool Has => Current.valid;

        public static void Save(string zone, string label, Vector3 position, float yaw, float clockSeconds)
        {
            current = new Data { zone = zone, label = label, scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, position = position, yaw = yaw, clockSeconds = clockSeconds, savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), valid = true };
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(current)); PlayerPrefs.Save();
            Debug.Log($"[Checkpoint] {zone} / {label} at {position:0.0}");
        }

        /// <summary>Put a character back at the last checkpoint. Returns false if there is none.</summary>
        public static bool Restore(Transform who)
        {
            if (!Has || who == null) return false;
            if (!string.IsNullOrEmpty(Current.scene) && Current.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) return false;
            var cc = who.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
            who.SetPositionAndRotation(Current.position, Quaternion.Euler(0f, Current.yaw, 0f));
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
            return true;
        }

        public static void Clear() { current = new Data(); PlayerPrefs.DeleteKey(Key); }
    }
}
