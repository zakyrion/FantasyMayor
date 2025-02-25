using UniRx;

public interface IGeneratorPagesDataLayer : IPageDataLayer<GeneratorPages>
{
    void SetCurrentPage(GeneratorPages page);
}

public class GeneratorPagesDataLayer : IGeneratorPagesDataLayer
{
    private readonly ReactiveProperty<GeneratorPages> _currentPage = new(GeneratorPages.TestGenerator);
    public IReadOnlyReactiveProperty<GeneratorPages> CurrentPage => _currentPage;

    public void SetCurrentPage(GeneratorPages page)
    {
        _currentPage.Value = page;
    }
}