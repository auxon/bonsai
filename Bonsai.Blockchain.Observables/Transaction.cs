using System;

namespace Bonsai.Observables.Blockchain
{
    public interface ITransaction<TId, TData>
    {
        TId GetTransactionId();
        TData SetData(TData input);
        TData GetData();
    }

    public class Transaction<TId, TData> : ITransaction<TId, TData>
    {
        public TId TransactionId { get; set; }
        public TData Data { get; set; }

        public TId GetTransactionId() => this.TransactionId;
        public TData GetData() => this.Data;
        public TData SetData(TData input) => this.Data = input;
    }
}
