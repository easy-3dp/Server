using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal static class Hex
    {
        public static byte[] Decode(string str)
        {
            byte[] bytes = new byte[str.Length>>1];
            for (int i = 0; i < str.Length; i += 2)
            {
                bytes[i>>1] = byte.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return bytes;
        }

        public static void Decode(string str, Span<byte> bytes)
        {
            for (int i = 0; i < str.Length; i += 2)
            {
                bytes[i >> 1] = byte.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
            }
        }

        public static string Encode(Span<byte> bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i ++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
