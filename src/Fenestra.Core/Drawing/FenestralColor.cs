using System.Globalization;

namespace Fenestra.Core.Drawing
{
    /// <summary>
    /// Platform-agnostic RGB color representation with hex string support.
    /// </summary>
    public readonly struct FenestralColor : IEquatable<FenestralColor>
    {
        /// <summary>
        /// Gets the red channel value (0-255).
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets the green channel value (0-255).
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets the blue channel value (0-255).
        /// </summary>
        public byte B { get; }

        /// <summary>
        /// Gets the color as a hex string in #RRGGBB format.
        /// </summary>
        public string Hex => string.Format("#{0:X2}{1:X2}{2:X2}", R, G, B);

        /// <summary>
        /// Initializes a new color from individual red, green, and blue channel values.
        /// </summary>
        public FenestralColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        private FenestralColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Hex color cannot be null or empty.", nameof(hex));

            var normalized = NormalizeHex(hex);

            R = byte.Parse(normalized.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            G = byte.Parse(normalized.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            B = byte.Parse(normalized.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates a color from individual red, green, and blue channel values.
        /// </summary>
        public static FenestralColor FromRgb(byte r, byte g, byte b)
        {
            return new FenestralColor(r, g, b);
        }

        /// <summary>
        /// Creates a color from a hex string (e.g. "#FF0000" or "F00").
        /// </summary>
        public static FenestralColor FromHex(string hex)
        {
            return new FenestralColor(hex);
        }

        public void Deconstruct(out byte r, out byte g, out byte b)
        {
            r = R;
            g = G;
            b = B;
        }

        public override string ToString()
        {
            return Hex;
        }

        public bool Equals(FenestralColor other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object? obj)
        {
            if (obj is FenestralColor)
                return Equals((FenestralColor)obj);

            return false;
        }

        public override int GetHashCode()
        {
            return (R << 16) | (G << 8) | B;
        }

        public static bool operator ==(FenestralColor left, FenestralColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FenestralColor left, FenestralColor right)
        {
            return !left.Equals(right);
        }

        public static implicit operator FenestralColor(string hex)
        {
            return new FenestralColor(hex);
        }

        private static string NormalizeHex(string hex)
        {
            var value = hex.Trim();

            if (!value.StartsWith("#"))
                value = "#" + value;

            if (value.Length == 4)
            {
                value = string.Format("#{0}{0}{1}{1}{2}{2}",
                    value[1],
                    value[2],
                    value[3]);
            }

            if (value.Length != 7)
                throw new FormatException("Hex color must be in the format #RGB or #RRGGBB.");

            for (int i = 1; i < value.Length; i++)
            {
                if (!Uri.IsHexDigit(value[i]))
                    throw new FormatException("Hex color contains invalid characters.");
            }

            return value.ToUpperInvariant();
        }
    }
}