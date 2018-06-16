using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Abacus.Extensions
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
            var json = JsonConvert.SerializeObject(o, Formatting.Indented, new JsonSerializerSettings { MaxDepth = 2 });

            var bytes = from c in json.ToCharArray()
                        from bs in BitConverter.GetBytes(c)
                        select bs;

            return bytes;
        }

        public static T Deserialize<T>(this byte[] bytes)
        {
            T val;

            using (var tr = new JsonTextReader(new StringReader(bytes.BytesToHex())))
            {
                var serializer = new JsonSerializer();

                val = (T)serializer.Deserialize(tr);
            }
            
            return val;
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
