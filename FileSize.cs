using System;
using System.Collections.Generic;

namespace tempnuker
{
    public struct FileSize : IFormattable
    {
        private readonly ulong _value;

        private const int DEFAULT_PRECISION = 2;

        private static readonly IList<string> Units;

        static FileSize()
        {
            Units = new List<string>(){
            "B", "KB", "MB", "GB", "TB", "EB"
            };
        }

        public FileSize(ulong value)
        {
            _value = value;
        }

        public static explicit operator FileSize(ulong value)
        {
            return new FileSize(value);
        }

        override public string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (String.IsNullOrEmpty(format))
                return ToString(DEFAULT_PRECISION);
            else if (int.TryParse(format, out int precision))
                return ToString(precision);
            else
                return _value.ToString(format, formatProvider);
        }

        /// <summary>
        /// Formats the FileSize using the given number of decimals.
        /// </summary>
        public string ToString(int precision)
        {
            double pow = Math.Floor((_value > 0 ? Math.Log(_value) : 0) / Math.Log(1024));
            pow = Math.Min(pow, Units.Count - 1);
            double value = (double)_value / Math.Pow(1024, pow);
            return value.ToString(pow == 0 ? "F0" : "F" + precision.ToString()) + " " + Units[(int)pow];
        }
    }
}