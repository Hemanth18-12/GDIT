using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LegacyOfTemples.Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public event Action OnInteract;
        public event Action OnInvestigate;
        public event Action OnToggleFlashlight;
        public event Action OnToggleInventory;
        public event Action OnToggleJournal;
        public event Action OnToggleMap;
        public event Action OnToggleHelp;
        public event Action OnTogglePause;
        public event Action OnJump;
        public event Action OnCrouch;

        [Header("State")]
        public Vector2 MoveVector { get; private set; }
        public Vector2 LookVector { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }

        // Customizable Keybindings Dictionary
        private readonly Dictionary<string, Key> keyBindings = new Dictionary<string, Key>()
        {
            { "MoveForward", Key.W },
            { "MoveBackward", Key.S },
            { "MoveLeft", Key.A },
            { "MoveRight", Key.D },
            { "Run", Key.LeftShift },
            { "Jump", Key.Space },
            { "Crouch", Key.LeftCtrl },
            { "CrouchAlt", Key.C },
            { "Interact", Key.E },
            { "Investigate", Key.F },
            { "Flashlight", Key.G },
            { "Inventory", Key.I },
            { "Journal", Key.J },
            { "Map", Key.M },
            { "Help", Key.H },
            { "Pause", Key.Escape }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBindings();
        }

        private void Update()
        {
            UpdateInputVectors();
            HandleActions();
        }

        private void UpdateInputVectors()
        {
            Keyboard kb = Keyboard.current;
            Gamepad gp = Gamepad.current;
            Mouse mouse = Mouse.current;

            Vector2 move = Vector2.zero;

            if (kb != null)
            {
                if (IsKeyDown(kb, "MoveForward")) move.y += 1f;
                if (IsKeyDown(kb, "MoveBackward")) move.y -= 1f;
                if (IsKeyDown(kb, "MoveLeft")) move.x -= 1f;
                if (IsKeyDown(kb, "MoveRight")) move.x += 1f;

                IsRunning = IsKeyDown(kb, "Run");
            }

            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.05f)
                {
                    move = stick;
                }
                if (gp.rightTrigger.isPressed || gp.leftStickButton.isPressed)
                {
                    IsRunning = true;
                }
            }

            MoveVector = Vector2.ClampMagnitude(move, 1f);

            Vector2 look = Vector2.zero;
            if (mouse != null)
            {
                look = mouse.delta.ReadValue() * 0.1f;
            }
            if (gp != null)
            {
                Vector2 rStick = gp.rightStick.ReadValue();
                if (rStick.sqrMagnitude > 0.05f)
                {
                    look = rStick * 2.5f;
                }
            }
            LookVector = look;
        }

        private void HandleActions()
        {
            Keyboard kb = Keyboard.current;
            Gamepad gp = Gamepad.current;

            if (kb == null && gp == null) return;

            // Jump
            if ((kb != null && IsKeyPressed(kb, "Jump")) || (gp != null && gp.buttonSouth.wasPressedThisFrame))
            {
                OnJump?.Invoke();
            }

            // Crouch
            if ((kb != null && (IsKeyPressed(kb, "Crouch") || IsKeyPressed(kb, "CrouchAlt"))) || (gp != null && gp.buttonEast.wasPressedThisFrame))
            {
                IsCrouching = !IsCrouching;
                OnCrouch?.Invoke();
            }

            // Interact (E)
            if ((kb != null && IsKeyPressed(kb, "Interact")) || (gp != null && gp.buttonWest.wasPressedThisFrame))
            {
                OnInteract?.Invoke();
            }

            // Investigate (F)
            if ((kb != null && IsKeyPressed(kb, "Investigate")) || (gp != null && gp.leftShoulder.wasPressedThisFrame))
            {
                OnInvestigate?.Invoke();
            }

            // Flashlight (G)
            if ((kb != null && IsKeyPressed(kb, "Flashlight")) || (gp != null && gp.dpad.up.wasPressedThisFrame))
            {
                OnToggleFlashlight?.Invoke();
            }

            // Inventory (I)
            if ((kb != null && IsKeyPressed(kb, "Inventory")) || (gp != null && gp.selectButton.wasPressedThisFrame))
            {
                OnToggleInventory?.Invoke();
            }

            // Journal (J)
            if ((kb != null && IsKeyPressed(kb, "Journal")) || (gp != null && gp.dpad.right.wasPressedThisFrame))
            {
                OnToggleJournal?.Invoke();
            }

            // Map (M)
            if ((kb != null && IsKeyPressed(kb, "Map")) || (gp != null && gp.dpad.down.wasPressedThisFrame))
            {
                OnToggleMap?.Invoke();
            }

            // Help (H)
            if ((kb != null && IsKeyPressed(kb, "Help")) || (gp != null && gp.dpad.left.wasPressedThisFrame))
            {
                OnToggleHelp?.Invoke();
            }

            // Pause (Esc)
            if ((kb != null && IsKeyPressed(kb, "Pause")) || (gp != null && gp.startButton.wasPressedThisFrame))
            {
                OnTogglePause?.Invoke();
            }
        }

        private bool IsKeyDown(Keyboard kb, string action)
        {
            if (keyBindings.TryGetValue(action, out Key key))
            {
                return kb[key].isPressed;
            }
            return false;
        }

        private bool IsKeyPressed(Keyboard kb, string action)
        {
            if (keyBindings.TryGetValue(action, out Key key))
            {
                return kb[key].wasPressedThisFrame;
            }
            return false;
        }

        public void Rebind(string action, Key newKey)
        {
            if (keyBindings.ContainsKey(action))
            {
                keyBindings[action] = newKey;
                PlayerPrefs.SetInt($"KeyBind_{action}", (int)newKey);
                PlayerPrefs.Save();
                Debug.Log($"[InputManager] Rebound {action} to {newKey}");
            }
        }

        public Key GetBinding(string action)
        {
            return keyBindings.TryGetValue(action, out Key k) ? k : Key.None;
        }

        public string GetBindingDisplayString(string action)
        {
            if (keyBindings.TryGetValue(action, out Key k))
            {
                return k.ToString();
            }
            return "Unassigned";
        }

        private void LoadBindings()
        {
            List<string> keys = new List<string>(keyBindings.Keys);
            foreach (var act in keys)
            {
                if (PlayerPrefs.HasKey($"KeyBind_{act}"))
                {
                    keyBindings[act] = (Key)PlayerPrefs.GetInt($"KeyBind_{act}");
                }
            }
        }
    }
}
