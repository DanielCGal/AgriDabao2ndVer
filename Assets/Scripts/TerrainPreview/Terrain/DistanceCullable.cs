using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public class DistanceCullable : MonoBehaviour
    {
        public static DistanceCullable Attach(GameObject go)
        {
            if (go == null)
                return null;

            DistanceCullable existing = go.GetComponent<DistanceCullable>();
            return existing != null ? existing : go.AddComponent<DistanceCullable>();
        }

        public bool IsVisible { get; private set; } = true;
        public float Radius { get; private set; } = 1f;

        [Tooltip("When far, also disable colliders (extra physics savings). Off keeps all interaction/area logic intact.")]
        public bool alsoCullColliders = false;

        private Renderer[] renderers;
        private Collider[] colliders;
        private Vector3 centerOffset;
        private bool cached;
        private bool registered;

        private void Start()
        {
            EnsureCached();
            RegisterWithManager();
        }

        private void OnEnable()
        {
            if (cached)
                RegisterWithManager();
        }

        private void OnDisable()
        {
            UnregisterFromManager();
        }

        private void OnDestroy()
        {
            UnregisterFromManager();
        }

        public Vector3 GetWorldCenter()
        {
            return transform.position + centerOffset;
        }

        public void SetVisible(bool visible)
        {
            if (IsVisible == visible)
                return;

            IsVisible = visible;

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].enabled = visible;
                }
            }

            if (alsoCullColliders && colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        colliders[i].enabled = visible;
                }
            }
        }

        public void Recache()
        {
            cached = false;
            EnsureCached();
        }

        private void EnsureCached()
        {
            if (cached)
                return;

            NeutralizeBillboardLOD();

            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            ComputeBounds();

            cached = true;
        }

        private void NeutralizeBillboardLOD()
        {
            TreeBillboardLOD[] billboards = GetComponentsInChildren<TreeBillboardLOD>(true);

            for (int i = 0; i < billboards.Length; i++)
            {
                TreeBillboardLOD billboard = billboards[i];
                if (billboard == null)
                    continue;

                if (billboard.modelRoot != null)
                    billboard.modelRoot.SetActive(true);

                billboard.enabled = false;
            }
        }

        private void ComputeBounds()
        {
            centerOffset = Vector3.zero;
            Radius = 1f;

            if (renderers == null || renderers.Length == 0)
                return;

            bool hasBounds = false;
            Bounds combined = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];

                if (r == null || !r.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    combined = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds)
                return;

            centerOffset = combined.center - transform.position;
            Radius = combined.extents.magnitude;
        }

        private void RegisterWithManager()
        {
            if (registered)
                return;

            DistanceCullingManager.Instance.Register(this);
            registered = true;
        }

        private void UnregisterFromManager()
        {
            if (!registered)
                return;

            if (DistanceCullingManager.HasInstance)
                DistanceCullingManager.Instance.Unregister(this);

            registered = false;
        }
    }
}
