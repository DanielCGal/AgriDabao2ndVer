using System;
using UnityEngine;

namespace AgriDabao3D
{
    public static class DeviceIdentity
    {
        private const string Key = "device.id";

        private static string cached;

        public static string Current
        {
            get
            {
                if (!string.IsNullOrEmpty(cached))
                    return cached;

                cached = PlayerPrefs.GetString(Key, string.Empty);

                if (string.IsNullOrEmpty(cached))
                {
                    cached = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(Key, cached);
                    PlayerPrefs.Save();
                }

                return cached;
            }
        }
    }
}
