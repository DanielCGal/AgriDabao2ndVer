using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public sealed class CropRuntimeAdapter
    {
        private readonly CoconutTreeInstance coconut;
        private readonly BananaPlantInstance banana;
        private readonly TropicalCropPlantInstance tropical;

        private CropRuntimeAdapter(CoconutTreeInstance value) { coconut = value; }
        private CropRuntimeAdapter(BananaPlantInstance value) { banana = value; }
        private CropRuntimeAdapter(TropicalCropPlantInstance value) { tropical = value; }

        public GameObject Root => coconut != null ? coconut.gameObject : banana != null ? banana.gameObject : tropical.gameObject;
        public Transform Transform => Root.transform;

        public FarmCropType CropType
        {
            get
            {
                if (coconut != null) return FarmCropType.Coconut;
                if (banana != null) return FarmCropType.Banana;
                return CropClimateRules.GetFarmCropType(tropical.cropKind);
            }
        }

        public string CropId => coconut != null ? coconut.cropId : banana != null ? banana.cropId : tropical.cropId;
        public string CropName => coconut != null ? coconut.cropName : banana != null ? banana.cropName : tropical.cropName;
        public string CropDisplayName => coconut != null ? "Coconut" : banana != null ? "Banana" : tropical.CropDisplayName;
        public string District => coconut != null ? coconut.districtName : banana != null ? banana.districtName : tropical.districtName;
        public string Stage => coconut != null ? coconut.stage.ToString() : banana != null ? banana.stage.ToString() : tropical.stage.ToString();
        public float Health => coconut != null ? coconut.health : banana != null ? banana.health : tropical.health;
        public float AverageHealth => coconut != null ? coconut.averageHealth : banana != null ? banana.averageHealth : tropical.averageHealth;
        public float Stress => coconut != null ? coconut.stress : banana != null ? banana.stress : tropical.stress;
        public float Moisture => coconut != null ? coconut.moisture : banana != null ? banana.moisture : tropical.moisture;
        public float Fertility => coconut != null ? coconut.fertility : banana != null ? banana.fertility : tropical.fertility;
        public float Drainage => coconut != null ? coconut.drainage : banana != null ? banana.drainage : tropical.drainage;
        public float SoilSuitability => coconut != null ? coconut.soilSuitability : banana != null ? banana.soilSuitability : tropical.soilSuitability;

        public void AddHealth(float value)
        {
            if (coconut != null) coconut.health = Mathf.Clamp(coconut.health + value, 0f, 100f);
            else if (banana != null) banana.health = Mathf.Clamp(banana.health + value, 0f, 100f);
            else tropical.health = Mathf.Clamp(tropical.health + value, 0f, 100f);
        }

        public void AddStress(float value)
        {
            if (coconut != null) coconut.stress = Mathf.Clamp(coconut.stress + value, 0f, 100f);
            else if (banana != null) banana.stress = Mathf.Clamp(banana.stress + value, 0f, 100f);
            else tropical.stress = Mathf.Clamp(tropical.stress + value, 0f, 100f);
        }

        public void AddMoisture(float value)
        {
            if (coconut != null) coconut.moisture = Mathf.Clamp01(coconut.moisture + value);
            else if (banana != null) banana.moisture = Mathf.Clamp01(banana.moisture + value);
            else tropical.moisture = Mathf.Clamp01(tropical.moisture + value);
        }

        public void AddFertility(float value)
        {
            if (coconut != null) coconut.fertility = Mathf.Clamp01(coconut.fertility + value);
            else if (banana != null) banana.fertility = Mathf.Clamp01(banana.fertility + value);
            else tropical.fertility = Mathf.Clamp01(tropical.fertility + value);
        }

        public void AddDrainage(float value)
        {
            if (coconut != null) coconut.drainage = Mathf.Clamp01(coconut.drainage + value);
            else if (banana != null) banana.drainage = Mathf.Clamp01(banana.drainage + value);
            else tropical.drainage = Mathf.Clamp01(tropical.drainage + value);
        }

        public CropClimateMaintenanceState GetOrAddMaintenanceState()
        {
            CropClimateMaintenanceState state = Root.GetComponent<CropClimateMaintenanceState>();
            if (state == null) state = Root.AddComponent<CropClimateMaintenanceState>();
            state.Bind(this);
            return state;
        }

        public static bool TryCreate(GameObject root, out CropRuntimeAdapter adapter)
        {
            adapter = null;
            if (root == null) return false;
            CoconutTreeInstance coconut = root.GetComponent<CoconutTreeInstance>();
            if (coconut != null) { adapter = new CropRuntimeAdapter(coconut); return true; }
            BananaPlantInstance banana = root.GetComponent<BananaPlantInstance>();
            if (banana != null) { adapter = new CropRuntimeAdapter(banana); return true; }
            TropicalCropPlantInstance tropical = root.GetComponent<TropicalCropPlantInstance>();
            if (tropical != null) { adapter = new CropRuntimeAdapter(tropical); return true; }
            return false;
        }

        public static bool TryFromCollider(Collider collider, out CropRuntimeAdapter adapter)
        {
            adapter = null;
            if (collider == null) return false;
            CoconutTreeInstance coconut = collider.GetComponentInParent<CoconutTreeInstance>();
            if (coconut != null) { adapter = new CropRuntimeAdapter(coconut); return true; }
            BananaPlantInstance banana = collider.GetComponentInParent<BananaPlantInstance>();
            if (banana != null) { adapter = new CropRuntimeAdapter(banana); return true; }
            TropicalCropPlantInstance tropical = collider.GetComponentInParent<TropicalCropPlantInstance>();
            if (tropical != null) { adapter = new CropRuntimeAdapter(tropical); return true; }
            return false;
        }

        public static List<CropRuntimeAdapter> FindAll()
        {
            List<CropRuntimeAdapter> result = new List<CropRuntimeAdapter>();
            foreach (CoconutTreeInstance crop in Object.FindObjectsByType<CoconutTreeInstance>(FindObjectsSortMode.None))
                if (crop != null) result.Add(new CropRuntimeAdapter(crop));
            foreach (BananaPlantInstance crop in Object.FindObjectsByType<BananaPlantInstance>(FindObjectsSortMode.None))
                if (crop != null) result.Add(new CropRuntimeAdapter(crop));
            foreach (TropicalCropPlantInstance crop in Object.FindObjectsByType<TropicalCropPlantInstance>(FindObjectsSortMode.None))
                if (crop != null) result.Add(new CropRuntimeAdapter(crop));
            return result;
        }
    }
}
