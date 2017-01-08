using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alguin.Utilities
{
    /// <summary>
    /// Class for string extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Get the number from string format *_number
        /// </summary>
        /// <param name="text">string</param>
        /// <returns>number from string</returns>
        public static int LastNumber(this string text)
        {
            var parts = text.Split('_');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid format: " + text);

            int result;
            try
            {
                result = int.Parse(parts[1]);
            }
            catch
            {
                throw new ArgumentException("Invalid number format: " + parts[1]);
            }
            return result;
        }
    }
}
