using System;

namespace DTAConfig.OptionPanels;

internal partial class DisplayOptionsPanel
{
    /// <summary>
    /// A single screen resolution.
    /// </summary>
    private sealed class ScreenResolution : IComparable<ScreenResolution>
    {
        public ScreenResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets or sets the width of the resolution in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the resolution in pixels.
        /// </summary>
        public int Height { get; set; }

        public override string ToString()
        {
            return Width + "x" + Height;
        }

        public int CompareTo(ScreenResolution res2)
        {
            if (Width < res2.Width)
            {
                return -1;
            }
            else if (Width > res2.Width)
            {
                return 1;
            }

            // equal
            if (Height < res2.Height)
                return -1;
            else if (Height > res2.Height)
                return 1;
            else
                return 0;

        }

        public override bool Equals(object obj)
        {
            if (obj is not ScreenResolution resolution)
                return false;

            return CompareTo(resolution) == 0;
        }

        public override int GetHashCode()
        {
            return new { Width, Height }.GetHashCode();
        }
    }
}