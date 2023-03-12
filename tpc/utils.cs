using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpc
{
    // Utility functions.
    public static class utils
    {

        // Whether the character is alphabetic.
        public static bool isAlpha(char ch)
        {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
        }

        // Whether the character is a digit.
        public static bool isDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        // Whether the character is a valid first character of an identifier.
        public static bool isIdentifierStart(char ch)
        {
            return isAlpha(ch) || ch == '_';
        }

        // Whether the character is a valid subsequent (non-first) character of an identifier.
        public static bool isIdentifierPart(char ch)
        {
            return isIdentifierStart(ch) || isDigit(ch);
        }

        // Whether the character is whitespace.
        public static bool isWhitespace(char ch)
        {
            return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
        }

        // Format number or string to width characters, left-aligned.
        public static string leftAlign(string value, int width)
        {
            // Convert to string.
            value = "" + value;

            // Pad to width.
            while (value.Length < width)
            {
                value = value + " ";
            }

            return value;
        }

        // Format number or string to width characters, right-aligned.
        public static string rightAlign(string value, int width)
        {
            // Convert to string.
            value = "" + value;

            // Pad to width.
            while (value.Length < width)
            {
                value = " " + value;
            }

            return value;
        }

        // Truncate toward zero.
        public static decimal trunc(int value)
        {
            if (value < 0)
            {
                return Math.Ceiling((decimal)value);
            }
            else
            {
                return Math.Floor((decimal)value);
            }
        }

        // Repeat a string "count" times.
        public static string repeatString(string s, int count)
        {
            var result = "";

            // We go through each bit of "count", adding a string of the right length
            // to "result" if the bit is 1.
            while (true)
            {
                if ((count & 1) != 0)
                {
                    result += s;
                }

                // Move to the next bit.
                count >>= 1;
                if (count == 0)
                {
                    // Exit here before needlessly doubling the size of "s".
                    break;
                }

                // Double the length of "s" to correspond to the value of the shifted bit.
                s += s;
            }

            return result;
        }

        // Log an object written out in human-readable JSON. This can't handle
        // circular structures.
        public static void logAsJson(Object obj)
        {
            // console.log(JSON.stringify(obj, null, 2));
            Console.WriteLine(obj.ToString());
        }


    }
}
