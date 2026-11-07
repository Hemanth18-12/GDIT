using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using LegacyOfTemples.Core;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.World.Editor
{
    public static class ShowcaseEditorTools
    {
        [MenuItem("Tools/Legacy/Showcase")]
        public static void RunShowcaseInEditor()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Showcase Mode", "Please enter Play Mode first, or run from the Main Menu 'SHOWCASE TOUR' button.", "OK");
                return;
            }

            if (ShowcaseDirector.Instance != null)
            {
                ShowcaseDirector.Instance.StartTour();
            }
            else
            {
                GameObject go = new GameObject("ShowcaseDirector");
                ShowcaseDirector director = go.AddComponent<ShowcaseDirector>();
                director.StartTour();
            }
        }
    }
}

