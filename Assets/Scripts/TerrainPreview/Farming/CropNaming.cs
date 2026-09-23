using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AgriDabao3D
{
    /// <summary>
    /// Friendly per-crop names like "corn_1", "cacao_2". These are a display/AI-facing
    /// alias only - the machine <c>cropId</c> GUID and all matching logic are unchanged.
    /// </summary>
    public static class CropNaming
    {
        /// <summary>
        /// Next friendly name for a crop type, e.g. "corn_1" then "corn_2". Numbering is
        /// derived by scanning the live crops, so it keeps counting correctly after a
        /// save/reload with no persistent counter to reset.
        /// </summary>
        public static string NextName(string cropTypeLabel)
        {
            string prefix = Slug(cropTypeLabel) + "_";

            int max = 0;
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null)
                    continue;

                string name = crop.CropName;
                if (string.IsNullOrEmpty(name))
                    continue;

                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string tail = name.Substring(prefix.Length);
                if (int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > max)
                    max = n;
            }

            return prefix + (max + 1);
        }

        /// <summary>{ cropId GUID -> friendly name } for every live crop.</summary>
        public static Dictionary<string, string> LiveIdToNameMap()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null)
                    continue;

                string id = crop.CropId;
                string name = crop.CropName;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                    map[id] = name;
            }
            return map;
        }

        /// <summary>
        /// Replaces any raw crop GUID inside AI-generated text with the crop's friendly
        /// name, so the player never sees the machine id even if the model leaks one.
        /// GUIDs are unique 32-char strings, so the replacement can't corrupt other text.
        /// </summary>
        public static string Humanize(string aiText)
        {
            if (string.IsNullOrEmpty(aiText))
                return aiText;

            foreach (KeyValuePair<string, string> pair in LiveIdToNameMap())
            {
                if (!string.IsNullOrEmpty(pair.Key))
                    aiText = aiText.Replace(pair.Key, pair.Value);
            }
            return aiText;
        }

        private static string Slug(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return "crop";

            StringBuilder sb = new StringBuilder(label.Length);
            foreach (char c in label.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_')
                    sb.Append('_');
            }

            string result = sb.ToString().Trim('_');
            return result.Length == 0 ? "crop" : result;
        }
    }
}
