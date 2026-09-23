using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    [CreateAssetMenu(
        menuName = "AgriDabao/Pest Disease Database",
        fileName = "PestDiseaseDatabase"
    )]
    public class PestDiseaseDatabase : ScriptableObject
    {
        public List<PestDiseaseRule> rules =
            new List<PestDiseaseRule>();

        public List<PestDiseaseRule> GetRulesForCrop(
            FarmCropType crop)
        {
            return rules.FindAll(
                rule =>
                    rule != null &&
                    rule.affectedCrop == crop
            );
        }

        public PestDiseaseRule GetRule(
            FarmCropType crop,
            PestDiseaseType type)
        {
            return rules.Find(
                rule =>
                    rule != null &&
                    rule.affectedCrop == crop &&
                    rule.type == type
            );
        }
    }
}
