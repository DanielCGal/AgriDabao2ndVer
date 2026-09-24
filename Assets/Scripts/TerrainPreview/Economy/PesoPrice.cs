using System;
using System.Globalization;

namespace AgriDabao3D
{
    public static class PesoPrice
    {
        private const float MaxInspectorPesos = 21474836f;

        public static int Centavos(decimal pesos)
        {
            if (pesos <= 0m)
                return 0;

            return (int)decimal.Round(pesos * 100m, MidpointRounding.AwayFromZero);
        }

        public static int CentavosFromInspector(float pesos)
        {
            if (float.IsNaN(pesos) || pesos <= 0f)
                return 0;

            if (pesos >= MaxInspectorPesos)
                return int.MaxValue;

            return Centavos((decimal)pesos);
        }

        public static int TotalPesos(int centavosEach, int count)
        {
            if (centavosEach <= 0 || count <= 0)
                return 0;

            long centavos = (long)centavosEach * count;
            long pesos = (centavos + 50L) / 100L;
            return pesos > int.MaxValue ? int.MaxValue : (int)pesos;
        }

        public static string Label(int centavos)
        {
            if (centavos < 0)
                centavos = 0;

            int pesos = centavos / 100;
            int cents = centavos % 100;

            string whole = "P" + pesos.ToString(CultureInfo.InvariantCulture);
            return cents == 0
                ? whole
                : whole + "." + cents.ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
