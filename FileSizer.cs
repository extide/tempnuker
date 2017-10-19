using System;
using System.Collections.Generic;
using System.Text;

namespace tempnuker
{
    static class FileSizer
    {
        private const int PRECISION = 2;

        private static IList<string> Units;

        static FileSizer()
        {
            Units = new List<string>(){
                "B", "KB", "MB", "GB", "TB", "EB"
            };
        }

        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToBytes(this long bytes)
        {
            return ToBytes((double)bytes);
        }

        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToBytes(this ulong bytes)
        {
            return ToBytes((double)bytes);
        }

        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToBytes(this int bytes)
        {
            return ToBytes((double)bytes);
        }

        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToBytes(this uint bytes)
        {
            return ToBytes((double)bytes);
        }

        private static string ToBytes(double bytes)
        {
            double pow = Math.Floor((bytes > 0 ? Math.Log(bytes) : 0) / Math.Log(1024));
            pow = Math.Min(pow, Units.Count - 1);
            double value = bytes / Math.Pow(1024, pow);
            return value.ToString(pow == 0 ? "F0" : "F" + PRECISION.ToString()) + " " + Units[(int)pow];
        }
    }
}
