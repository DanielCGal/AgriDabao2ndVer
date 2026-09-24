using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AgriDabao3D
{
    public static class CropNaming
    {
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
