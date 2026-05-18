using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Modules.TerrainView.Views
{
    /// <summary>
    ///     Renders a thin border around a single selected hex.
    ///     Mesh and material are created at runtime and owned by this MonoBehaviour.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class HexSelectionView : DisposedMono
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private MeshFilter _meshFilter;

        [SerializeField]
        private MeshRenderer _meshRenderer;

        private Material _runtimeMaterial;

        private void Awake()
        {
            EnsureComponents();
            EnsureMaterial();
            HideSelectionMesh();
        }

        protected override void OnDestroy()
        {
            DestroyCurrentMesh();

            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }

            base.OnDestroy();
        }

        /// <summary>
        ///     Generates a border mesh from pre-computed outer and inner vertex rings and enables the renderer.
        /// </summary>
        public void ShowSelectionBorder(NativeArray<float3> outerRing, NativeArray<float3> innerRing)
        {
            EnsureComponents();
            EnsureMaterial();

            if (_meshFilter == null || _meshRenderer == null)
            {
                Debug.LogError("[HexSelectionView] Mesh components are missing.");
                return;
            }

            DestroyCurrentMesh();

            var mesh = BuildBorderMesh(outerRing, innerRing);
            _meshFilter.sharedMesh = mesh;
            _meshRenderer.enabled = true;
        }

        /// <summary>
        ///     Hides the current selection mesh and destroys its runtime mesh resource.
        /// </summary>
        public void HideSelectionMesh()
        {
            EnsureComponents();

            DestroyCurrentMesh();

            if (_meshRenderer != null)
                _meshRenderer.enabled = false;
        }

        private Mesh BuildBorderMesh(NativeArray<float3> outerRing, NativeArray<float3> innerRing)
        {
            var n = outerRing.Length;
            var m = innerRing.Length;

            var cx = 0f;
            var cz = 0f;
            for (var i = 0; i < n; i++)
            {
                cx += outerRing[i].x;
                cz += outerRing[i].z;
            }
            cx /= n;
            cz /= n;

            var outer = SortByAngle(outerRing, cx, cz);
            var inner = SortByAngle(innerRing, cx, cz);

            var vertices = new Vector3[n + m];
            var normals = new Vector3[n + m];
            var triangles = new int[(n + m) * 3];

            for (var i = 0; i < n; i++)
            {
                vertices[i] = new Vector3(outer[i].x, outer[i].y, outer[i].z);
                normals[i] = Vector3.up;
            }
            for (var i = 0; i < m; i++)
            {
                vertices[n + i] = new Vector3(inner[i].x, inner[i].y, inner[i].z);
                normals[n + i] = Vector3.up;
            }

            int oi = 0, ii = 0, oAdv = 0, iAdv = 0, ti = 0;

            while (oAdv < n || iAdv < m)
            {
                bool advanceOuter;
                if (oAdv >= n)
                    advanceOuter = false;
                else if (iAdv >= m)
                    advanceOuter = true;
                else
                    advanceOuter = (long)oAdv * m <= (long)iAdv * n;

                var oNext = (oi + 1) % n;
                var iNext = (ii + 1) % m;

                if (advanceOuter)
                {
                    triangles[ti]     = oi;
                    triangles[ti + 1] = n + ii;
                    triangles[ti + 2] = oNext;
                    oi = oNext;
                    oAdv++;
                }
                else
                {
                    triangles[ti]     = oi;
                    triangles[ti + 1] = n + ii;
                    triangles[ti + 2] = n + iNext;
                    ii = iNext;
                    iAdv++;
                }

                ti += 3;
            }

            var mesh = new Mesh { name = "HexSelectionBorder" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private float3[] SortByAngle(NativeArray<float3> ring, float cx, float cz)
        {
            var sorted = new float3[ring.Length];
            for (var i = 0; i < ring.Length; i++)
                sorted[i] = ring[i];

            Array.Sort(sorted, (a, b) =>
                Mathf.Atan2(a.z - cz, a.x - cx).CompareTo(Mathf.Atan2(b.z - cz, b.x - cx)));

            return sorted;
        }

        private void DestroyCurrentMesh()
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
                return;

            var mesh = _meshFilter.sharedMesh;
            _meshFilter.sharedMesh = null;
            Destroy(mesh);
        }

        private void EnsureComponents()
        {
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void EnsureMaterial()
        {
            if (_meshRenderer == null || _runtimeMaterial != null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogError("[HexSelectionView] Failed to find a runtime shader for selection rendering.");
                return;
            }

            _runtimeMaterial = new Material(shader)
            {
                name = "HexSelectionRuntimeMaterial"
            };

            var borderColor = new Color(1f, 0.82f, 0.18f, 1f);
            if (_runtimeMaterial.HasProperty(BaseColorId))
                _runtimeMaterial.SetColor(BaseColorId, borderColor);
            if (_runtimeMaterial.HasProperty(ColorId))
                _runtimeMaterial.SetColor(ColorId, borderColor);

            _meshRenderer.sharedMaterial = _runtimeMaterial;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            _meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }
}
