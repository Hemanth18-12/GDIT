using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Characters;
using LegacyOfTemples.World;

namespace LegacyOfTemples.Gameplay.Editor
{
    public static class NewZoneSceneTools
    {
        public const string ScenePath = "Assets/_Project/Scenes/NewWorld/TempleRing.unity";
        [MenuItem("Tools/Legacy/New Zones/Create new connected world")]
        public static void Create()
        {
            // Read the existing scene to reuse the confirmed actor and camera configuration; never save it.
            var source = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Environments/Courtyard.unity", OpenSceneMode.Single);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var camera = Object.FindAnyObjectByType<OrbitCamera>();
            if (player == null || camera == null) throw new System.Exception("Confirmed player/camera missing in source scene");
            var actor = Object.Instantiate(player.gameObject); actor.name = "Aryan";
            var cam = Object.Instantiate(camera.gameObject); cam.name = "Main Camera";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(actor, scene); SceneManager.MoveGameObjectToScene(cam, scene);
            SceneManager.SetActiveScene(scene);
            actor.transform.SetPositionAndRotation(new Vector3(0f, 0.08f, -35f), Quaternion.identity);
            var orbit = cam.GetComponent<OrbitCamera>();
            var so = new SerializedObject(orbit); so.FindProperty("target").objectReferenceValue = actor.transform; so.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("New Bazaar — runtime layout").AddComponent<NewBazaar>();
            new GameObject("New Courtyard — runtime layout").AddComponent<NewCourtyard>();
            new GameObject("New Vaults — runtime layout").AddComponent<NewVaults>();
            new GameObject("New Stepwell — runtime layout").AddComponent<NewStepwell>();
            new GameObject("New SunWall — runtime layout").AddComponent<NewSunWall>();
            new GameObject("New Jungle — runtime layout").AddComponent<NewJungleChase>();
            new GameObject("NewWorld_Director").AddComponent<NewWorldDirector>();
            var spawn = new GameObject("Arrival").AddComponent<SpawnPoint>(); spawn.transform.position = actor.transform.position;
            var sunlight = new GameObject("Late afternoon sun").AddComponent<Light>(); sunlight.type = LightType.Directional;
            sunlight.intensity = 1.5f; sunlight.color = new Color(1f, 0.86f, 0.68f); sunlight.shadows = LightShadows.Soft;
            sunlight.transform.rotation = Quaternion.Euler(32f, -65f, 0f); RenderSettings.sun = sunlight;
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.39f, 0.3f); RenderSettings.ambientGroundColor = new Color(0.22f, 0.18f, 0.13f);
            var sky = new Material(Shader.Find("Skybox/Procedural")); sky.SetFloat("_AtmosphereThickness", 0.75f);
            Directory.CreateDirectory("Assets/_Project/Scenes/NewWorld"); var oldSky = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenes/NewWorld/AfternoonSky.mat"); if (oldSky == null) AssetDatabase.CreateAsset(sky, "Assets/_Project/Scenes/NewWorld/AfternoonSky.mat"); else { Object.DestroyImmediate(sky); sky = oldSky; } RenderSettings.skybox = sky;
            var data = cam.GetComponent<UniversalAdditionalCameraData>(); if (data != null) data.renderPostProcessing = true;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(source, true);
            // Append a development scene. Legacy scenes stay enabled until the full replacement route is verified.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath); scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            var evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/QA/zones")); Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "scene-created.txt"), "TempleRing created with confirmed Aryan and orbit camera. Old scene read without saving.\n");
        }
    }
}
