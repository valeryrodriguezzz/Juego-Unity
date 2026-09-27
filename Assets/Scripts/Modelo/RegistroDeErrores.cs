using System;

namespace ImperiosEnGuerra.Modelo
{
    /// <summary>
    /// MODELO: el sitio al que van a parar los errores que se atrapan dentro
    /// de los hilos.
    ///
    /// POR QUE HACE FALTA
    /// Una excepcion que se escapa de un hilo secundario NO cierra el juego:
    /// mata en silencio a ese hilo y nada mas. Eso es peor que un error
    /// visible, porque el juego sigue andando con una pieza muerta y no se
    /// nota hasta mucho despues: la granja deja de producir, el hambre deja
    /// de bajar, la tienda deja de reponer, el arma deja de repararse. Y en
    /// la consola no aparece nada.
    ///
    /// Por eso cada hilo del proyecto envuelve su trabajo en try-catch, y lo
    /// que atrapa lo manda aqui. Esta clase no sabe ni imprime nada: solo
    /// avisa. Quien escucha es ErroresController, que ya es codigo de Unity y
    /// puede escribirlo en la consola y en log_partida.txt.
    ///
    /// Se hace asi para que el Modelo siga siendo C# puro, sin una sola
    /// referencia a UnityEngine: de otro modo habria que llamar a Debug.Log
    /// desde aqui y el Modelo dejaria de poder compilarse y probarse solo.
    ///
    /// Es thread-safe: la llaman los ocho hilos del juego, a veces a la vez.
    /// </summary>
    public static class RegistroDeErrores
    {
        private static readonly object _candado = new object();
        private static int _cuantos;

        /// <summary>Cuantos errores se han atrapado desde que arranco el juego.</summary>
        public static int Cuantos
        {
            get { lock (_candado) { return _cuantos; } }
        }

        /// <summary>
        /// Avisa de un error atrapado. El primer parametro es donde paso, para
        /// no tener que adivinarlo: "GranjaModel.ProducirEnHilo", por ejemplo.
        /// </summary>
        public static event Action<string, Exception> Ocurrio;

        public static void Reportar(string donde, Exception error)
        {
            if (error == null) return;

            lock (_candado) { _cuantos++; }

            // El aviso va con su propio try-catch, y no es paranoia: esto se
            // llama DESDE un catch. Si el suscriptor fallara (por ejemplo al
            // intentar escribir en un disco lleno), la excepcion saldria del
            // bloque catch original y mataria el hilo, que es exactamente lo
            // que se esta intentando evitar.
            try
            {
                Ocurrio?.Invoke(donde, error);
            }
            catch
            {
                // Aqui ya no hay nada mas que se pueda hacer.
            }
        }

        /// <summary>
        /// Ejecuta algo y atrapa lo que falle, devolviendo si salio bien. Sirve
        /// para el caso corriente de "haz esto y si se cae, que no se lleve el
        /// hilo por delante".
        /// </summary>
        public static bool Intentar(string donde, Action accion)
        {
            if (accion == null) return false;

            try
            {
                accion();
                return true;
            }
            catch (Exception ex)
            {
                Reportar(donde, ex);
                return false;
            }
        }

        /// <summary>
        /// Dispara un evento sin que un suscriptor roto se lleve por delante a
        /// quien lo disparo.
        ///
        /// Es la version pensada para los eventos del Modelo, y hace falta mas
        /// de lo que parece: StockCambio, NivelCambio o DurabilidadCambio los
        /// escuchan Vistas de Unity, y una Vista a medio destruir al cambiar de
        /// escena puede lanzar. Sin esto, esa excepcion sale por donde se
        /// disparo el evento: si fue un hilo, lo mata; si fue una compra, deja
        /// al jugador con el oro cobrado y la UI a medio refrescar.
        ///
        ///     RegistroDeErrores.Avisar("TiendaArmasModel.StockCambio",
        ///         () =&gt; StockCambio?.Invoke(tipo, StockDe(tipo)));
        /// </summary>
        public static void Avisar(string donde, Action disparar)
        {
            if (disparar == null) return;

            try
            {
                disparar();
            }
            catch (Exception ex)
            {
                Reportar(donde, ex);
            }
        }

        /// <summary>Solo para las pruebas: vuelve a empezar la cuenta.</summary>
        public static void Reiniciar()
        {
            lock (_candado) { _cuantos = 0; }
        }
    }
}
