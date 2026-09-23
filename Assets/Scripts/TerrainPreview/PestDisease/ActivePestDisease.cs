using System;
using UnityEngine;

namespace AgriDabao3D
{
    [Serializable]
    public class ActivePestDisease
    {
        public PestDiseaseType type;

        [Range(0f, 100f)]
        public float severity;

        [Range(0f, 100f)]
        public float targetSeverity;

        public float startedGameDay;
    }
}
