using System;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// A stable name for this installation, used to tell one phone from another
    /// when the same account signs in on both.
    ///
    /// Generated once and kept in PlayerPrefs rather than read from
    /// SystemInfo.deviceUniqueIdentifier, which on Android can change between OS
    /// versions, is not guaranteed on every device, and identifies the hardware
    /// rather than the install. A value of our own is stable for as long as the
    /// game is installed, which is exactly the lifetime of a saved login, and it
    /// says nothing about the device or its owner.
    /// </summary>
    public static class DeviceIdentity
    {
        private const string Key = "device.id";

        private static string cached;

        /// <summary>This installation's id. Created on first use.</summary>
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
