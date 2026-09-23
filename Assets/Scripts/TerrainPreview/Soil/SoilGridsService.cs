using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace AgriDabao3D
{
    public class SoilGridsService : MonoBehaviour
    {
        public IEnumerator QuerySoil(float lat, float lon, Action<SoilSample> onDone)
        {
            string url =
                "https://rest.isric.org/soilgrids/v2.0/properties/query" +
                $"?lon={lon}&lat={lat}" +
                "&property=sand&property=silt&property=clay&property=phh2o" +
                "&property=soc&property=cfvo&property=bdod&property=nitrogen" +
                "&depth=0-5cm&value=mean";

            using UnityWebRequest req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("SoilGrids request failed: " + req.error);

                // fallback fake sample so your prototype still works
                onDone?.Invoke(new SoilSample
                {
                    sand = 45f,
                    silt = 28f,
                    clay = 27f,
                    phh2o = 6.1f,
                    soc = 22f,
                    cfvo = 6f,
                    bdod = 125f,
                    nitrogen = 14f
                });
                yield break;
            }

            string json = req.downloadHandler.text;

            Debug.Log($"SoilGrids success for lat={lat}, lon={lon}");
            Debug.Log("SoilGrids raw JSON received.");

            // You should parse this with Newtonsoft Json package in Unity.
            // Package Manager -> Add package -> Newtonsoft Json
            SoilSample sample = SoilGridsJsonParser.Parse(json);

            if (sample != null)
            {
                Debug.Log(
                    $"Parsed SoilGrids -> sand={sample.sand}, silt={sample.silt}, clay={sample.clay}, " +
                    $"phh2o={sample.phh2o}, soc={sample.soc}, cfvo={sample.cfvo}, bdod={sample.bdod}, nitrogen={sample.nitrogen}"
                );
            }


            if (sample == null)
            {
                Debug.LogWarning("Failed to parse SoilGrids JSON, using fallback.");
                sample = new SoilSample
                {
                    sand = 45f,
                    silt = 28f,
                    clay = 27f,
                    phh2o = 6.1f,
                    soc = 22f,
                    cfvo = 6f,
                    bdod = 125f,
                    nitrogen = 14f
                };
            }

            onDone?.Invoke(sample);
        }
    }
}
