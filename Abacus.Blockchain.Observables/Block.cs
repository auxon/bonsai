using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Bonsai.Observables.Blockchain
{
    public static class BlockHelper
    {
        public static int ParseMagicNumber<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            return Convert.ToInt32(enumValue);
        }
    }
    public enum Bitcoin: uint
    {
        BSV = 0x0B110907,
        BTC = 0xD9B4BEF9,
        BCH = 0xE3E1F3E8,
    }

    [Serializable]
    public class Block<T> : IBlock<T>, IObservable<T>
    {
        public uint MagicNumber => GetMagicNumberForType();
        public long Height;
        public long Difficulty;
        public string Hash;
        public string PreviousBlockHash;
        public double Nonce;
        public DateTime Timestamp;
        public T Data;
        public Version Version => System.Version.Parse("0.01");

        public Block()
        {
            if (!typeof(T).IsEnum)
            {
                throw new InvalidOperationException("T must be an enum type.");
            }
        }
    private uint GetMagicNumberForType()
    {
        if (typeof(T) != typeof(Bitcoin))
        {
            throw new InvalidOperationException("T is not a supported type for determining the magic number.");
        }

        var chainType = (Bitcoin)(object)default(T);
        return chainType switch
        {
            Bitcoin.BSV => (uint)Bitcoin.BSV,
            Bitcoin.BTC => (uint)Bitcoin.BTC,
            Bitcoin.BCH => (uint)Bitcoin.BCH,
            _ => throw new NotSupportedException($"Unsupported Bitcoin chain type: {chainType}")
        };
    }

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
