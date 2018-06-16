using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Abacus.Extensions;

namespace Abacus.Cryptography
{
    /// <summary>
    /// Represents a SHA256 hash as a hex string or byte[] hash, or tuple of hex string and byte[] hash.
    /// </summary>
    class Sha256 : Tuple<byte[], string>
    {
        private readonly byte[] hash;
        private readonly string hexHash;
        private readonly string original;

        public byte[] Hash => this.hash;
        public string HexHash => this.hexHash;
        public string Original => this.original;

        public Sha256(byte[] bytes, string input) : base(bytes, input)
        {
            this.hexHash = (this.hash = bytes = HashHash(this.original = input)).ToHex();
        }

        public static byte[] HashHash(string input)
        {
            var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.ASCII.GetBytes(input);
            return sha256.ComputeHash(sha256.ComputeHash(bytes));
        }

        public override string ToString()
        {
            return this.original;
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(this.hash.ToHex(), 16);
        }
    }
}
