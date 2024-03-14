using System;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace Bonsai.Observables
{
    public class CustomObservable<T> { }
public class CustomObservableProvider : IQbservableProvider
{
    public IQbservable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new CustomObservable<TElement>(expression);
    }
}
}
