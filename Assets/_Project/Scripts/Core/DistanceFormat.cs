using System.Globalization;

namespace Lumen.Core
{
    /// <summary>
    /// Converte distancias do jogo (unidades do Unity) em quilometros do Sistema Solar para o HUD.
    /// Escala fictícia: o trecho Netuno -> Terra tem ~15.100 unidades e na realidade ~4,3 bilhoes
    /// de km, entao 1 unidade = KmPerUnit km. (O tamanho dos planetas segue outra escala, so a
    /// distancia entre eles e convertida.)
    /// </summary>
    public static class DistanceFormat
    {
        public const float KmPerUnit = 280000f;

        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        /// <summary>Texto curto: "4,25 bilhões de km", "812,4 milhões de km" ou "950.000 km".</summary>
        public static string Km(float units)
        {
            double km = units * (double)KmPerUnit;

            if (km >= 1e9) return (km / 1e9).ToString("0.00", PtBr) + " bilhões de km";
            if (km >= 1e6) return (km / 1e6).ToString("0.0", PtBr) + " milhões de km";
            return km.ToString("N0", PtBr) + " km";
        }
    }
}
