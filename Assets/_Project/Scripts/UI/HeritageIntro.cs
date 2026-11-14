using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using LegacyOfTemples.Gameplay;

namespace LegacyOfTemples.UI
{
    /// <summary>
    /// Short introduction to the real Shree Jagannath Temple of Puri, shown once at the start of a new game,
    /// before the opening cinematic. Text cards over the game's own pictures, with narration.
    /// The last card says plainly that the game's story is fiction. Space = next card, Esc = skip.
    /// </summary>
    public sealed class HeritageIntro : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static int CardsShown { get; private set; }

        private static readonly string[][] Cards =
        {
            new[] { "SHREE JAGANNATH TEMPLE", "Puri, Odisha", "On the eastern coast of India, in the town of Puri, stands one of the country's most loved temples: the Shree Jagannath Temple." },
            new[] { "NINE HUNDRED YEARS", "Begun in the 12th century", "It was begun by King Anantavarman Chodaganga Deva of the Eastern Ganga dynasty, nearly nine hundred years ago." },
            new[] { "KALINGA ARCHITECTURE", "A tower of carved stone", "Its curved stone tower rises about sixty-five metres above the town. At the top are the Nila Chakra, the blue wheel, and a flag that is changed every day." },
            new[] { "RATH YATRA", "The festival of chariots", "Every year, three great wooden chariots are built and pulled through the streets by thousands of people." },
            new[] { "A LIVING TEMPLE", "One of the four Char Dham", "It is one of the four Char Dham pilgrimage sites, and its kitchen feeds thousands of pilgrims every day." },
            new[] { "THIS STORY IS FICTION", "Legacy of the Temple", "The village, the people and the three stolen objects in this game are invented. The respect for the real temple is not. Our story begins at ten past three in the morning." },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook() { OpeningCinematic.Prelude = Play; }

        private static IEnumerator Play()
        {
            var go = new GameObject("HeritageIntro"); var hi = go.AddComponent<HeritageIntro>();
            yield return hi.Run();
            Destroy(go);
        }

        private IEnumerator Run()
        {
            Active = true; CardsShown = 0;
            float keepScale = Time.timeScale; Time.timeScale = 0f; // the Dawn Clock must not run during the introduction
            var c = gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 800;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            var group = gameObject.AddComponent<CanvasGroup>();
            var bg = Full("Black").gameObject.AddComponent<Image>(); bg.color = Color.black;
            var picRt = Full("Picture"); var pic = picRt.gameObject.AddComponent<RawImage>(); pic.color = new Color(1f, 1f, 1f, 0f);
            var shade = Full("Shade").gameObject.AddComponent<Image>(); shade.color = new Color(0f, 0f, 0f, 0.55f);
            var title = Text("IntroTitle", 76, ScreenTextRenderer.Gold, true, new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.74f));
            var sub = Text("IntroSub", 36, new Color(0.56f, 0.86f, 0.9f), false, new Vector2(0.1f, 0.49f), new Vector2(0.9f, 0.56f));
            var body = Text("IntroBody", 40, ScreenTextRenderer.Ivory, false, new Vector2(0.16f, 0.2f), new Vector2(0.84f, 0.47f));
            var hint = Text("IntroHint", 26, ScreenTextRenderer.Gold, false, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.1f));
            hint.text = "[Space] Next     [Esc] Skip introduction";
            var set = Resources.Load<LoadingImageSet>("UI/LoadingImages");
            var voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0f; voice.ignoreListenerPause = true;

            for (int i = 0; i < Cards.Length && !OpeningCinematic.SkipRequested; i++)
            {
                Texture2D img = set != null && set.slots.Length > 0 ? set.slots[i % set.slots.Length].image : null;
                pic.texture = img; title.text = Cards[i][0]; sub.text = Cards[i][1]; body.text = Cards[i][2]; CardsShown = i + 1;
                var clip = Resources.Load<AudioClip>("Voice/narrator/intro_" + (i + 1) + "_tts");
                voice.Stop(); if (clip != null) { voice.clip = clip; voice.Play(); }
                float hold = Mathf.Max(6.5f, clip != null ? clip.length + 1.2f : 0f), t = 0f; bool next = false, skipAll = false;
                while (t < hold && !next && !skipAll && !OpeningCinematic.SkipRequested)
                {
                    t += Time.unscaledDeltaTime;
                    float a = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((hold - t) / 0.6f);
                    title.alpha = sub.alpha = body.alpha = a; pic.color = new Color(1f, 1f, 1f, img != null ? 0.75f * a : 0f);
                    picRt.localScale = Vector3.one * (1.04f + 0.05f * t / hold);
                    var kb = Keyboard.current; var gp = Gamepad.current;
                    if (t > 0.4f)
                    {
                        if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (gp != null && gp.buttonSouth.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)) next = true;
                        if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame)) skipAll = true;
                    }
                    yield return null;
                }
                if (skipAll) break;
            }
            voice.Stop();
            for (float f = 0f; f < 0.5f && !OpeningCinematic.SkipRequested; f += Time.unscaledDeltaTime) { group.alpha = 1f - f / 0.5f; yield return null; }
            Time.timeScale = keepScale <= 0f ? 1f : keepScale; Active = false;
            yield return null; // let the key that closed the last card go before the cinematic reads input
        }

        private void OnDestroy() { if (Active) { Active = false; if (Time.timeScale == 0f) Time.timeScale = 1f; } }

        private RectTransform Full(string n)
        {
            var rt = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
        }

        private TextMeshProUGUI Text(string n, float size, Color col, bool isTitle, Vector2 min, Vector2 max)
        {
            var t = ScreenTextRenderer.MakeText(transform, n, size, col, isTitle, TextAlignmentOptions.Center);
            t.raycastTarget = false; var r = t.rectTransform; r.anchorMin = min; r.anchorMax = max; r.pivot = new Vector2(0.5f, 0.5f); r.offsetMin = r.offsetMax = Vector2.zero;
            t.textWrappingMode = TextWrappingModes.Normal; t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = size * 0.5f;
            return t;
        }
    }
}
