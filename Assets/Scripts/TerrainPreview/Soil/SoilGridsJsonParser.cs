using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AgriDabao3D
{
    public static class SoilGridsJsonParser
    {
        public static SoilSample Parse(string json)
        {
            try
            {
                JObject root = JObject.Parse(json);
                JArray layers = (JArray)root["properties"]?["layers"];
                if (layers == null) return null;

                SoilSample sample = new SoilSample();

                foreach (JObject layer in layers)
                {
                    string name = layer["name"]?.ToString();
                    JArray depths = (JArray)layer["depths"];
                    if (depths == null || depths.Count == 0) continue;

                    JObject firstDepth = (JObject)depths[0];
                    float rawValue = firstDepth["values"]?["mean"]?.Value<float>() ?? 0f;
                    float convertedValue = ConvertSoilGridsValue(name, rawValue);

                    switch (name)
                    {
                        case "sand": sample.sand = convertedValue; break;
                        case "silt": sample.silt = convertedValue; break;
                        case "clay": sample.clay = convertedValue; break;
                        case "phh2o": sample.phh2o = convertedValue; break;
                        case "soc": sample.soc = convertedValue; break;
                        case "cfvo": sample.cfvo = convertedValue; break;
                        case "bdod": sample.bdod = convertedValue; break;
                        case "nitrogen": sample.nitrogen = convertedValue; break;
                    }
                }

                return sample;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("SoilGrids parse error: " + ex.Message);
                return null;
            }
        }

        private static float ConvertSoilGridsValue(string propertyName, float rawValue)
        {
            switch (propertyName)
            {
                // SoilGrids sand/silt/clay often come in tenths of percent
                case "sand":
                case "silt":
                case "clay":
                    return Mathf.Clamp(rawValue / 10f, 0f, 100f);

                // pH often comes in tenths
                case "phh2o":
                    return Mathf.Clamp(rawValue / 10f, 3.5f, 9.5f);

                // organic carbon: compress to a game-friendly range
                case "soc":
                    return Mathf.Clamp(rawValue / 20f, 0f, 100f);

                // coarse fragments volume: tenths of percent works well
                case "cfvo":
                    return Mathf.Clamp(rawValue / 10f, 0f, 100f);

                // bulk density: keep it around your existing gameplay thresholds
                case "bdod":
                    return Mathf.Clamp(rawValue, 60f, 180f);

                // nitrogen: compress to a readable gameplay range
                case "nitrogen":
                    return Mathf.Clamp(rawValue / 10f, 0f, 100f);

                default:
                    return rawValue;
            }
        }
    }
}
