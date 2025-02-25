using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

[RequireComponent(typeof(UIDocument))]
public abstract class DocViewBase<IDataLayer, IModel, UIType> : DisposedMono
    where UIType : Enum where IDataLayer : IPageDataLayer<UIType>
{
    [SerializeField] protected UIDocument UiDocument;
    protected IDataLayer DataLayer;
    protected IModel Model;
    protected abstract UIType UiType { get; }

    protected void Awake()
    {
        AddDisposable(DataLayer.CurrentPage.Subscribe(data => gameObject.SetActive(data.Equals(UiType))));
    }

    [Inject]
    private void Construct(IDataLayer dataLayer, IModel model)
    {
        DataLayer = dataLayer;
        Model = model;
    }
}

[RequireComponent(typeof(UIDocument))]
public abstract class DocViewBase<IDataLayer, UIType> : DisposedMono
    where UIType : Enum where IDataLayer : IPageDataLayer<UIType>
{
    [SerializeField] protected UIDocument UiDocument;
    protected IDataLayer DataLayer;
    protected abstract UIType UiType { get; }

    protected void Awake()
    {
        AddDisposable(DataLayer.CurrentPage.Subscribe(data => gameObject.SetActive(data.Equals(UiType))));
    }

    [Inject]
    private void Construct(IDataLayer dataLayer)
    {
        DataLayer = dataLayer;
    }
}