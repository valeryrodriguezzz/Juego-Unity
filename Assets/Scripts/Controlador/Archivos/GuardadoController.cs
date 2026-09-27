using System;
using System.Collections.Generic;
using System.IO;
using ImperiosEnGuerra.Modelo;
using UnityEngine;

namespace ImperiosEnGuerra.Controlador
{
    /// <summary>
    /// CONTROLADOR de los archivos de partidas guardadas.
    ///
    /// Hermano de ArchivoController y con el mismo reparto: el Modelo
    /// (PartidaGuardadaModel) sabe QUE se guarda y como se escribe en texto;
    /// esta clase solo sabe DONDE va ese texto y como leerlo de vuelta.
    ///
    /// Las partidas quedan en:
    ///     Application.persistentDataPath/partidas/partida_1.txt ... _6.txt
    ///
    /// En Windows eso es
    ///     C:/Users/&lt;tu usuario&gt;/AppData/LocalLow/&lt;empresa&gt;/&lt;juego&gt;/partidas
    /// y se puede abrir con el Bloc de notas: son archivos de texto normales.
    ///
    /// CONCURRENCIA
    /// Mismo patron que ArchivoController: un solo candado para todos los
    /// archivos, porque cualquier hilo podria pedir guardar. Y
    /// persistentDataPath solo se puede leer desde el hilo principal, asi que
    /// se copia al arrancar el juego y los hilos usan la copia.
    /// </summary>
    public static class GuardadoController
    {
        public const int RANURAS = 6;

        private static string _carpeta;
        private static readonly object _candado = new object();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Inicializar()
        {
            _carpeta = Path.Combine(Application.persistentDataPath, "partidas");
        }

        private static string Carpeta()
        {
            if (_carpeta == null)
                _carpeta = Path.Combine(Application.persistentDataPath, "partidas");

            return _carpeta;
        }

        private static string Ruta(int ranura)
        {
            return Path.Combine(Carpeta(), "partida_" + ranura + ".txt");
        }

        /// <summary>Donde estan los archivos, para poder decirselo al jugador.</summary>
        public static string CarpetaVisible()
        {
            return Carpeta();
        }

        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve las RANURAS posiciones; las vacias vienen en null. Se
        /// devuelve el hueco y no solo lo que hay para que el menu pueda
        /// pintar "Ranura 3 - vacia" y dejar guardar ahi.
        /// </summary>
        public static List<PartidaGuardadaModel> Listar()
        {
            var lista = new List<PartidaGuardadaModel>();

            for (int i = 1; i <= RANURAS; i++)
                lista.Add(Cargar(i));

            return lista;
        }

        public static PartidaGuardadaModel Cargar(int ranura)
        {
            try
            {
                string ruta = Ruta(ranura);
                string texto;

                lock (_candado)
                {
                    if (!File.Exists(ruta)) return null;
                    texto = File.ReadAllText(ruta);
                }

                return PartidaGuardadaModel.DeTexto(texto);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Guardado] No pude leer la ranura " + ranura + ": " + ex.Message);
                return null;
            }
        }

        public static bool Guardar(int ranura, PartidaGuardadaModel datos)
        {
            if (datos == null) return false;

            try
            {
                string texto = datos.ATexto();

                lock (_candado)
                {
                    Directory.CreateDirectory(Carpeta());
                    File.WriteAllText(Ruta(ranura), texto);
                }

                Debug.Log("[Guardado] Partida guardada en la ranura " + ranura +
                          ": " + Ruta(ranura));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Guardado] No pude guardar en la ranura " + ranura +
                                 ": " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Guarda en la primera ranura libre. Si estan todas llenas, pisa la
        /// mas vieja: es mejor eso que negarse a guardar y que el jugador
        /// pierda el rato que acaba de jugar.
        /// </summary>
        public static int GuardarEnLibre(PartidaGuardadaModel datos)
        {
            List<PartidaGuardadaModel> actuales = Listar();

            for (int i = 0; i < actuales.Count; i++)
                if (actuales[i] == null)
                    return Guardar(i + 1, datos) ? i + 1 : -1;

            int masVieja = 1;
            string fechaMasVieja = null;

            for (int i = 0; i < actuales.Count; i++)
            {
                string f = actuales[i].Fecha;

                if (fechaMasVieja == null || string.CompareOrdinal(f, fechaMasVieja) < 0)
                {
                    // Las fechas se escriben como yyyy-MM-dd HH:mm, que ordena
                    // igual alfabeticamente que cronologicamente. Por eso se
                    // pueden comparar como texto sin volver a parsearlas.
                    fechaMasVieja = f;
                    masVieja = i + 1;
                }
            }

            return Guardar(masVieja, datos) ? masVieja : -1;
        }

        public static bool Borrar(int ranura)
        {
            try
            {
                string ruta = Ruta(ranura);

                lock (_candado)
                {
                    if (!File.Exists(ruta)) return false;
                    File.Delete(ruta);
                }

                Debug.Log("[Guardado] Borre la ranura " + ranura + ".");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Guardado] No pude borrar la ranura " + ranura +
                                 ": " + ex.Message);
                return false;
            }
        }

        public static bool HayAlguna()
        {
            for (int i = 1; i <= RANURAS; i++)
                if (Cargar(i) != null) return true;

            return false;
        }
    }
}
