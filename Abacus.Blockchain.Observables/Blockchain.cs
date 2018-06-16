using System;

namespace Abacus.Observables.Blockchain
{
    public class Blockchain<T> : IObservable<Block<T>>
    {
        public IDisposable Subscribe(IObserver<Block<T>> observer)
        {
            throw new NotImplementedException();
        }
    }
}
