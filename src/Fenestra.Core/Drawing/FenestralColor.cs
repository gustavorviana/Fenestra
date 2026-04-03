using System.Globalization;

namespace Fenestra.Core.Drawing
{
    public readonly struct FenestralColor : IEquatable<FenestralColor>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public string Hex => string.Format("#{0:X2}{1:X2}{2:X2}", R, G, B);

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

        public static FenestralColor FromRgb(byte r, byte g, byte b)
        {
            return new FenestralColor(r, g, b);
        }

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