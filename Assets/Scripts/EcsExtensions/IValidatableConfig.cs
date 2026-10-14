namespace EcsExtensions
{
    // A config that checks its own authored data; ConfigLoaderSystem<T> calls Validate right after loading
    // and before the config becomes visible in EntityStorages. Throw on any authoring violation.
    public interface IValidatableConfig
    {
        void Validate();
    }
}
