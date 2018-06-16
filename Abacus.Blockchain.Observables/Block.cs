using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Abacus.Observables.Blockchain
{
    [Serializable]
    public struct Block<T> : IBlock<T>
    {
        public int MagicNumber => Configuration.MAGIC_NUMBER;
        public long Height;
        public long Difficulty;
        public string Hash;
        public string PreviousBlockHash;
        public double Nonce;
        public DateTime Timestamp;
        public T Data;
        public Version Version => System.Version.Parse("0.01");

        public long GetHeight() => this.Height;
        public string GetHash() => this.Hash;
        public T GetData() => this.Data;
        public T SetData(T input) => this.Data = input;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"##################################################################################################");
            sb.AppendLine($@"#  ABACUS BLOCK v{Version.ToString(2)} | Magic:  {MagicNumber}");
            sb.AppendLine($@"#  Height:  {Height} | Timestamp:  {Timestamp} | Difficulty:  {Difficulty}");
            sb.AppendLine($@"#  Hash:  {Hash}  | Nonce:  {Nonce}");
            sb.AppendLine($@"#  Prev:  {PreviousBlockHash}");
            sb.AppendLine($@"#  Data:  {Data?.ToString()}");
            sb.AppendLine($@"##################################################################################################");

            return sb.ToString();
        }

    }
}
