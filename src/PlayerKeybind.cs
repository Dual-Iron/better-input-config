using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace ImprovedInput;

/// <summary>
/// A simple per-player keybind.
/// </summary>
public sealed class PlayerKeybind
{
    internal static List<PlayerKeybind> GuiKeybinds()
    {
        List<PlayerKeybind> ret = new(keybinds);
        ret.RemoveAll(p => p.HideConfig);
        return ret;
    }

    /// <summary>Every keybind currently registered, including vanilla and modded keybinds.</summary>
    public static IReadOnlyList<PlayerKeybind> Keybinds() => keybindsReadonly;

    internal static readonly List<PlayerKeybind> keybinds = new();
    internal static readonly ReadOnlyCollection<PlayerKeybind> keybindsReadonly = new(keybinds);

    // Don't move these. The indices matter.

    /// <summary>The PAUSE button. Usually ignored for anyone but the first player.</summary>
    public static readonly PlayerKeybind Pause = Register("vanilla:pause", "Vanilla", "Pause", KeyCode.Escape, KeyCode.JoystickButton9, KeyCode.JoystickButton6);

    /// <summary>The MAP button.</summary>
    public static readonly PlayerKeybind Map = Register("vanilla:map", "Vanilla", "Map", KeyCode.Space, KeyCode.JoystickButton5, KeyCode.JoystickButton5);
    /// <summary>The GRAB button.</summary>
    public static readonly PlayerKeybind Grab = Register("vanilla:grab", "Vanilla", "Grab", KeyCode.LeftShift, KeyCode.JoystickButton0, KeyCode.JoystickButton2);
    /// <summary>The JUMP button.</summary>
    public static readonly PlayerKeybind Jump = Register("vanilla:jump", "Vanilla", "Jump", KeyCode.Z, KeyCode.JoystickButton1, KeyCode.JoystickButton0);
    /// <summary>The THROW button.</summary>
    public static readonly PlayerKeybind Throw = Register("vanilla:throw", "Vanilla", "Throw", KeyCode.X, KeyCode.JoystickButton2, KeyCode.JoystickButton1);

    /// <summary>The UP button. Ignored for controllers.</summary>
    public static readonly PlayerKeybind Up = Register("vanilla:up", "Vanilla", "Up", KeyCode.UpArrow, KeyCode.None);
    /// <summary>The LEFT button. Ignored for controllers.</summary>
    public static readonly PlayerKeybind Left = Register("vanilla:left", "Vanilla", "Left", KeyCode.LeftArrow, KeyCode.None);
    /// <summary>The DOWN button. Ignored for controllers.</summary>
    public static readonly PlayerKeybind Down = Register("vanilla:down", "Vanilla", "Down", KeyCode.DownArrow, KeyCode.None);
    /// <summary>The RIGHT button. Ignored for controllers.</summary>
    public static readonly PlayerKeybind Right = Register("vanilla:right", "Vanilla", "Right", KeyCode.RightArrow, KeyCode.None);

    /// <summary>
    /// Registers a new keybind.
    /// </summary>
    /// <param name="id">The unique ID for the keybind.</param>
    /// <param name="mod">The display name of the mod that registered this keybind.</param>
    /// <param name="name">A short name to show in the Input Settings screen.</param>
    /// <param name="keyboardPreset">The default value for keyboards.</param>
    /// <param name="gamepadPreset">The default value for controllers.</param>
    /// <returns>A new <see cref="PlayerKeybind"/> to be used like <c>player.JustPressed(keybind)</c>.</returns>
    /// <exception cref="ArgumentException">The <paramref name="id"/> is invalid or already taken.</exception>
    public static PlayerKeybind Register(string id, string mod, string name, KeyCode keyboardPreset, KeyCode gamepadPreset)
    {
        return Register(id, mod, name, keyboardPreset, gamepadPreset, gamepadPreset);
    }

    /// <summary>
    /// Registers a new keybind.
    /// </summary>
    /// <param name="id">The unique ID for the keybind.</param>
    /// <param name="mod">The display name of the mod that registered this keybind.</param>
    /// <param name="name">A short name to show in the Input Settings screen.</param>
    /// <param name="keyboardPreset">The default value for keyboards.</param>
    /// <param name="gamepadPreset">The default value for PlayStation, Switch Pro, and other controllers.</param>
    /// <param name="xboxPreset">The default value for Xbox controllers.</param>
    /// <returns>A new <see cref="PlayerKeybind"/> to be used like <c>player.JustPressed(keybind)</c>.</returns>
    /// <exception cref="ArgumentException">The <paramref name="id"/> is invalid or already taken.</exception>
    public static PlayerKeybind Register(string id, string mod, string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        Validate(id, mod, name);
        keybinds.Add(new(id, mod, name, keyboardPreset, gamepadPreset, xboxPreset == KeyCode.None ? gamepadPreset : xboxPreset) {
            index = keybinds.Count
        });
        return keybinds.Last();
    }

    private static void Validate(string id, string mod, string name)
    {
        ArgumentException e = null;
        if (string.IsNullOrWhiteSpace(id) || id.Contains("<optA>") || id.Contains("<optB>")) {
            e = new ArgumentException($"The keybind id \"{id}\" is invalid.");
        }
        else if (string.IsNullOrWhiteSpace(mod)) {
            e = new ArgumentException($"The keybind mod \"{mod}\" is invalid.");
        }
        else if (string.IsNullOrWhiteSpace(name)) {
            e = new ArgumentException($"The keybind mod \"{name}\" is invalid.");
        }
        else if (keybinds.Any(k => k.Id == id)) {
            e = new ArgumentException($"A keybind with the id {id} has already been registered.");
        }
        if (e != null) {
            Debug.Log($"[ERROR] {e.Message}");
            throw e;
        }
    }

    /// <summary>
    /// Gets a keybind given its <paramref name="id"/>.
    /// </summary>
    /// <returns>The keybind, or <see langword="null"/> if none was found.</returns>
    public static PlayerKeybind Get(string id) => keybinds.FirstOrDefault(k => k.Id == id);

    internal PlayerKeybind(string id, string mod, string name, KeyCode keyboardDefault, KeyCode gamepadDefault, KeyCode xboxDefault)
    {
        Id = id;
        Mod = mod;
        Name = name;
        KeyboardPreset = keyboardDefault;
        GamepadPreset = gamepadDefault;
        XboxPreset = xboxDefault;

        keyboard = new KeyCode[CustomInputExt.maxMaxPlayers];
        gamepad = new KeyCode[CustomInputExt.maxMaxPlayers];

        for (int i = 0; i < CustomInputExt.maxMaxPlayers; i++) {
            keyboard[i] = keyboardDefault;
            gamepad[i] = gamepadDefault;
        }
    }

    internal int index = -1;

    /// <summary>A unique ID.</summary>
    public string Id { get; }
    /// <summary>The display name of the mod that registered this keybind.</summary>
    public string Mod { get; }
    /// <summary>The display name of the keybind.</summary>
    public string Name { get; }
    /// <summary>The default value for keyboards.</summary>
    public KeyCode KeyboardPreset { get; }
    /// <summary>The default value for PlayStation, Switch Pro, and other controllers.</summary>
    public KeyCode GamepadPreset { get; }
    /// <summary>The default value for Xbox controllers.</summary>
    public KeyCode XboxPreset { get; }

    /// <summary>A longer description to show at the bottom of the screen when configuring the keybind.</summary>
    public string Description { get; set; }

    /// <summary>If true, using the map suppresses the keybind.</summary>
    public bool MapSuppressed { get; set; } = true;
    /// <summary>If true, sleeping suppresses the keybind.</summary>
    public bool SleepSuppressed { get; set; } = true;
    /// <summary>If true, the keybind will not be configurable through the Input Settings screen.</summary>
    public bool HideConfig { get; set; } = false;
    /// <summary>If true, the conflict warning will be hidden when this key conflicts with the given key.</summary>
    /// <remarks>May be null.</remarks>
    public Func<PlayerKeybind, bool> HideConflict { get; set; }

    internal readonly KeyCode[] keyboard;
    internal readonly KeyCode[] gamepad;

    /// <summary>True if the binding for <paramref name="playerNumber"/> is set.</summary>
    public bool Bound(int playerNumber) => CurrentBinding(playerNumber) != KeyCode.None;
    /// <summary>True if the binding for <paramref name="playerNumber"/> is not set.</summary>
    public bool Unbound(int playerNumber) => CurrentBinding(playerNumber) == KeyCode.None;

    /// <summary>The current keycode configured for the given <paramref name="playerNumber"/> on keyboard.</summary>
    public KeyCode Keyboard(int playerNumber)
    {
        if (playerNumber < 0 || playerNumber >= CustomInputExt.MaxPlayers) {
            throw new ArgumentOutOfRangeException(nameof(playerNumber));
        }
        if (this == Pause) playerNumber = 0;
        return keyboard[playerNumber];
    }

    /// <summary>The current keycode configured for the given <paramref name="playerNumber"/> on a controller.</summary>
    public KeyCode Gamepad(int playerNumber)
    {
        if (playerNumber < 0 || playerNumber >= CustomInputExt.MaxPlayers) {
            throw new ArgumentOutOfRangeException(nameof(playerNumber));
        }
        if (this == Pause) playerNumber = 0;
        return gamepad[playerNumber];
    }

    /// <summary>The current recognized keycode for the given <paramref name="playerNumber"/>.</summary>
    public KeyCode CurrentBinding(int playerNumber)
    {
        if (playerNumber < 0 || playerNumber >= CustomInputExt.MaxPlayers) {
            throw new ArgumentOutOfRangeException(nameof(playerNumber));
        }
        if (this == Pause) playerNumber = 0;
        if (CustomInputExt.UsingGamepad(playerNumber)) {
            return gamepad[playerNumber];
        }
        return keyboard[playerNumber];
    }

    /// <summary>
    /// Checks if <see langword="this"/> for <paramref name="playerNumber"/> conflicts with <paramref name="other"/> for <paramref name="otherPlayerNumber"/>. This ignores <see cref="HideConflict"/>.
    /// </summary>
    public bool ConflictsWith(int playerNumber, PlayerKeybind other, int otherPlayerNumber)
    {
        if (playerNumber == otherPlayerNumber && this == other) {
            return false;
        }
        Options.ControlSetup[] controls = RWCustom.Custom.rainWorld.options.controls;
        if (controls[playerNumber].controlPreference != controls[otherPlayerNumber].controlPreference) {
            return false;
        }
        if (controls[playerNumber].UsingGamepad() && controls[otherPlayerNumber].UsingGamepad() && controls[playerNumber].gamePadNumber != controls[otherPlayerNumber].gamePadNumber) {
            return false;
        }
        if (CurrentBinding(playerNumber) == KeyCode.None) {
            return false;
        }
        return CurrentBinding(playerNumber) == other.CurrentBinding(otherPlayerNumber);
    }

    internal bool VisiblyConflictsWith(int playerNumber, PlayerKeybind other, int otherPlayerNumber)
    {
        return ConflictsWith(playerNumber, other, otherPlayerNumber) && !(HideConflict?.Invoke(other) ?? false) && !(other.HideConflict?.Invoke(this) ?? false);
    }

    /// <summary>Checks if the key is currently being pressed by <paramref name="playerNumber"/>.</summary>
    public bool CheckRawPressed(int playerNumber)
    {
        // More or less copypasted from RWInput.PlayerInputPC
        if (playerNumber < 0 || playerNumber >= CustomInputExt.MaxPlayers) {
            throw new ArgumentOutOfRangeException(nameof(playerNumber));
        }
        if (this == Pause) playerNumber = 0;

        var rw = RWCustom.Custom.rainWorld;
        var controller = RWInput.PlayerRecentController(playerNumber, rw);
        rw.options.controls[playerNumber].UpdateActiveController(controller, false);
        var controllerType = rw.options.controls[playerNumber].GetActivePreset();

        bool notMultiplayer = rw.processManager == null || !rw.processManager.IsGameInMultiplayerContext();
        if (!notMultiplayer && controllerType == Options.ControlSetup.Preset.None) {
            return false;
        }

        if (notMultiplayer && controllerType == Options.ControlSetup.Preset.None) {
            controllerType = Options.ControlSetup.Preset.KeyboardSinglePlayer;
        }

        bool gamePad = controllerType != Options.ControlSetup.Preset.KeyboardSinglePlayer;
        if (!gamePad) {
            return Input.GetKey(keyboard[playerNumber]);
        }

        return CustomInputExt.ResolveButtonDown(gamepad[playerNumber], controller, controllerType);
    }

    /// <summary>
    /// Returns <see cref="Id"/>.
    /// </summary>
    public override string ToString() => Id;
}
