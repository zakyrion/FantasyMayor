using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class HightmapUI : MonoBehaviour
{
    [SerializeField] private RawImage _heightmapImage;
    [Inject] private HeightmapDataLayer _heightmapDataLayer;

    private void Awake()
    {
        _heightmapDataLayer.HeightmapTexture.Subscribe(HeightmapChanged);

        _heightmapImage.gameObject.SetActive(false);
    }

    private void HeightmapChanged(Texture texture)
    {
        _heightmapImage.gameObject.SetActive(true);
        _heightmapImage.texture = texture;
    }
}