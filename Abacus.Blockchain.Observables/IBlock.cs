using System;
using System.Collections.Generic;
using System.Text;

namespace Abacus.Observables.Blockchain
{
    public interface IBlock<T>
    {
        long GetHeight();
        string GetHash();
        T SetData(T input);
        T GetData();
    }
}
