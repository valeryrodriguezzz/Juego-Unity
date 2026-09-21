using System;
using System.Collections.Generic;
using System.Threading;

namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>Por que salio bien o mal una compra. La UI muestra el mensaje segun esto.</summary>
    public enum ResultadoCompra
    {
        Exito,
        PersonajeNoPuedeComprar, // no es el Trabajador
        ArmaNoComprable,         // lanza / espada / arco
        YaLaPosee,
        SinStock,
        OroInsuficiente
    }

    /// <summary>
    /// La tienda donde se venden las 4 armas comprables.
    ///
    /// CONCURRENCIA: la tienda tiene su PROPIO hilo de reabastecimiento que le va
    /// devolviendo stock cada cierto tiempo (el herrero sigue fabricando aunque el
    /// jugador este del otro lado del mapa). Ese hilo escribe el diccionario de
    /// stock mientras el jugador puede estar comprando, por eso todo va con lock.
    ///
    /// Es la misma idea de RecursoModel.IniciarRegeneracion(), aplicada a la tienda.
    /// </summary>
    public sealed class TiendaArmasModel
    {
        private readonly object _candado = new object();
        private readonly Dictionary<TipoArma, int> _stock = new Dictionary<TipoArma, int>();

        private Thread _hiloReabastecimiento;
        private volatile bool _reabasteciendo;
        private readonly ManualResetEventSlim _senalParar = new ManualResetEventSlim(false);

        public int StockMaximoPorArma { get; }
        public int MsEntreReabastecimientos { get; }

        /// <summary>Avisa que cambio el stock de un arma. Puede venir de otro hilo.</summary>
        public event Action<TipoArma, int> StockCambio;

        public TiendaArmasModel(int stockMaximoPorArma = 3, int msEntreReabastecimientos = 8000)
        {
            StockMaximoPorArma = Math.Max(1, stockMaximoPorArma);
            MsEntreReabastecimientos = Math.Max(500, msEntreReabastecimientos);

            // La tienda solo maneja las armas marcadas como comprables.
            foreach (var par in CatalogoArmas.Fichas)
            {
                if (par.Value.EsComprable)
                    _stock[par.Key] = StockMaximoPorArma;
            }
        }

        public int StockDe(TipoArma tipo)
        {
            lock (_candado)
            {
                return _stock.TryGetValue(tipo, out int cantidad) ? cantidad : 0;
            }
        }

        public List<TipoArma> ArmasEnVenta()
        {
            lock (_candado) { return new List<TipoArma>(_stock.Keys); }
        }

        // -------------------------------------------------------------------
        //  COMPRA
        // -------------------------------------------------------------------

        /// <summary>
        /// Intenta venderle un arma al jugador.
        ///
        /// Aqui esta el punto delicado de concurrencia: la compra toca DOS estados
        /// compartidos que tienen candados distintos (el stock de la tienda y el oro
        /// del JugadorModel). Si un hilo tomara primero el de la tienda y otro primero
        /// el del jugador, se traban mutuamente (deadlock).
        ///
        /// Solucion usada: nunca se sostienen los dos candados a la vez. Se hace una
        /// transaccion en tres pasos:
        ///   1. RESERVAR el stock bajo el lock de la tienda, y soltarlo.
        ///   2. COBRAR el oro (JugadorModel usa su propio lock, ya sin el nuestro).
        ///   3. Si el cobro falla, DEVOLVER la reserva (compensacion).
        /// </summary>
        public ResultadoCompra Comprar(JugadorModel jugador, InventarioArmasModel inventario, TipoArma tipo)
        {
            if (jugador == null) throw new ArgumentNullException(nameof(jugador));
            if (inventario == null) throw new ArgumentNullException(nameof(inventario));

            // --- Validaciones de reglas del juego (no tocan estado compartido) ---
            if (!inventario.PuedeComprar)
                return ResultadoCompra.PersonajeNoPuedeComprar;

            if (!CatalogoArmas.EsComprable(tipo))
                return ResultadoCompra.ArmaNoComprable;

            if (inventario.Posee(tipo))
                return ResultadoCompra.YaLaPosee;

            // --- Paso 1: reservar el stock ---
            lock (_candado)
            {
                if (!_stock.TryGetValue(tipo, out int cantidad) || cantidad <= 0)
                    return ResultadoCompra.SinStock;

                _stock[tipo] = cantidad - 1;
            }

            StockCambio?.Invoke(tipo, StockDe(tipo));

            // --- Paso 2: cobrar el oro (ya sin nuestro candado) ---
            int precio = CatalogoArmas.Precio(tipo);

            if (!CobrarOro(jugador, precio))
            {
                DevolverStock(tipo); // paso 3: compensacion
                return ResultadoCompra.OroInsuficiente;
            }

            // --- Entregar el arma ---
            ArmaModel arma = CatalogoArmas.Crear(tipo);

            if (!inventario.Agregar(arma))
            {
                // Carrera rarisima: alguien se la agrego entre la validacion y aqui.
                // Devolvemos el oro y el stock para no dejar al jugador robado.
                jugador.AgregarRecurso(TipoRecurso.Oro, precio);
                DevolverStock(tipo);
                return ResultadoCompra.YaLaPosee;
            }

            return ResultadoCompra.Exito;
        }

        private void DevolverStock(TipoArma tipo)
        {
            lock (_candado)
            {
                if (_stock.TryGetValue(tipo, out int cantidad))
                    _stock[tipo] = Math.Min(StockMaximoPorArma, cantidad + 1);
            }

            StockCambio?.Invoke(tipo, StockDe(tipo));
        }

        /// <summary>
        /// Cobra el precio del arma.
        ///
        /// Usa GastarSiAlcanza (el metodo [8] que se agrego a JugadorModel) y NO
        /// la pareja TieneRecursos + Gasto_Recursos. Esas son dos operaciones
        /// separadas: dos hilos podrian pasar los dos por el TieneRecursos con
        /// el mismo oro en la bolsa y cobrar ambos. GastarSiAlcanza comprueba y
        /// descuenta dentro del mismo lock, asi que o cobra completo o no cobra.
        /// </summary>
        private static bool CobrarOro(JugadorModel jugador, int precio)
        {
            return jugador.GastarSiAlcanza(precio);
        }

        // -------------------------------------------------------------------
        //  HILO DE REABASTECIMIENTO
        // -------------------------------------------------------------------

        public void IniciarReabastecimiento()
        {
            lock (_candado)
            {
                if (_reabasteciendo)
                    return;

                _reabasteciendo = true;
                _senalParar.Reset();

                _hiloReabastecimiento = new Thread(BucleReabastecimiento)
                {
                    IsBackground = true, // no deja colgado el editor de Unity
                    Name = "Tienda-Reabastecimiento"
                };

                _hiloReabastecimiento.Start();
            }
        }

        public void DetenerReabastecimiento()
        {
            Thread hilo;

            lock (_candado)
            {
                if (!_reabasteciendo)
                    return;

                _reabasteciendo = false;
                hilo = _hiloReabastecimiento;
                _hiloReabastecimiento = null;
            }

            _senalParar.Set();

            if (hilo != null && hilo.IsAlive)
                hilo.Join(500);
        }

        private void BucleReabastecimiento()
        {
            while (_reabasteciendo)
            {
                if (_senalParar.Wait(MsEntreReabastecimientos))
                    break;

                var repuestas = new List<TipoArma>();

                lock (_candado)
                {
                    // Se recorre una copia de las llaves porque dentro del foreach
                    // se modifica el diccionario.
                    var llaves = new List<TipoArma>(_stock.Keys);

                    foreach (TipoArma tipo in llaves)
                    {
                        if (_stock[tipo] < StockMaximoPorArma)
                        {
                            _stock[tipo]++;
                            repuestas.Add(tipo);
                        }
                    }
                }

                // Los eventos se disparan fuera del lock.
                foreach (TipoArma tipo in repuestas)
                    StockCambio?.Invoke(tipo, StockDe(tipo));
            }
        }

        /// <summary>Mensaje listo para mostrarle al jugador en la UI.</summary>
        public static string Mensaje(ResultadoCompra resultado, TipoArma tipo)
        {
            string nombre = CatalogoArmas.Nombre(tipo);

            switch (resultado)
            {
                case ResultadoCompra.Exito: return "Compraste: " + nombre;
                case ResultadoCompra.PersonajeNoPuedeComprar: return "Tu personaje no puede cambiar de arma.";
                case ResultadoCompra.ArmaNoComprable: return nombre + " no esta a la venta.";
                case ResultadoCompra.YaLaPosee: return "Ya tienes " + nombre + ".";
                case ResultadoCompra.SinStock: return "No queda " + nombre + " en la tienda.";
                case ResultadoCompra.OroInsuficiente: return "Te falta oro para " + nombre + ".";
                default: return "";
            }
        }
    }
}