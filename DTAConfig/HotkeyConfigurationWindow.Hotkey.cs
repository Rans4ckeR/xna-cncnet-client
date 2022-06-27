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
