using UnityEngine;

namespace AgriDabao3D
{
    public static class DavaoGeoReference
    {
        // Corners of the area selection satellite map. These belong to the map
        // picture the player drags the box on, so they have to be replaced
        // together with it - a new map with the old corners would hand the terrain
        // generator coordinates for somewhere else entirely.
        private const double TopLeftLat = 7.6089;
        private const double TopLeftLon = 125.1439;

        private const double TopRightLat = 7.6089;
        private const double TopRightLon = 125.7132;

        private const double BottomLeftLat = 6.9381;
        private const double BottomLeftLon = 125.1509;

        private const double BottomRightLat = 6.9381;
        private const double BottomRightLon = 125.7182;

        // u = 0 left, 1 right
        // v = 0 bottom, 1 top
        public static Vector2 LatLonFromNormalized(float u, float v)
        {
            double bottomLat = Mathf.Lerp((float)BottomLeftLat, (float)BottomRightLat, u);
            double bottomLon = Mathf.Lerp((float)BottomLeftLon, (float)BottomRightLon, u);

            double topLat = Mathf.Lerp((float)TopLeftLat, (float)TopRightLat, u);
            double topLon = Mathf.Lerp((float)TopLeftLon, (float)TopRightLon, u);

            double lat = Mathf.Lerp((float)bottomLat, (float)topLat, v);
            double lon = Mathf.Lerp((float)bottomLon, (float)topLon, v);

            return new Vector2((float)lat, (float)lon);
        }

        public static Vector2 GetSamplePointA(Rect selectedRect)
        {
            float u = selectedRect.xMin + selectedRect.width * 0.25f;
            float v = selectedRect.yMin + selectedRect.height * 0.25f;
            return LatLonFromNormalized(u, v);
        }

        public static Vector2 GetSamplePointB(Rect selectedRect)
        {
            float u = selectedRect.xMin + selectedRect.width * 0.75f;
            float v = selectedRect.yMin + selectedRect.height * 0.75f;
            return LatLonFromNormalized(u, v);
        }
    }
}
