using System;
using System.Collections.Generic;
using System.Text;

namespace Bonsai.Observables.Blockchain
{
    public interface IBlock<T>
    {
        long GetHeight();
        string GetHash();
        T SetData(T input);
        T GetData();
    }
}
