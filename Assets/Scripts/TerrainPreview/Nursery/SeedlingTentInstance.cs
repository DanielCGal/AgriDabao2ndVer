using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public class SeedlingTentInstance : MonoBehaviour
    {
        [Tooltip("Where each seedling bag stands on the racks, in slot order (eight).")]
        public Transform[] bagAnchors = new Transform[NurserySystem.BagCount];

        [Tooltip("Seedling size when just sown and when ready, as a share of its prefab's own scale.")]
        public float seedlingStartScale = 0.35f;
        public float seedlingReadyScale = 1f;

        private readonly GameObject[] bagVisuals = new GameObject[NurserySystem.BagCount];
        private readonly bool[] bagShownFilled = new bool[NurserySystem.BagCount];
        private readonly GameObject[] seedlingVisuals = new GameObject[NurserySystem.BagCount];
        private readonly string[] seedlingShown = new string[NurserySystem.BagCount];
        private readonly Vector3[] seedlingBaseScale = new Vector3[NurserySystem.BagCount];
        private readonly float[] seedlingHeight = new float[NurserySystem.BagCount];
        private readonly float[] bagTop = new float[NurserySystem.BagCount];
        private FarmingInteractionSystem farming;

        private FarmingInteractionSystem Farming
        {
            get
            {
                if (farming == null)
                    farming = FindFirstObjectByType<FarmingInteractionSystem>();
                return farming;
            }
        }

        public void SeatOnTerrain(Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
                return;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Vector3 position = transform.position;
            float lowest = terrain.SampleHeight(position);

            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                Vector3[] corners =
                {
                    new Vector3(bounds.min.x, 0f, bounds.min.z),
                    new Vector3(bounds.min.x, 0f, bounds.max.z),
                    new Vector3(bounds.max.x, 0f, bounds.min.z),
                    new Vector3(bounds.max.x, 0f, bounds.max.z)
                };

                foreach (Vector3 corner in corners)
                    lowest = Mathf.Min(lowest, terrain.SampleHeight(corner));
            }

            position.y = lowest + terrain.transform.position.y;
            transform.position = position;
        }

        public void Refresh(NurserySystem nursery)
        {
            if (nursery == null)
                return;

            for (int slot = 0; slot < NurserySystem.BagCount; slot++)
                RefreshSlot(nursery, slot);
        }

        public void RefreshGrowth(NurserySystem nursery)
        {
            if (nursery == null)
                return;

            for (int slot = 0; slot < NurserySystem.BagCount; slot++)
            {
                if (seedlingVisuals[slot] != null)
                    ScaleSeedling(nursery, slot);
            }
        }

        private void RefreshSlot(NurserySystem nursery, int slot)
        {
            Transform anchor = AnchorFor(slot);
            if (anchor == null)
                return;

            SeedlingBagStatus status = nursery.GetStatus(slot);
            bool filled = status != SeedlingBagStatus.Empty;

            if (bagVisuals[slot] == null || bagShownFilled[slot] != filled)
            {
                if (bagVisuals[slot] != null)
                    Destroy(bagVisuals[slot]);

                GameObject prefab = Farming != null
                    ? (filled ? Farming.seedlingBagFilledPrefab : Farming.seedlingBagEmptyPrefab)
                    : null;

                bagVisuals[slot] = prefab != null ? Spawn(prefab, anchor, "Bag") : null;
                bagShownFilled[slot] = filled;
            }

            string showing = null;
            GameObject seedlingPrefab = null;
            if ((status == SeedlingBagStatus.Growing || status == SeedlingBagStatus.Ready) &&
                nursery.TryGetMaterial(slot, out PlantingMaterialInfo info) && Farming != null)
            {
                seedlingPrefab = Farming.GetSeedlingPrefab(info.Crop);
                showing = seedlingPrefab != null ? info.Item.ToString() : null;
            }

            if (seedlingShown[slot] != showing)
            {
                if (seedlingVisuals[slot] != null)
                    Destroy(seedlingVisuals[slot]);

                seedlingVisuals[slot] = null;
                seedlingShown[slot] = showing;

                if (seedlingPrefab != null)
                {
                    GameObject seedling = Spawn(seedlingPrefab, anchor, "Seedling");
                    seedlingBaseScale[slot] = seedling.transform.localScale;
                    seedlingHeight[slot] = MeasureHeight(seedling, anchor);
                    bagTop[slot] = BagTop(slot, anchor);
                    seedlingVisuals[slot] = seedling;
                }
            }

            if (seedlingVisuals[slot] != null)
                ScaleSeedling(nursery, slot);
        }

        private void ScaleSeedling(NurserySystem nursery, int slot)
        {
            float progress = nursery.GetStatus(slot) == SeedlingBagStatus.Ready ? 1f : nursery.GrowthProgress(slot);
            float scale = Mathf.Lerp(seedlingStartScale, seedlingReadyScale, progress);

            Transform seedling = seedlingVisuals[slot].transform;
            seedling.localScale = seedlingBaseScale[slot] * scale;
            float rooted = seedlingHeight[slot] * scale * FarmingInteractionSystem.SeedlingRootShare;
            seedling.localPosition = Vector3.up * Mathf.Max(0.01f, bagTop[slot] - rooted);
        }

        private static float MeasureHeight(GameObject model, Transform anchor)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return 0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float scale = anchor.lossyScale.y;
            return scale > 0.0001f ? bounds.size.y / scale : bounds.size.y;
        }

        private float BagTop(int slot, Transform anchor)
        {
            GameObject bag = bagVisuals[slot];
            if (bag == null)
                return 0f;

            Renderer[] renderers = bag.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return 0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float worldTop = bounds.max.y - bounds.size.y * 0.12f;
            return anchor.InverseTransformPoint(new Vector3(anchor.position.x, worldTop, anchor.position.z)).y;
        }

        private Transform AnchorFor(int slot)
        {
            if (bagAnchors != null && slot < bagAnchors.Length && bagAnchors[slot] != null)
                return bagAnchors[slot];

            string name = "BagAnchor_" + slot;
            Transform existing = transform.Find(name);
            if (existing != null)
                return existing;

            Transform created = new GameObject(name).transform;
            created.SetParent(transform, false);
            created.localPosition = new Vector3(-1.2f + (slot % 4) * 0.8f, 0f, slot < 4 ? -0.6f : 0.6f);
            return created;
        }

        private static GameObject Spawn(GameObject prefab, Transform anchor, string label)
        {
            GameObject instance = Instantiate(prefab, anchor, false);
            instance.name = prefab.name + "_" + label;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = prefab.transform.localRotation;
            instance.transform.localScale = prefab.transform.localScale;

            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
                Destroy(body);

            DistanceCullable.Attach(instance);
            return instance;
        }
    }
}
