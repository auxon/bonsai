using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO;

namespace Bonsai.Extensions
{
    public static class ByteAndHexExtensions
    {
        //TODO:  Validate string input, and deal with upper and lowercase variants.
        //TODO:  Optimize.
        public static byte[] HexToBytes(this string input)
        {
            input = input.Replace("-", "");
            var outputLength = input.Length / 2;
            var output = new byte[outputLength];
            for (var i = 0; i < outputLength; i++)
                output[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
            return output;
        }

        public static string BytesToHex(this byte[] input)
        {
            return BitConverter.ToString(input).Replace("-", "");
        }

        public static IEnumerable<byte> Serialize(this object o)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, MaxDepth = 2 };
            var json = JsonSerializer.Serialize(o, options);

            var bytes = Encoding.UTF8.GetBytes(json);
            return bytes;

        }

        public static T Deserialize<T>(this byte[] bytes)
        {
            return bytes.Deserialize<T>();
        }
    }

    public static class StringExtensions
    {
        public static string Repeat(this string input, long number)
        {
            var sb = new StringBuilder(input);
            for (int i = 0; i < number - 1; i++)
            {
                sb.Append(input);
            }
            return sb.ToString();
        }
    }
}
