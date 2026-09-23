using System;
using System.Globalization;

namespace AgriDabao3D
{
    /// <summary>
    /// Prices in centavos, wallets in whole pesos.
    ///
    /// The shop and the shipping bin use the Davao City government price list,
    /// which has centavos in it - corn seed is P388.89, a coconut sells for
    /// P16.89. The wallet, the save file, the marketplace and the server all
    /// count whole pesos, and moving all of that to centavos would have meant
    /// rewriting every saved farm. So prices are held exactly, in centavos, and
    /// only the total of a purchase or a sale becomes pesos: rounded to the
    /// nearest peso, with half a peso rounding up.
    ///
    /// The server repeats this rule (PesoRounding in the backend) when it charges
    /// a marketplace listing fee, and the two must round identically or the fee a
    /// player is shown would differ from the fee they pay. That is why rounding
    /// is done in whole-number arithmetic rather than with Math.Round, which in
    /// C# rounds halves to the nearest even number while Java rounds them up.
    ///
    /// Deliberately free of UnityEngine, so the rule can be compiled and checked
    /// on its own against the server's copy.
    /// </summary>
    public static class PesoPrice
    {
        /// <summary>The largest price that still fits in centavos.</summary>
        private const float MaxInspectorPesos = 21474836f;

        /// <summary>
        /// A price written in pesos, as centavos. Pass a decimal literal (388.89m)
        /// so nothing is lost on the way.
        /// </summary>
        public static int Centavos(decimal pesos)
        {
            if (pesos <= 0m)
                return 0;

            return (int)decimal.Round(pesos * 100m, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// A price typed into the Inspector, as centavos. Inspector fields are
        /// floats, and 16.89 cannot be held exactly in a float, so the value goes
        /// through decimal - which keeps the seven significant digits a float
        /// really has - before it is rounded to a whole centavo.
        /// </summary>
        public static int CentavosFromInspector(float pesos)
        {
            if (float.IsNaN(pesos) || pesos <= 0f)
                return 0;

            if (pesos >= MaxInspectorPesos)
                return int.MaxValue;

            return Centavos((decimal)pesos);
        }

        /// <summary>
        /// What <paramref name="count"/> items at <paramref name="centavosEach"/>
        /// come to in whole pesos: the total is rounded to the nearest peso, halves
        /// rounding up. The price of each one is never rounded on its own.
        /// </summary>
        public static int TotalPesos(int centavosEach, int count)
        {
            if (centavosEach <= 0 || count <= 0)
                return 0;

            long centavos = (long)centavosEach * count;
            long pesos = (centavos + 50L) / 100L;
            return pesos > int.MaxValue ? int.MaxValue : (int)pesos;
        }

        /// <summary>
        /// A price for display: "P388.89", or "P15" when there are no centavos, so
        /// whole-peso prices read the way they always have.
        /// </summary>
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
