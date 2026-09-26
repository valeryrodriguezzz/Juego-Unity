using System;
using System.IO;
using UnityEngine;

namespace ImperiosEnGuerra.Controlador
{
    // Maneja la lectura y escritura de archivos de texto del juego.
    // Se crean 3 archivos en Application.persistentDataPath:
    //   - configuracion.txt  : estado inicial de la partida
    //   - log_partida.txt    : registro de cada accion
    //   - resultado_final.txt: quien gano y cuanto duro
    public static class ArchivoController
    {
        // Application.persistentDataPath solo se puede leer desde el hilo principal,
        // asi que se guarda aqui al arrancar el juego y los hilos usan la copia.
        private static string _carpeta;

        // Varios hilos pueden escribir a la vez: un solo candado para todos los archivos.
        private static readonly object _candadoArchivo = new object();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Inicializar()
        {
            _carpeta = Application.persistentDataPath;
        }

        private static string Ruta(string nombreArchivo)
        {
            if (_carpeta == null) _carpeta = Application.persistentDataPath;
            return Path.Combine(_carpeta, nombreArchivo);
        }

        // Guarda los datos iniciales cuando empieza la partida.
        // Se llama desde PartidaModel.IniciarBatalla() o desde el Controlador.
        public static void GuardarConfiguracion(string nombreJugador, string civilizacion, string detalle = "")
        {
            try
            {
                string contenido =
                    "=== CONFIGURACION DE PARTIDA ===" + Environment.NewLine +
                    "Jugador     : " + nombreJugador   + Environment.NewLine +
                    "Civilizacion: " + civilizacion    + Environment.NewLine +
                    "Fecha inicio: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (!string.IsNullOrEmpty(detalle))
                    contenido += Environment.NewLine + detalle;

                lock (_candadoArchivo) { File.WriteAllText(Ruta("configuracion.txt"), contenido); }
                Debug.Log("[Archivo] configuracion.txt guardado en: " + Ruta("configuracion.txt"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Archivo] No se pudo guardar configuracion.txt: " + ex.Message);
            }
        }

        // Agrega una linea al log de la partida.
        // Se llama cada vez que el jugador o la IA hacen algo importante.
        public static void RegistrarAccion(int turno, string quienJuega, string accion, string resultado)
        {
            try
            {
                // Formato pedido en el enunciado
                string bloque =
                    "Turno: " + quienJuega + " " + turno + Environment.NewLine +
                    "Acción: " + accion + Environment.NewLine +
                    "Resultado: " + resultado + Environment.NewLine + Environment.NewLine;

                lock (_candadoArchivo) { File.AppendAllText(Ruta("log_partida.txt"), bloque); }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Archivo] No se pudo escribir en log_partida.txt: " + ex.Message);
            }
        }

        // Guarda el resultado final cuando la partida termina.
        // Se llama desde PartidaModel.TerminarBatalla().
        public static void GuardarResultado(string ganador, string duracion, bool jugadorGano, string estadoFinal = "")
        {
            try
            {
                string contenido =
                    "=== RESULTADO FINAL ===" + Environment.NewLine +
                    "Ganador  : " + ganador    + Environment.NewLine +
                    "Resultado: " + (jugadorGano ? "Victoria del jugador" : "Victoria de la IA") + Environment.NewLine +
                    "Duracion : " + duracion   + Environment.NewLine +
                    "Fecha fin: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (!string.IsNullOrEmpty(estadoFinal))
                    contenido += Environment.NewLine + estadoFinal;

                lock (_candadoArchivo) { File.WriteAllText(Ruta("resultado_final.txt"), contenido); }
                Debug.Log("[Archivo] resultado_final.txt guardado. Ganador: " + ganador);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Archivo] No se pudo guardar resultado_final.txt: " + ex.Message);
            }
        }

        // Borra el log anterior al iniciar una partida nueva.
        public static void LimpiarLog()
        {
            try
            {
                string ruta = Ruta("log_partida.txt");
                lock (_candadoArchivo) { if (File.Exists(ruta)) File.Delete(ruta); }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Archivo] No se pudo limpiar log_partida.txt: " + ex.Message);
            }
        }
    }
}
