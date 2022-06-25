using System;
using Microsoft.Xna.Framework.Input;

namespace DTAConfig;

public partial class HotkeyConfigurationWindow
{
    /// <summary>
    /// Represents a keyboard key with modifiers.
    /// </summary>
    private struct Hotkey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hotkey" /> struct. Creates a new hotkey by
        /// decoding a Tiberian Sun / Red Alert 2 encoded key value.
        /// </summary>
        /// <param name="encodedKeyValue">The encoded key value.</param>
        public Hotkey(int encodedKeyValue)
        {
            Key = (Keys)(encodedKeyValue & 255);
            Modifier = (KeyModifiers)(encodedKeyValue >> 8);
        }

        public Hotkey(Keys key, KeyModifiers modifiers)
        {
            Key = key;
            Modifier = modifiers;
        }

        public Keys Key { get; private set; }

        public KeyModifiers Modifier { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is not Hotkey)
                return false;

            Hotkey hotkey = (Hotkey)obj;
            return hotkey.Key == Key && hotkey.Modifier == Modifier;
        }

        public override int GetHashCode()
        {
            return GetTSEncoded();
        }

        /// <summary>
        /// Returns the hotkey in the Tiberian Sun / Red Alert 2 Keyboard.ini encoded format.
        /// </summary>
        public int GetTSEncoded()
        {
            return ((int)Modifier << 8) + (int)Key;
        }

        public override string ToString()
        {
            if (Key == Keys.None && Modifier == KeyModifiers.None)
                return string.Empty;

            return GetString();
        }

        public string ToStringWithNone()
        {
            if (Key == Keys.None && Modifier == KeyModifiers.None)
                return "None";

            return GetString();
        }

        /// <summary>
        /// Returns the display string for an XNA key. Allows overriding specific key enum names to
        /// be more suitable for the UI.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>A string.</returns>
        private static string GetKeyDisplayString(Keys key)
        {
            return key switch
            {
                Keys.D0 => "0",
                Keys.D1 => "1",
                Keys.D2 => "2",
                Keys.D3 => "3",
                Keys.D4 => "4",
                Keys.D5 => "5",
                Keys.D6 => "6",
                Keys.D7 => "7",
                Keys.D8 => "8",
                Keys.D9 => "9",
                Keys.None => throw new NotImplementedException(),
                Keys.Back => throw new NotImplementedException(),
                Keys.Tab => throw new NotImplementedException(),
                Keys.Enter => throw new NotImplementedException(),
                Keys.CapsLock => throw new NotImplementedException(),
                Keys.Escape => throw new NotImplementedException(),
                Keys.Space => throw new NotImplementedException(),
                Keys.PageUp => throw new NotImplementedException(),
                Keys.PageDown => throw new NotImplementedException(),
                Keys.End => throw new NotImplementedException(),
                Keys.Home => throw new NotImplementedException(),
                Keys.Left => throw new NotImplementedException(),
                Keys.Up => throw new NotImplementedException(),
                Keys.Right => throw new NotImplementedException(),
                Keys.Down => throw new NotImplementedException(),
                Keys.Select => throw new NotImplementedException(),
                Keys.Print => throw new NotImplementedException(),
                Keys.Execute => throw new NotImplementedException(),
                Keys.PrintScreen => throw new NotImplementedException(),
                Keys.Insert => throw new NotImplementedException(),
                Keys.Delete => throw new NotImplementedException(),
                Keys.Help => throw new NotImplementedException(),
                Keys.A => throw new NotImplementedException(),
                Keys.B => throw new NotImplementedException(),
                Keys.C => throw new NotImplementedException(),
                Keys.D => throw new NotImplementedException(),
                Keys.E => throw new NotImplementedException(),
                Keys.F => throw new NotImplementedException(),
                Keys.G => throw new NotImplementedException(),
                Keys.H => throw new NotImplementedException(),
                Keys.I => throw new NotImplementedException(),
                Keys.J => throw new NotImplementedException(),
                Keys.K => throw new NotImplementedException(),
                Keys.L => throw new NotImplementedException(),
                Keys.M => throw new NotImplementedException(),
                Keys.N => throw new NotImplementedException(),
                Keys.O => throw new NotImplementedException(),
                Keys.P => throw new NotImplementedException(),
                Keys.Q => throw new NotImplementedException(),
                Keys.R => throw new NotImplementedException(),
                Keys.S => throw new NotImplementedException(),
                Keys.T => throw new NotImplementedException(),
                Keys.U => throw new NotImplementedException(),
                Keys.V => throw new NotImplementedException(),
                Keys.W => throw new NotImplementedException(),
                Keys.X => throw new NotImplementedException(),
                Keys.Y => throw new NotImplementedException(),
                Keys.Z => throw new NotImplementedException(),
                Keys.LeftWindows => throw new NotImplementedException(),
                Keys.RightWindows => throw new NotImplementedException(),
                Keys.Apps => throw new NotImplementedException(),
                Keys.Sleep => throw new NotImplementedException(),
                Keys.NumPad0 => throw new NotImplementedException(),
                Keys.NumPad1 => throw new NotImplementedException(),
                Keys.NumPad2 => throw new NotImplementedException(),
                Keys.NumPad3 => throw new NotImplementedException(),
                Keys.NumPad4 => throw new NotImplementedException(),
                Keys.NumPad5 => throw new NotImplementedException(),
                Keys.NumPad6 => throw new NotImplementedException(),
                Keys.NumPad7 => throw new NotImplementedException(),
                Keys.NumPad8 => throw new NotImplementedException(),
                Keys.NumPad9 => throw new NotImplementedException(),
                Keys.Multiply => throw new NotImplementedException(),
                Keys.Add => throw new NotImplementedException(),
                Keys.Separator => throw new NotImplementedException(),
                Keys.Subtract => throw new NotImplementedException(),
                Keys.Decimal => throw new NotImplementedException(),
                Keys.Divide => throw new NotImplementedException(),
                Keys.F1 => throw new NotImplementedException(),
                Keys.F2 => throw new NotImplementedException(),
                Keys.F3 => throw new NotImplementedException(),
                Keys.F4 => throw new NotImplementedException(),
                Keys.F5 => throw new NotImplementedException(),
                Keys.F6 => throw new NotImplementedException(),
                Keys.F7 => throw new NotImplementedException(),
                Keys.F8 => throw new NotImplementedException(),
                Keys.F9 => throw new NotImplementedException(),
                Keys.F10 => throw new NotImplementedException(),
                Keys.F11 => throw new NotImplementedException(),
                Keys.F12 => throw new NotImplementedException(),
                Keys.F13 => throw new NotImplementedException(),
                Keys.F14 => throw new NotImplementedException(),
                Keys.F15 => throw new NotImplementedException(),
                Keys.F16 => throw new NotImplementedException(),
                Keys.F17 => throw new NotImplementedException(),
                Keys.F18 => throw new NotImplementedException(),
                Keys.F19 => throw new NotImplementedException(),
                Keys.F20 => throw new NotImplementedException(),
                Keys.F21 => throw new NotImplementedException(),
                Keys.F22 => throw new NotImplementedException(),
                Keys.F23 => throw new NotImplementedException(),
                Keys.F24 => throw new NotImplementedException(),
                Keys.NumLock => throw new NotImplementedException(),
                Keys.Scroll => throw new NotImplementedException(),
                Keys.LeftShift => throw new NotImplementedException(),
                Keys.RightShift => throw new NotImplementedException(),
                Keys.LeftControl => throw new NotImplementedException(),
                Keys.RightControl => throw new NotImplementedException(),
                Keys.LeftAlt => throw new NotImplementedException(),
                Keys.RightAlt => throw new NotImplementedException(),
                Keys.BrowserBack => throw new NotImplementedException(),
                Keys.BrowserForward => throw new NotImplementedException(),
                Keys.BrowserRefresh => throw new NotImplementedException(),
                Keys.BrowserStop => throw new NotImplementedException(),
                Keys.BrowserSearch => throw new NotImplementedException(),
                Keys.BrowserFavorites => throw new NotImplementedException(),
                Keys.BrowserHome => throw new NotImplementedException(),
                Keys.VolumeMute => throw new NotImplementedException(),
                Keys.VolumeDown => throw new NotImplementedException(),
                Keys.VolumeUp => throw new NotImplementedException(),
                Keys.MediaNextTrack => throw new NotImplementedException(),
                Keys.MediaPreviousTrack => throw new NotImplementedException(),
                Keys.MediaStop => throw new NotImplementedException(),
                Keys.MediaPlayPause => throw new NotImplementedException(),
                Keys.LaunchMail => throw new NotImplementedException(),
                Keys.SelectMedia => throw new NotImplementedException(),
                Keys.LaunchApplication1 => throw new NotImplementedException(),
                Keys.LaunchApplication2 => throw new NotImplementedException(),
                Keys.OemSemicolon => throw new NotImplementedException(),
                Keys.OemPlus => throw new NotImplementedException(),
                Keys.OemComma => throw new NotImplementedException(),
                Keys.OemMinus => throw new NotImplementedException(),
                Keys.OemPeriod => throw new NotImplementedException(),
                Keys.OemQuestion => throw new NotImplementedException(),
                Keys.OemTilde => throw new NotImplementedException(),
                Keys.OemOpenBrackets => throw new NotImplementedException(),
                Keys.OemPipe => throw new NotImplementedException(),
                Keys.OemCloseBrackets => throw new NotImplementedException(),
                Keys.OemQuotes => throw new NotImplementedException(),
                Keys.Oem8 => throw new NotImplementedException(),
                Keys.OemBackslash => throw new NotImplementedException(),
                Keys.ProcessKey => throw new NotImplementedException(),
                Keys.Attn => throw new NotImplementedException(),
                Keys.Crsel => throw new NotImplementedException(),
                Keys.Exsel => throw new NotImplementedException(),
                Keys.EraseEof => throw new NotImplementedException(),
                Keys.Play => throw new NotImplementedException(),
                Keys.Zoom => throw new NotImplementedException(),
                Keys.Pa1 => throw new NotImplementedException(),
                Keys.OemClear => throw new NotImplementedException(),
                Keys.ChatPadGreen => throw new NotImplementedException(),
                Keys.ChatPadOrange => throw new NotImplementedException(),
                Keys.Pause => throw new NotImplementedException(),
                Keys.ImeConvert => throw new NotImplementedException(),
                Keys.ImeNoConvert => throw new NotImplementedException(),
                Keys.Kana => throw new NotImplementedException(),
                Keys.Kanji => throw new NotImplementedException(),
                Keys.OemAuto => throw new NotImplementedException(),
                Keys.OemCopy => throw new NotImplementedException(),
                Keys.OemEnlW => throw new NotImplementedException(),
                _ => key.ToString(),
            };
        }

        /// <summary>
        /// Creates the display string for this key.
        /// </summary>
        private string GetString()
        {
            string str = string.Empty;

            if (Modifier.HasFlag(KeyModifiers.Shift))
                str += "SHIFT+";

            if (Modifier.HasFlag(KeyModifiers.Ctrl))
                str += "CTRL+";

            if (Modifier.HasFlag(KeyModifiers.Alt))
                str += "ALT+";

            if (Key == Keys.None)
                return str;

            return str + Hotkey.GetKeyDisplayString(Key);
        }
    }
}