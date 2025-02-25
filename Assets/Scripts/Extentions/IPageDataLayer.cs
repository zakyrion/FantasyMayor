using System;
using UniRx;

public interface IPageDataLayer<Page> where Page : Enum
{
    IReadOnlyReactiveProperty<Page> CurrentPage { get; }
}