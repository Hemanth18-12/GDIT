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
        public static event Action<string> OnIdolRecovered;
        public static event Action<GameState> OnGameState;

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
        }
    }
}

