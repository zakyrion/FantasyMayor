using UniRx;
using UnityEngine;

public class HeightmapDataLayer
{
    private readonly ReactiveProperty<Texture> _texture = new();

    public IReadOnlyReactiveProperty<Texture> HeightmapTexture => _texture;

    public void SetTexture(Texture texture)
    {
        _texture.SetValueAndForceNotify(texture);
    }
}