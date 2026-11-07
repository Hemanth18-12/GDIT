using System;

namespace LegacyOfTemples.Core
{
    public static class EventBus
    {
        public static event Action<string> OnSceneChanged;
        public static event Action<IInteractable> OnPlayerInteract;
        public static event Action<string> OnClueFound;
        public static event Action<string> OnPuzzleSolved;
        public static event Action<string> OnDialogueStart;
        public static event Action<string> OnDialogueEnd;
        public static event Action<string> OnDialogueLine;
        /// <summary>Frame on which an overlay consumed the Escape key (so Pause does not also open).</summary>
        public static int EscapeConsumedFrame = -1;
        /// <summary>Asks the dialogue UI to close without running completion callbacks.</summary>
        public static event Action OnRequestDialogueEnd;
        public static void RaiseRequestDialogueEnd() { OnRequestDialogueEnd?.Invoke(); }
        public static void RaiseDialogueLine(string speakerName) { OnDialogueLine?.Invoke(speakerName); }
        public static event Action<string> OnIdolRecovered;
        public static event Action<GameState> OnGameState;
        public static event Action<string, string[], string[], Action<int>, Action> OnRequestDialogue;
        public static event Action<string, string> OnShowPrompt;
        public static event Action OnHidePrompt;
        public static event Action<string, string, string> OnUpdateObjective;
        public static event Action<bool> OnShowcaseTourToggled;
        public static event Action<bool> OnOverlayMenuToggled;

        public static bool IsPhotoModeActive { get; set; }
        public static bool IsShowcaseActive { get; set; }

        public static void RaiseSceneChanged(string sceneName)
        {
            OnSceneChanged?.Invoke(sceneName);
        }

        public static void RaisePlayerInteract(IInteractable interactable)
        {
            OnPlayerInteract?.Invoke(interactable);
        }

        public static void RaiseClueFound(string clueId)
        {
            OnClueFound?.Invoke(clueId);
        }

        public static void RaisePuzzleSolved(string puzzleId)
        {
            OnPuzzleSolved?.Invoke(puzzleId);
        }

        public static void RaiseDialogueStart(string dialogueId)
        {
            OnDialogueStart?.Invoke(dialogueId);
        }

        public static void RaiseDialogueEnd(string dialogueId)
        {
            OnDialogueEnd?.Invoke(dialogueId);
        }

        public static void RaiseIdolRecovered(string idolId)
        {
            OnIdolRecovered?.Invoke(idolId);
        }

        public static void RaiseGameState(GameState state)
        {
            OnGameState?.Invoke(state);
        }

        public static void RaiseRequestDialogue(string speaker, string[] lines, string[] choices = null, Action<int> onChoice = null, Action onComplete = null)
        {
            OnRequestDialogue?.Invoke(speaker, lines, choices, onChoice, onComplete);
        }

        public static void RaiseShowPrompt(string key, string promptText)
        {
            OnShowPrompt?.Invoke(key, promptText);
        }

        public static void RaiseHidePrompt()
        {
            OnHidePrompt?.Invoke();
        }

        public static void RaiseUpdateObjective(string title, string body, string counter = null)
        {
            OnUpdateObjective?.Invoke(title, body, counter);
        }

        public static void RaiseShowcaseTourToggled(bool active)
        {
            OnShowcaseTourToggled?.Invoke(active);
        }

        public static void RaiseOverlayMenuToggled(bool active)
        {
            OnOverlayMenuToggled?.Invoke(active);
        }

        /// <summary>
        /// Clears all event subscribers. Useful when reloading domain or tearing down tests.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnSceneChanged = null;
            OnPlayerInteract = null;
            OnClueFound = null;
            OnPuzzleSolved = null;
            OnDialogueStart = null;
            OnDialogueEnd = null;
            OnIdolRecovered = null;
            OnGameState = null;
            OnRequestDialogue = null;
            OnShowPrompt = null;
            OnHidePrompt = null;
            OnUpdateObjective = null;
            OnShowcaseTourToggled = null;
            OnOverlayMenuToggled = null;
            IsPhotoModeActive = false;
            IsShowcaseActive = false;
        }
    }
}

