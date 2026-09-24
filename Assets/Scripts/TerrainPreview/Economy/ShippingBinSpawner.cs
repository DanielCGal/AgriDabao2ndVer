using UnityEngine;

namespace AgriDabao3D
{
    public class ShippingBinSpawner : MonoBehaviour
    {
        [Header("References")]
        public GameObject shippingBinPrefab;
        public TemporaryTerrainGenerator terrainGenerator;
        public Transform player;

        [Header("Spawn Settings")]
        public float forwardDistance = 10f;
        public float sideOffset = 4f;
        public float extraHeight = 0.3f;

        private GameObject spawnedBin;

        public GameObject SpawnedBin => spawnedBin;

        private void Start()
        {
            if (!FarmLoadContext.IsRestoring)
            {
                Invoke(nameof(SpawnBinNearPlayer), 0.8f);
            }
        }

        public void SpawnBinNearPlayerNow()
        {
            CancelInvoke(nameof(SpawnBinNearPlayer));
            SpawnBinNearPlayer();
        }

        public GameObject RestoreBin(
            Vector3 savedPosition,
            Quaternion savedRotation)
        {
            CancelInvoke(nameof(SpawnBinNearPlayer));

            if (shippingBinPrefab == null)
            {
                Debug.LogWarning(
                    "ShippingBinSpawner: Shipping-bin prefab is not assigned."
                );

                return null;
            }

            if (spawnedBin != null)
            {
                Destroy(spawnedBin);
            }

            spawnedBin = Instantiate(
                shippingBinPrefab,
                savedPosition,
                savedRotation
            );

            spawnedBin.name = "ShippingBin_Runtime";

            if (spawnedBin.GetComponent<ShippingBinSeller>() == null)
            {
                spawnedBin.AddComponent<ShippingBinSeller>();
            }

            DistanceCullable.Attach(spawnedBin);

            return spawnedBin;
        }

        private void SpawnBinNearPlayer()
        {
            if (spawnedBin != null)
                return;

            ResolveReferences();

            if (shippingBinPrefab == null ||
                terrainGenerator == null ||
                terrainGenerator.targetTerrain == null ||
                player == null)
            {
                Debug.LogWarning(
                    "ShippingBinSpawner: Missing prefab, terrain, or player."
                );

                return;
            }

            Terrain terrain = terrainGenerator.targetTerrain;

            Vector3 flatForward = player.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.forward;

            flatForward.Normalize();

            Vector3 flatRight = player.right;
            flatRight.y = 0f;

            if (flatRight.sqrMagnitude < 0.001f)
                flatRight = Vector3.right;

            flatRight.Normalize();

            Vector3 candidate =
                player.position +
                flatForward * forwardDistance +
                flatRight * sideOffset;

            float terrainY =
                terrain.SampleHeight(candidate) +
                terrain.transform.position.y;

            candidate.y = terrainY + extraHeight;

            spawnedBin = Instantiate(
                shippingBinPrefab,
                candidate,
                shippingBinPrefab.transform.rotation
            );

            spawnedBin.name = "ShippingBin_Runtime";

            if (spawnedBin.GetComponent<ShippingBinSeller>() == null)
            {
                spawnedBin.AddComponent<ShippingBinSeller>();
            }

            DistanceCullable.Attach(spawnedBin);

            Vector3 toPlayer =
                player.position - spawnedBin.transform.position;

            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion yawOnly = Quaternion.LookRotation(
                    toPlayer.normalized,
                    Vector3.up
                );

                Vector3 euler = yawOnly.eulerAngles;

                spawnedBin.transform.rotation = Quaternion.Euler(
                    shippingBinPrefab.transform.eulerAngles.x,
                    euler.y,
                    shippingBinPrefab.transform.eulerAngles.z
                );
            }
        }

        private void ResolveReferences()
        {
            if (terrainGenerator == null)
            {
                terrainGenerator =
                    Object.FindFirstObjectByType
                    <TemporaryTerrainGenerator>();
            }

            if (player == null)
            {
                FirstPersonTerrainController controller =
                    Object.FindFirstObjectByType
                    <FirstPersonTerrainController>();

                if (controller != null)
                    player = controller.transform;
            }
        }
    }
}
