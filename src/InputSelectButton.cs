﻿using Menu;
using RWCustom;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ImprovedInput;

sealed class InputSelectButton : SimpleButton
{
    private readonly MenuLabel keybindLabel;
    private readonly MenuLabel currentKey;
    private readonly FSprite circle;
    private readonly FSprite arrow;
    private readonly new InputOptionsMenu menu;
    private readonly bool compactMode;

    public readonly PlayerKeybind keybind;

    private Color? buttonColor;
    private float filled;
    private float lastFilled;
    private int blinkCounter;
    private int recentlyUsedGreyedOut;
    private float recentlyUsedFlash;
    private float lastRecentlyUsedFlash;
    private bool init;

    private Options.ControlSetup.Preset lastControllerType = Options.ControlSetup.Preset.None;
    Options.ControlSetup.Preset ControllerType => CustomInputExt.GetControllerType(Player);

    bool lastGamepad;
    public bool Gamepad => CustomInputExt.UsingGamepad(ControlSetup.index);

    int lastPlayer;
    int Player => menu.manager.rainWorld.options.playerToSetInputFor;

    public bool PlayerOneOnly => keybind.index == 0; // just pause button for now
    public bool MovementKey => keybind.index is 5 or 6 or 7 or 8;

    Options.ControlSetup ControlSetup => PlayerOneOnly ? menu.manager.rainWorld.options.controls[0] : menu.CurrentControlSetup;

    public InputSelectButton(MenuObject owner, PlayerKeybind keybind, bool compact, Vector2 pos) : base(owner.menu, owner, "", "", pos, new Vector2(30f, 30f))
    {
        compactMode = compact;
        menu = (InputOptionsMenu)owner.menu;
        this.keybind = keybind;

        lastGamepad = Gamepad;
        lastPlayer = Player;

        keybindLabel = new MenuLabel(menu, this, owner.menu.Translate(keybind.Name), new Vector2(compact ? -26 : -30, compact ? 8 : 0), size, false);
        keybindLabel.label.alignment = FLabelAlignment.Right;
        subObjects.Add(keybindLabel);

        currentKey = new MenuLabel(menu, this, "", new Vector2(compact ? -26 : 30f, compact ? -8 : 0), size, false, null);
        currentKey.label.alignment = compact ? FLabelAlignment.Right : FLabelAlignment.Left;
        subObjects.Add(currentKey);

        Container.AddChild(circle = new("Futile_White") {
            shader = menu.manager.rainWorld.Shaders["VectorCircleFadable"]
        });
        Container.AddChild(arrow = new("keyShiftB"));
    }

    private KeyCode CurrentlyDisplayed()
    {
        bool arrowKey = keybind.index is 5 or 6 or 7 or 8;

        return Gamepad && !arrowKey ? keybind.gamepad[ControlSetup.index] : keybind.keyboard[ControlSetup.index];
    }

    public override Color MyColor(float timeStacker)
    {
        KeyCode current = CurrentlyDisplayed();

        // Blink red if conflicting keys on current character
        if (current != KeyCode.None && (blinkCounter % 20 < 10) && CustomInputExt.KeybindsOfType(Player, current, stopAt: 2) > 1) {
            return Color.red;
        }

        float t = (blinkCounter % 4 < 2) ? 0f : Custom.SCurve(Mathf.Lerp(lastRecentlyUsedFlash, recentlyUsedFlash, timeStacker), 0.4f);

        Color color = Color.Lerp(base.MyColor(timeStacker), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), t);

        if (current != KeyCode.None && !buttonBehav.greyedOut && !PlayerOneOnly) {
            bool DuplicateKeys(int player)
            {
                Options.ControlSetup[] controls = menu.manager.rainWorld.options.controls;
                if (Player == player || controls[Player].controlPreference != controls[player].controlPreference) {
                    return false;
                }
                if (controls[player].UsingGamepad() && controls[Player].gamePadNumber != controls[player].gamePadNumber) {
                    return false;
                }
                return CustomInputExt.KeybindsOfType(player, current, 1) > 0;
            }
            // Hint at Survivor, Monk, Hunter, or Nightcat (respectively) having a duplicate key
            if (blinkCounter % 80 is < 20 && DuplicateKeys(0))              return Color.Lerp(color, new Color(1, 1, 1), 0.5f);
            if (blinkCounter % 80 is >= 20 and < 40 && DuplicateKeys(1))    return Color.Lerp(color, new Color(1, 1, 0), 0.2f);
            if (blinkCounter % 80 is >= 40 and < 60 && DuplicateKeys(2))    return Color.Lerp(color, new Color(1, 0, 0), 0.15f);
            if (blinkCounter % 80 is >= 60 && DuplicateKeys(3))             return Color.Lerp(color, new Color(0, 0, .5f), 0.2f);
        }

        return color;
    }

    public void InputAssigned(KeyCode keyCode)
    {
        string keyCodeString = keyCode.ToString();
        if (keyCodeString.Length > 4 && keyCodeString.Substring(0, 5) == "Mouse") {
            menu.PlaySound(SoundID.MENU_Error_Ping);
        }
        else if ((keyCodeString.Length > 7 && keyCodeString.Substring(0, 8) == "Joystick") != Gamepad) {
            menu.PlaySound(SoundID.MENU_Error_Ping);
        }
        else {
            if ((keyCode == KeyCode.Escape || keyCode == CurrentlyDisplayed()) && keybind.index != 0) {
                menu.PlaySound(SoundID.MENU_Checkbox_Uncheck);

                keyCode = KeyCode.None;
            }
            else {
                menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);
            }

            if (Gamepad)
                keybind.gamepad[Player] = keyCode;
            else
                keybind.keyboard[Player] = keyCode;
        }

        Flash();
    }

    public void Flash()
    {
        RefreshKeyDisplay();
        recentlyUsedGreyedOut = 60;
        recentlyUsedFlash = 1f;
    }

    public override void Update()
    {
        if (!init) {
            init = true;
            RefreshKeyDisplay();
        }

        base.Update();

        blinkCounter++;
        lastFilled = filled;

        if (menu.settingInput is IntVector2 value && value.y == keybind.index) {
            filled = Custom.LerpAndTick(filled, 1f, 0.05f, 0.05f);
            if (blinkCounter % 30 < 15) {
                currentKey.text = "?";
            }
            else {
                RefreshKeyDisplay();
            }
        }
        else {
            filled = Custom.LerpAndTick(filled, 0f, 0.05f, 0.05f);
        }

        if (recentlyUsedGreyedOut --> 0) {
            if (Selected) {
                buttonBehav.greyedOut = true;
            }
            if (recentlyUsedGreyedOut == 0 || menu.selectedObject != null && menu.selectedObject != this) {
                recentlyUsedGreyedOut = 0;
                buttonBehav.greyedOut = false;
            }
        }
        else if (PlayerOneOnly) {
            buttonBehav.greyedOut = menu.CurrentControlSetup.index != 0;
        }
        else if (MovementKey) {
            buttonBehav.greyedOut = Gamepad;
        }

        if (!MovementKey && lastGamepad != Gamepad
            || !MovementKey && Gamepad && lastControllerType != ControllerType
            || !PlayerOneOnly && lastPlayer != Player && !(MovementKey && Gamepad)) {
            RefreshKeyDisplay();
        }

        lastControllerType = ControllerType;
        lastPlayer = Player;
        lastGamepad = Gamepad;
        lastRecentlyUsedFlash = recentlyUsedFlash;
        recentlyUsedFlash = Mathf.Max(0f, recentlyUsedFlash - 0.025f);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        float a = currentKey.label.alpha;
        if (currentKey.label.text != "?" && buttonColor.HasValue) {
            currentKey.label.color = Color.Lerp(MyColor(timeStacker), buttonColor.Value, 0.5f);
        }
        else if (currentKey.label.text == "None") {
            currentKey.label.color = MyColor(timeStacker) * 0.65f;
        }
        else {
            currentKey.label.color = MyColor(timeStacker);
        }
        currentKey.label.alpha = a;

        circle.x = DrawX(timeStacker) + size.x / 2f;
        circle.y = DrawY(timeStacker) + size.y / 2f;
        circle.scale = Mathf.Lerp(0.5f, 0.875f, Mathf.Pow(Custom.SCurve(Mathf.Lerp(lastFilled, filled, timeStacker), 0.3f), 1.4f));
        circle.color = new Color(0f, 1f, Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastFilled, filled, timeStacker)), 0.5f));
        arrow.x = DrawX(timeStacker) + size.x / 2f + (compactMode ? -26 - arrow.width / 2f : 30 + arrow.width / 2f);
        arrow.y = DrawY(timeStacker) + size.y / 2f + (compactMode ? -10 : 0);
        arrow.scale = 1f;

        a = arrow.alpha;
        arrow.color = MyColor(timeStacker);
        arrow.alpha = a;
    }

    public override void RemoveSprites()
    {
        base.RemoveSprites();
        circle.RemoveFromContainer();
        arrow.RemoveFromContainer();
    }

    public void RefreshKeyDisplay()
    {
        recentlyUsedFlash = Mathf.Max(recentlyUsedFlash, 0.65f);

        bool notGreyed = !(MovementKey && Gamepad || PlayerOneOnly && menu.CurrentControlSetup.index != 0);
        if (notGreyed && Gamepad && CustomInputExt.GetControllerType(ControlSetup.index) == Options.ControlSetup.Preset.None) {
            arrow.alpha = 0;
            currentKey.label.alpha = 1;
            currentKey.text = "< N / A >";
        }
        else {
            string text = CustomInputExt.ButtonText(Player, CurrentlyDisplayed(), out buttonColor);
            if (text.EndsWith("Arrow")) {
                currentKey.label.alpha = 0;
                arrow.alpha = 1;
                arrow.rotation = text switch {
                    "LArrow" => -90,
                    "RArrow" => 90,
                    "DownArrow" => 180,
                    _ => 0
                };
            }
            else {
                arrow.alpha = 0;
                currentKey.label.alpha = 1;
                currentKey.text = text;
            }
        }
    }

    public override void Clicked()
    {
        menu.mouseModeBeforeAssigningInput = menu.manager.menuesMouseMode;
        menu.selectedObject = this;
        menu.settingInput = new(0, keybind.index);
        menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
    }

    public string HoverText()
    {
        if (recentlyUsedGreyedOut < 1 && PlayerOneOnly && menu.CurrentControlSetup.index != 0) {
            return menu.Translate("Only available for player 1");
        }
        if (recentlyUsedGreyedOut < 1 && MovementKey && Gamepad) {
            return menu.Translate("Only available for keyboard");
        }
        if (currentKey.text == "< N / A >") {
            return menu.Translate("Connect a controller to bind this button");
        }
        string mod = keybind.Mod == "Vanilla" ? "" : $" ({keybind.Mod})";
        string desc = string.IsNullOrWhiteSpace(keybind.Description) ? "" : $"\n{keybind.Description}";
        return Regex.Replace(menu.Translate("Bind <X> button"), "<X>", $"< {keybindLabel.text} >") + mod + desc;
    }
}
