using System;
using System.Collections.Generic;

namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>
    /// El arsenal de UN jugador: que armas posee, cual lleva equipada y si su
    /// personaje tiene permitido comprar mas.
    ///
    /// Clase C# pura (Modelo). Thread-safe con un lock propio, porque la puede
    /// tocar el hilo del juego (equipar, golpear) y, en el caso de la IA, su
    /// propio hilo de decisiones.
    /// </summary>
    public sealed class InventarioArmasModel
    {
        private readonly object _candado = new object();
        private readonly Dictionary<TipoArma, ArmaModel> _armas = new Dictionary<TipoArma, ArmaModel>();
        private ArmaModel _equipada;

        public TipoPersonaje Personaje { get; }

        /// <summary>true solo para el personaje que puede ir a la tienda.</summary>
        public bool PuedeComprar { get; }

        /// <summary>
        /// Avisa que se cambio de arma. Se puede disparar desde cualquier hilo,
        /// asi que el Controlador debe encolarlo y atenderlo en Update().
        /// </summary>
        public event Action<ArmaModel> ArmaEquipadaCambio;

        public InventarioArmasModel(TipoPersonaje personaje)
        {
            Personaje = personaje;
            PuedeComprar = CatalogoArmas.PuedeComprarArmas(personaje);

            // El Pawn empieza con las manos vacias: Equipada se queda en null
            // hasta que compre su primera herramienta. Todo el codigo que la
            // usa comprueba el null, asi que se puede jugar sin nada equipado
            // (solo que recolectar a mano no rinde).
            if (CatalogoArmas.TryArmaPorDefecto(personaje, out TipoArma inicial))
            {
                ArmaModel arma = CatalogoArmas.Crear(inicial);

                _armas[inicial] = arma;
                _equipada = arma;
                _equipada.IniciarMantenimiento(); // arranca su hilo
            }
        }

        /// <summary>Arma que el personaje lleva puesta en este momento.</summary>
        public ArmaModel Equipada
        {
            get { lock (_candado) { return _equipada; } }
        }

        public bool Posee(TipoArma tipo)
        {
            lock (_candado) { return _armas.ContainsKey(tipo); }
        }

        /// <summary>Copia de las armas que posee (copia, para no exponer el diccionario interno).</summary>
        public List<ArmaModel> Listar()
        {
            lock (_candado) { return new List<ArmaModel>(_armas.Values); }
        }

        /// <summary>
        /// Regla del juego: solo el personaje comprador puede adquirir armas,
        /// solo las 4 que estan marcadas como comprables, y solo si no la tiene ya.
        /// </summary>
        public bool PuedeAdquirir(TipoArma tipo)
        {
            if (!PuedeComprar) return false;
            if (!CatalogoArmas.EsComprable(tipo)) return false;

            lock (_candado) { return !_armas.ContainsKey(tipo); }
        }

        /// <summary>
        /// Mete un arma al inventario. Lo usa la tienda despues de cobrar.
        /// Devuelve false si ya la tenia.
        /// </summary>
        public bool Agregar(ArmaModel arma)
        {
            if (arma == null) throw new ArgumentNullException(nameof(arma));

            lock (_candado)
            {
                if (_armas.ContainsKey(arma.Tipo))
                    return false;

                _armas[arma.Tipo] = arma;

                // Si es la primera que consigue, se le equipa sola: seria raro
                // comprar el hacha y tener que ir a la mochila a ponersela.
                if (_equipada == null)
                {
                    _equipada = arma;
                    arma.IniciarMantenimiento();
                }

                return true;
            }
        }

        /// <summary>
        /// Cambia el arma equipada: apaga el hilo de la anterior y prende el de la nueva.
        /// Asi solo hay UN hilo de mantenimiento vivo por jugador.
        /// </summary>
        public bool Equipar(TipoArma tipo)
        {
            ArmaModel anterior;
            ArmaModel nueva;

            lock (_candado)
            {
                if (!_armas.TryGetValue(tipo, out nueva))
                    return false; // no la tiene

                if (ReferenceEquals(nueva, _equipada))
                    return true;  // ya la lleva puesta

                anterior = _equipada;
                _equipada = nueva;
            }

            // Los hilos se manejan FUERA del lock: DetenerMantenimiento hace Join
            // y no queremos quedarnos esperando con el candado tomado.
            anterior?.DetenerMantenimiento();
            nueva.IniciarMantenimiento();

            RegistroDeErrores.Avisar("InventarioArmasModel.ArmaEquipadaCambio",
                () => ArmaEquipadaCambio?.Invoke(nueva));
            return true;
        }

        /// <summary>
        /// Apaga los hilos de TODAS las armas. Llamalo desde OnDestroy /
        /// OnApplicationQuit del Controlador, o al terminar la partida.
        /// </summary>
        public void DetenerTodo()
        {
            List<ArmaModel> copia;
            lock (_candado) { copia = new List<ArmaModel>(_armas.Values); }

            foreach (ArmaModel arma in copia)
                arma.DetenerMantenimiento();
        }
    }
}
