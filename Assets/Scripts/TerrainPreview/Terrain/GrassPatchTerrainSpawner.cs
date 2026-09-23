using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public class GrassPatchTerrainSpawner : MonoBehaviour
    {
        [Header("References")]
        public TemporaryTerrainGenerator terrainGenerator;
        public Transform player;

        [Header("Grass Prefabs")]
        public GameObject grassPatchSmallPrefab;
        public GameObject grassPatchLargePrefab;

        [Header("Spawn Settings")]
        [Tooltip("How many grass patches are scattered over the whole terrain.\n\n" +
                 "Patches land anywhere on the 700x700m map, so this number is far less " +
                 "generous than it sounds: at the default 30m render distance only about " +
                 "two are in view at once, and about ten at the 60m maximum. Cutting it " +
                 "therefore saves very little while thinning an already sparse world - " +
                 "the per-patch triangle count is the far bigger lever, and that is " +
                 "handled by the mesh decimation importer instead.")]
        public int grassPatchCount = 350;
        public float edgePadding = 12f;
        public float minScale = 0.75f;
        public float maxScale = 1.45f;
        public float heightOffset = 0.02f;
        public int randomSeed = 9125;

        [Header("Performance / Mobile Culling")]
        public float visibleDistance = 75f;
        public float updateInterval = 0.35f;
        public bool disableCollidersOnGrass = true;

        private readonly List<GameObject> spawnedGrass = new List<GameObject>();
        private Terrain terrain;
        private float nextUpdateTime;
        private bool initialized;

        public float grassSizeMultiplier = 2.5f;

        private void Start()
        {
            visibleDistance = GameSettings.RenderDistance;
            GameSettings.Changed += OnSettingsChanged;
            StartCoroutine(WaitForTerrainThenSpawn());
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= OnSettingsChanged;
        }

        private void OnSettingsChanged()
        {
            visibleDistance = GameSettings.RenderDistance;
        }

        private IEnumerator WaitForTerrainThenSpawn()
        {
            while (terrain == null)
            {
                if (terrainGenerator == null)
                    terrainGenerator = Object.FindFirstObjectByType<TemporaryTerrainGenerator>();

                if (terrainGenerator != null && terrainGenerator.targetTerrain != null)
                    terrain = terrainGenerator.targetTerrain;

                if (terrain == null)
                    terrain = Object.FindFirstObjectByType<Terrain>();

                yield return null;
            }

            if (player == null)
            {
                FirstPersonTerrainController controller = Object.FindFirstObjectByType<FirstPersonTerrainController>();
                if (controller != null)
                    player = controller.transform;
            }

            if (grassPatchSmallPrefab == null && grassPatchLargePrefab == null)
            {
                Debug.LogWarning("GrassPatchTerrainSpawner: Assign at least one grass patch prefab.");
                yield break;
            }

            SpawnGrassPatches();
            UpdateGrassVisibility();

            initialized = true;

            Debug.Log("GrassPatchTerrainSpawner: Spawned grass patches = " + spawnedGrass.Count);
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (Time.time < nextUpdateTime)
                return;

            nextUpdateTime = Time.time + updateInterval;
            UpdateGrassVisibility();
        }

        private void SpawnGrassPatches()
        {
            ClearExistingGrass();

            Random.InitState(randomSeed);

            TerrainData td = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 size = td.size;

            for (int i = 0; i < grassPatchCount; i++)
            {
                GameObject prefab = PickGrassPrefab();
                if (prefab == null)
                    continue;

                float x = Random.Range(edgePadding, size.x - edgePadding);
                float z = Random.Range(edgePadding, size.z - edgePadding);

                Vector3 worldPos = terrainPos + new Vector3(x, 0f, z);
                worldPos.y = terrain.SampleHeight(worldPos) + terrainPos.y + heightOffset;

                Vector3 terrainNormal = GetTerrainNormal(worldPos);

                Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, terrainNormal);
                Quaternion randomYaw = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                Quaternion rotation = slopeRotation * randomYaw * prefab.transform.rotation;

                GameObject grass = Instantiate(prefab, worldPos, rotation, transform);
                grass.name = prefab.name + "_Runtime";

                float scaleMultiplier = Random.Range(minScale, maxScale);
                grass.transform.localScale = prefab.transform.localScale * scaleMultiplier * grassSizeMultiplier;

                if (disableCollidersOnGrass)
                    DisableColliders(grass);

                spawnedGrass.Add(grass);
            }
        }

        private Vector3 GetTerrainNormal(Vector3 worldPos)
        {
            if (terrain == null || terrain.terrainData == null)
                return Vector3.up;

            TerrainData td = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;

            float normalizedX = Mathf.Clamp01((worldPos.x - terrainPos.x) / td.size.x);
            float normalizedZ = Mathf.Clamp01((worldPos.z - terrainPos.z) / td.size.z);

            return td.GetInterpolatedNormal(normalizedX, normalizedZ);
        }

        private GameObject PickGrassPrefab()
        {
            if (grassPatchSmallPrefab != null && grassPatchLargePrefab != null)
                return Random.value < 0.65f ? grassPatchSmallPrefab : grassPatchLargePrefab;

            if (grassPatchSmallPrefab != null)
                return grassPatchSmallPrefab;

            return grassPatchLargePrefab;
        }

        private void UpdateGrassVisibility()
        {
            if (player == null || spawnedGrass.Count == 0)
                return;

            float visibleDistanceSqr = visibleDistance * visibleDistance;
            Vector3 playerPos = player.position;

            for (int i = 0; i < spawnedGrass.Count; i++)
            {
                GameObject grass = spawnedGrass[i];
                if (grass == null)
                    continue;

                Vector3 delta = grass.transform.position - playerPos;
                delta.y = 0f;

                bool shouldBeVisible = delta.sqrMagnitude <= visibleDistanceSqr;

                if (grass.activeSelf != shouldBeVisible)
                    grass.SetActive(shouldBeVisible);
            }
        }

        private void DisableColliders(GameObject go)
        {
            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private void ClearExistingGrass()
        {
            for (int i = spawnedGrass.Count - 1; i >= 0; i--)
            {
                if (spawnedGrass[i] != null)
                    Destroy(spawnedGrass[i]);
            }

            spawnedGrass.Clear();
        }
    }
}
