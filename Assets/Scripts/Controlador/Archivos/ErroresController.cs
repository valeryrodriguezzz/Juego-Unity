using System;
using ImperiosEnGuerra.Modelo;
using UnityEngine;

namespace ImperiosEnGuerra.Controlador
{
    /// <summary>
    /// CONTROLADOR que recoge los errores atrapados en los hilos y los deja
    /// donde se puedan ver: la consola de Unity, el archivo log_partida.txt y,
    /// si el error es del hilo principal, un aviso en pantalla.
    ///
    /// El Modelo no puede hacer esto por su cuenta porque es C# puro y no
    /// conoce Unity. Por eso RegistroDeErrores solo avisa y este Controlador
    /// escucha.
    ///
    /// OJO CON EL HILO
    /// El aviso llega desde el hilo que fallo, que casi nunca es el principal.
    /// Debug.LogError y ArchivoController se pueden llamar desde cualquier
    /// hilo, pero AvisoPantalla crea objetos de Unity y NO: por eso el texto
    /// se guarda en una variable volatile y lo muestra Update, que si corre en
    /// el hilo principal.
    ///
    /// NO HAY QUE MONTAR NADA EN UNITY: se crea solo al arrancar el juego.
    /// </summary>
    public class ErroresController : MonoBehaviour
    {
        private static ErroresController _instancia;

        [Tooltip("Mostrar tambien un aviso en pantalla al jugador.")]
        private const bool AVISAR_EN_PANTALLA = true;

        private volatile string _pendiente;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CrearSiNoExiste()
        {
            if (_instancia != null) return;

            var go = new GameObject("ErroresController");
            go.AddComponent<ErroresController>();
        }

        private void Awake()
        {
            if (_instancia != null && _instancia != this) { Destroy(gameObject); return; }

            _instancia = this;
            DontDestroyOnLoad(gameObject);

            RegistroDeErrores.Ocurrio += Anotar;
        }

        private void OnDestroy()
        {
            if (_instancia == this) RegistroDeErrores.Ocurrio -= Anotar;
        }

        /// <summary>Llega desde el hilo que fallo, no desde el principal.</summary>
        private void Anotar(string donde, Exception error)
        {
            string resumen = error.GetType().Name + ": " + error.Message;

            // Los dos son seguros desde cualquier hilo: Debug lo es por
            // diseño, y ArchivoController tiene su propio candado.
            Debug.LogError("[Error atrapado] " + donde + " -> " + resumen +
                           "\n" + error.StackTrace);

            ArchivoController.RegistrarAccion(
                RegistroDeErrores.Cuantos, "Sistema",
                "Error atrapado en " + donde, resumen);

            if (AVISAR_EN_PANTALLA) _pendiente = "Ups: fallo " + donde + ".";
        }

        private void Update()
        {
            if (_pendiente == null) return;

            string texto = _pendiente;
            _pendiente = null;

            AvisoPantalla.Mostrar(texto);
        }
    }
}
