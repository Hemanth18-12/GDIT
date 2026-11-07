using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LegacyOfTemples.World.Editor
{
    public static class CinematicLookTools
    {
        private const string ProfileFolder = "Assets/_Project/Settings/Cinematic";

        private static T Override<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component)) component = profile.Add<T>(true);
            if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        [MenuItem("Tools/Legacy/Apply Cinematic Scene Lighting")]
        public static void Apply()
        {
            Directory.CreateDirectory(ProfileFolder);
            AssetDatabase.Refresh();
            var report = new StringBuilder("Lighting changes — configuration report; visual and FPS validation required.\n");
            string[] names = { "MainMenu", "Courtyard", "Sanctum", "PuzzleWall", "ShrineCrime", "JunglePath" };
            foreach (string name in names)
            {
                string folder = name == "MainMenu" ? "Dev" : "Environments";
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{folder}/{name}.unity");
                if (name == "MainMenu")
                {
                    foreach (var camera in Object.FindObjectsByType<LegacyOfTemples.UI.MenuCameraController>())
                    {
                        var settings = new SerializedObject(camera);
                        settings.FindProperty("orbitDistance").floatValue = 42;
                        settings.FindProperty("cameraHeight").floatValue = 12;
                        settings.FindProperty("orbitCenter").vector3Value = new Vector3(0, 5.5f, 0);
                        settings.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                bool dawn = name == "Courtyard" || name == "MainMenu";
                bool interior = name == "Sanctum" || name == "PuzzleWall";
                if (name == "Sanctum")
                {
                    var shrine = GameObject.Find("LordJagannath_SacredDeityShrine");
                    if (shrine != null)
                    {
                        var renderers = shrine.GetComponentsInChildren<Renderer>(true);
                        var bounds = renderers[0].bounds;
                        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                        report.AppendLine($"Background shrine before: {bounds}");
                        shrine.transform.localScale *= 3f / bounds.size.y;
                        bounds = renderers[0].bounds;
                        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                        shrine.transform.position += new Vector3(-bounds.center.x, 0.7f-bounds.min.y, -7.5f-bounds.center.z);
                        report.AppendLine("Background shrine instance fitted to 3m height and centred on its existing altar; no interaction or pickup added.");
                    }
                }
                string path = $"{ProfileFolder}/{name}.asset";
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    profile.name = name + " Cinematic";
                    AssetDatabase.CreateAsset(profile, path);
                }
                profile.components.RemoveAll(c => c == null);
                Override<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
                var color = Override<ColorAdjustments>(profile);
                color.postExposure.Override(dawn ? -0.35f : -0.15f);
                color.contrast.Override(6f);
                color.saturation.Override(-8f);
                color.colorFilter.Override(dawn ? new Color(1f, 0.985f, 0.95f) : new Color(0.96f, 0.985f, 1f));
                var bloom = Override<Bloom>(profile);
                bloom.threshold.Override(1.2f);
                bloom.intensity.Override(0.16f);
                bloom.scatter.Override(0.5f);
                var vignette = Override<Vignette>(profile);
                vignette.intensity.Override(0.18f);
                vignette.smoothness.Override(0.5f);
                var volumes = Object.FindObjectsByType<Volume>();
                Volume volume = volumes.Length > 0 ? volumes[0] : new GameObject(name + " Cinematic Volume").AddComponent<Volume>();
                foreach (var other in volumes) if (other != volume) other.enabled = false;
                volume.isGlobal = true;
                volume.priority = 5;
                volume.sharedProfile = profile;
                EditorUtility.SetDirty(profile);
                foreach (var camera in Object.FindObjectsByType<Camera>())
                {
                    var data = camera.GetUniversalAdditionalCameraData();
                    data.renderPostProcessing = true;
                    camera.allowHDR = true;
                    camera.farClipPlane = 180;
                }
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = dawn ? new Color(0.54f, 0.62f, 0.73f) : new Color(0.30f, 0.37f, 0.47f);
                RenderSettings.ambientEquatorColor = dawn ? new Color(0.38f, 0.40f, 0.43f) : new Color(0.24f, 0.27f, 0.32f);
                RenderSettings.ambientGroundColor = dawn ? new Color(0.20f, 0.18f, 0.15f) : new Color(0.13f, 0.12f, 0.11f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = dawn ? 0.009f : (interior ? 0.012f : 0.022f);
                RenderSettings.fogColor = dawn ? new Color(0.36f, 0.40f, 0.46f) : new Color(0.055f, 0.105f, 0.14f);
                if (!dawn)
                {
                    var nightSky = AssetDatabase.LoadAssetAtPath<Material>($"{ProfileFolder}/NightSky.mat");
                    if (nightSky == null)
                    {
                        nightSky = new Material(Shader.Find("Skybox/Procedural"));
                        nightSky.SetFloat("_SunDisk", 0);
                        nightSky.SetFloat("_Exposure", 0.18f);
                        nightSky.SetColor("_SkyTint", new Color(0.20f, 0.35f, 0.55f));
                        nightSky.SetColor("_GroundColor", new Color(0.025f, 0.04f, 0.05f));
                        AssetDatabase.CreateAsset(nightSky, $"{ProfileFolder}/NightSky.mat");
                    }
                    RenderSettings.skybox = nightSky;
                }
                // This older fake light shaft was an opaque limestone cylinder.
                var opaqueShaft = GameObject.Find("Moonlight_ShaftVolume");
                if (opaqueShaft != null && opaqueShaft.TryGetComponent<Renderer>(out var shaftRenderer)) shaftRenderer.enabled = false;
                int pointsWithShadows = 0, enabledLights = 0;
                foreach (var light in Object.FindObjectsByType<Light>())
                {
                    if (light.name == "Sky fill for readable silhouettes") continue;
                    if (light.type == LightType.Rectangle || light.type == LightType.Disc)
                    {
                        light.lightmapBakeType = LightmapBakeType.Baked;
                        light.enabled = false;
                        continue;
                    }
                    if (light.type == LightType.Directional)
                    {
                        light.color = dawn ? new Color(1f, 0.851f, 0.627f) : new Color(0.38f, 0.57f, 0.85f);
                        light.intensity = dawn ? 1.85f : (interior ? 0.18f : 0.48f);
                        if (dawn) light.transform.rotation = Quaternion.Euler(16, -32, 0);
                        light.shadows = LightShadows.Soft;
                        light.shadowStrength = 0.85f;
                    }
                    else
                    {
                        light.color = new Color(1f, 0.67f, 0.34f);
                        light.intensity = interior ? 1.6f : 1.0f;
                        light.shadows = pointsWithShadows++ < 2 ? LightShadows.Soft : LightShadows.None;
                        light.range = Mathf.Min(light.range, 8.5f);
                        if (light.TryGetComponent<LightFlicker>(out var flicker))
                            flicker.Configure(interior ? 1.35f : 0.85f, interior ? 1.65f : 1.05f, 1.4f);
                    }
                    if (light.enabled) enabledLights++;
                }
                var fillObject = GameObject.Find("Sky fill for readable silhouettes");
                if (fillObject == null) fillObject = new GameObject("Sky fill for readable silhouettes");
                var fill = fillObject.GetComponent<Light>();
                if (fill == null) fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.color = dawn ? new Color(0.64f, 0.76f, 1f) : new Color(0.48f, 0.66f, 0.9f);
                fill.intensity = dawn ? 0.65f : (interior ? 0.5f : 0.6f);
                fill.shadows = LightShadows.None;
                fill.transform.rotation = Quaternion.Euler(42, 145, 0);
                enabledLights++;
                if (dawn || name == "JunglePath") AddHorizon(scene, name, dawn);
                EditorSceneManager.SaveScene(scene);
                report.AppendLine($"{name}: {enabledLights} lights, at most 2 point-light shadow maps, ACES, bloom 0.16 / threshold 1.2, exposure {color.postExposure.value}, components persisted as subassets={profile.components.Count}");
            }
            foreach (string materialName in new[] { "mat_StoneFloorWet", "mat_StoneTemple", "mat_WarmTempleStone", "mat_LightStone", "mat_LimestoneRelief", "mat_CarvedPillar", "mat_DarkStone", "mat_CourtyardStone" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Models/Environment/Materials/{materialName}.mat");
                if (material == null) continue;
                material.SetFloat("_Smoothness", materialName == "mat_StoneFloorWet" ? 0.12f : 0.18f);
                material.SetFloat("_Metallic", 0f);
                if (materialName == "mat_DarkStone") material.SetColor("_BaseColor", new Color(0.48f, 0.44f, 0.36f));
                if (materialName == "mat_CourtyardStone") material.SetColor("_BaseColor", new Color(0.57f, 0.53f, 0.45f));
                EditorUtility.SetDirty(material);
            }
            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Models/Environment/Materials/mat_SunriseSkybox.mat");
            if (sky != null)
            {
                sky.SetFloat("_Exposure", 0.8f);
                sky.SetFloat("_SunSize", 0.025f);
                sky.SetColor("_SkyTint", new Color(0.46f, 0.53f, 0.65f));
                EditorUtility.SetDirty(sky);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(GroundingTools.EvidenceRoot, "lighting-configuration.txt"), report.ToString());
        }

        private static void AddHorizon(Scene scene, string sceneName, bool dawn)
        {
            if (GameObject.Find("Distant terrain silhouette") != null) return;
            const int segments = 64;
            var vertices = new Vector3[(segments+1)*3];
            var triangles = new int[segments*12];
            for (int ring = 0; ring < 3; ring++)
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    float radius = ring == 0 ? 38 : (ring == 1 ? 75 : 125);
                    float ridge = 9 + Mathf.PerlinNoise(Mathf.Cos(angle)*2+5, Mathf.Sin(angle)*2+5)*16;
                    float y = ring == 0 ? -1.2f : (ring == 1 ? ridge : -2);
                    vertices[ring*(segments+1)+i] = new Vector3(Mathf.Cos(angle)*radius, y, Mathf.Sin(angle)*radius);
                }
            int t = 0;
            for (int ring=0; ring<2; ring++) for (int i=0; i<segments; i++)
            {
                int a=ring*(segments+1)+i, b=a+segments+1;
                triangles[t++]=a; triangles[t++]=a+1; triangles[t++]=b;
                triangles[t++]=a+1; triangles[t++]=b+1; triangles[t++]=b;
            }
            var mesh = new Mesh { name = sceneName + " horizon" };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals();
            string meshPath = $"{ProfileFolder}/{sceneName}Horizon.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null) Object.DestroyImmediate(mesh);
            else AssetDatabase.CreateAsset(mesh, meshPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{ProfileFolder}/Horizon.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(0.16f, 0.21f, 0.19f);
                material.SetFloat("_Smoothness", 0);
                AssetDatabase.CreateAsset(material, $"{ProfileFolder}/Horizon.mat");
            }
            var obj = new GameObject("Distant terrain silhouette");
            SceneManager.MoveGameObjectToScene(obj, scene);
            obj.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            obj.isStatic = true;
        }
    }
}
