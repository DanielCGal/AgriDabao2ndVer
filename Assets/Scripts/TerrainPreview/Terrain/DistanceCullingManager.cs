using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public class DistanceCullingManager : MonoBehaviour
    {
        private static DistanceCullingManager instance;

        public static bool HasInstance => instance != null;

        public static DistanceCullingManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = Object.FindFirstObjectByType<DistanceCullingManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("DistanceCullingManager");
                instance = go.AddComponent<DistanceCullingManager>();
                return instance;
            }
        }

        [Header("Culling Settings")]
        [Tooltip("Objects within this horizontal distance (metres) of the player are visible.")]
        public float visibleDistance = 60f;

        [Tooltip("Extra distance an object must exceed before it hides again, to stop flicker at the boundary.")]
        public float hysteresis = 6f;

        [Tooltip("Seconds between visibility passes. Higher = cheaper, but more pop-in latency.")]
        public float updateInterval = 0.3f;

        private readonly List<DistanceCullable> cullables = new List<DistanceCullable>();
        private Transform player;
        private float nextUpdateTime;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            visibleDistance = GameSettings.RenderDistance;
            GameSettings.Changed += OnSettingsChanged;
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= OnSettingsChanged;
            if (instance == this)
                instance = null;
        }

        private void OnSettingsChanged()
        {
            visibleDistance = GameSettings.RenderDistance;
        }

        public void Register(DistanceCullable cullable)
        {
            if (cullable != null && !cullables.Contains(cullable))
                cullables.Add(cullable);
        }

        public void Unregister(DistanceCullable cullable)
        {
            cullables.Remove(cullable);
        }

        private void Update()
        {
            if (Time.time < nextUpdateTime)
                return;

            nextUpdateTime = Time.time + updateInterval;

            if (!EnsurePlayer())
                return;

            Vector3 playerPos = player.position;

            for (int i = cullables.Count - 1; i >= 0; i--)
            {
                DistanceCullable cullable = cullables[i];
                if (cullable == null)
                {
                    cullables.RemoveAt(i);
                    continue;
                }

                Vector3 center = cullable.GetWorldCenter();
                float dx = center.x - playerPos.x;
                float dz = center.z - playerPos.z;
                float distSqr = dx * dx + dz * dz;

                float radius = cullable.Radius;
                float showThreshold = visibleDistance + radius;
                float hideThreshold = visibleDistance + hysteresis + radius;

                if (cullable.IsVisible)
                {
                    if (distSqr > hideThreshold * hideThreshold)
                        cullable.SetVisible(false);
                }
                else
                {
                    if (distSqr <= showThreshold * showThreshold)
                        cullable.SetVisible(true);
                }
            }
        }

        private bool EnsurePlayer()
        {
            if (player != null)
                return true;

            FirstPersonTerrainController controller =
                Object.FindFirstObjectByType<FirstPersonTerrainController>();

            if (controller != null)
            {
                player = controller.transform;
                return true;
            }

            if (Camera.main != null)
            {
                player = Camera.main.transform;
                return true;
            }

            return false;
        }
    }
}
