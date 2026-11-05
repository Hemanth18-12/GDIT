# Legacy of Temples

A third-person 3D action-investigation game set across ancient Indian heritage temples, developed in **Unity 6 (6000.6.8f1)** using the **Universal Render Pipeline (URP)**.

---

## 🛠️ Project Information

- **Engine Version**: Unity 6 (`6000.6.8f1`)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Scripting Language**: C# (.NET Standard 2.1 / C# 9.0+)
- **Target Platforms**: PC / Mac Standalone / WebGL

---

## 👥 3-Person Team Folder Ownership

To prevent merge conflicts and maintain clean isolation, work is partitioned across distinct folders and assemblies:

| Member | Domain | Assigned Folders & Assets | Assembly Definition |
| :--- | :--- | :--- | :--- |
| **Member 1** | **World & Environment** | `Assets/_Project/Scripts/World/`<br>`Assets/_Project/Scenes/Environments/`<br>`Assets/_Project/Models/Environment/` | `LOT.World` |
| **Member 2** | **Characters & AI** | `Assets/_Project/Scripts/Characters/`<br>`Assets/_Project/Models/Characters/`<br>`Assets/_Project/Animations/` | `LOT.Characters` |
| **Member 3** | **Gameplay, UI & Audio** | `Assets/_Project/Scripts/Gameplay/`<br>`Assets/_Project/Scripts/UI/`<br>`Assets/_Project/Audio/`<br>`Assets/_Project/Prefabs/`<br>`Assets/_Project/Scenes/Dev/` | `LOT.Gameplay`<br>`LOT.UI` |
| **Shared** | **Core Systems** | `Assets/_Project/Scripts/Core/` | `LOT.Core` |

---

## 🌿 Branching & Collaboration Rules

1. **Never commit directly to `main`**: All features must be developed on separate member branches (`member1-world`, `member2-characters`, `member3-gameplay`) and merged via Pull Requests.
2. **Always commit `.meta` files**: Every file and folder inside `Assets/` must have its matching `.meta` file staged and committed. Never delete or ignore `.meta` files.
3. **Never edit another member's folder or scene**: Cross-system interaction must occur exclusively through `LOT.Core` events (`EventBus`) and contracts (`IInteractable`).
4. **Use Visible Meta Files and Force Text serialization**: Verify `ProjectSettings/EditorSettings.asset` and `ProjectSettings/VersionControlSettings.asset` maintain text serialization.

---

## 📜 Core Contract (`LegacyOfTemples.Core`)

Assembly: `LOT.Core` (No dependencies; referenced by `LOT.World`, `LOT.Characters`, `LOT.Gameplay`, `LOT.UI`).

### 1. `GameState`
```csharp
namespace LegacyOfTemples.Core
{
    public enum GameState
    {
        Menu,
        Intro,
        Playing,
        Puzzle,
        Chase,
        Ending
    }
}
```

### 2. `IInteractable`
```csharp
using UnityEngine;

namespace LegacyOfTemples.Core
{
    public interface IInteractable
    {
        string Id { get; }
        string Prompt { get; }
        Transform Anchor { get; }
        float Radius { get; }
        void Interact();
    }
}
```

### 3. `EventBus`
```csharp
namespace LegacyOfTemples.Core
{
    public static class EventBus
    {
        // Actions
        public static event Action<string> OnSceneChanged;
        public static event Action<IInteractable> OnPlayerInteract;
        public static event Action<string> OnClueFound;
        public static event Action<string> OnPuzzleSolved;
        public static event Action<string> OnDialogueStart;
        public static event Action<string> OnDialogueEnd;
        public static event Action<string> OnIdolRecovered;
        public static event Action<GameState> OnGameState;

        // Safe Raise Methods
        public static void RaiseSceneChanged(string sceneName);
        public static void RaisePlayerInteract(IInteractable interactable);
        public static void RaiseClueFound(string clueId);
        public static void RaisePuzzleSolved(string puzzleId);
        public static void RaiseDialogueStart(string dialogueId);
        public static void RaiseDialogueEnd(string dialogueId);
        public static void RaiseIdolRecovered(string idolId);
        public static void RaiseGameState(GameState state);
    }
}
```

### 4. `Constants`
```csharp
namespace LegacyOfTemples.Core
{
    public static class Constants
    {
        // Scene IDs
        public const string Courtyard = "Courtyard";
        public const string Sanctum = "Sanctum";
        public const string PuzzleWall = "PuzzleWall";
        public const string ShrineCrime = "ShrineCrime";
        public const string JunglePath = "JunglePath";

        // Character IDs
        public const string Investigator = "investigator";
        public const string Robber1 = "robber1";
        public const string Robber2 = "robber2";
        public const string Priest = "priest";
        public const string Villager = "villager";
    }
}
```

---

## 🚀 Getting Started

1. Clone repository:
   ```bash
   git clone https://github.com/Hemanth18-12/GDIT.git
   ```
2. Open **Unity Hub**.
3. Click **Add** -> **Add project from disk** -> Select the cloned repository root folder.
4. Ensure Unity Editor **6000.6.8f1** is selected and open the project.

---

## 🎨 Art & Asset Credits

Special credit to the **Heritage Mystery Project** (`assets.blend`, `S20240010236JagannathTemple.blend`, and associated source assets) for the authentic 3D character models and temple artifacts:
- **Investigator**: Gumshoe Noir Detective with trench coat, hat, and glasses (`noir-detective`).
- **Villager (Meera)**: Indian woman dressed in an authentic red and green saree with gold borders (`indian-woman-in-saree`).
- **Priest (Ananda)**: Elderly Hindu temple priest with silk angavastram and rudraksha beads (`elderly-hindu-priest`).
- **Robber 1**: Corrupted King in black armor with bone crown (`corrupted-king-in-black-armor-with-bone-crown`).
- **Robber 2**: Corrupted Archangel boss with six wings (`corrupted-archangel-six-wings-boss`).
- **Temple Deities & Environment**: Sacred Lord Jagannath idol and Odisha-style stone temple mandapa structures.
