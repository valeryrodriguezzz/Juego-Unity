using System;
using System.Collections.Generic;
using System.Threading;

namespace ImperiosEnGuerra.Modelo
{
    /// <summary>Por que salio bien o mal la compra de un lote de provisiones.</summary>
    public enum ResultadoCompraRecurso
    {
        Exito,
        NoEstaEnVenta,
        SinStock,
        OroInsuficiente
    }

    /// <summary>
    /// Una cosa que vende el mercader: que recurso es, cuantas unidades entrega
    /// por compra y cuanto oro cuesta el lote.
    ///
    /// Se venden por LOTES y no de a una unidad para que comprar no sea dar
    /// treinta clics, y para que se note la diferencia entre ir a recolectar
    /// (gratis pero lento) y comprar (rapido pero cuesta oro).
    /// </summary>
    public sealed class OfertaRecurso
    {
        public TipoRecurso Recurso { get; }
        public string Nombre { get; }
        public int Unidades { get; }
        public int Precio { get; }

        public OfertaRecurso(TipoRecurso recurso, string nombre, int unidades, int precio)
        {
            Recurso = recurso;
            Nombre = nombre;
            Unidades = unidades;
            Precio = precio;
        }
    }

    /// <summary>
    /// MODELO de la parte de provisiones de la tienda: carne y madera a cambio
    /// de oro. Clase de C# pura, sin nada de Unity.
    ///
    /// Es la hermana de TiendaArmasModel y funciona igual:
    ///
    /// CONCURRENCIA: tiene su PROPIO hilo de reabastecimiento. El mercader va
    /// reponiendo lotes cada cierto tiempo aunque el jugador este del otro lado
    /// del mapa, asi que ese hilo escribe el diccionario de stock justo mientras
    /// el jugador puede estar comprando desde el hilo de Unity. Por eso todo
    /// acceso al stock va dentro del mismo lock.
    ///
    /// Y por eso la compra NO sostiene dos candados a la vez: reserva el lote
    /// bajo el candado de la tienda, lo suelta, cobra el oro con el candado del
    /// JugadorModel, y si el cobro falla devuelve la reserva. Si en vez de eso
    /// tomara los dos candados, dos hilos que los pidieran en orden contrario
    /// se quedarian trabados para siempre (deadlock).
    /// </summary>
    public sealed class TiendaRecursosModel
    {
        private readonly object _candado = new object();
        private readonly Dictionary<TipoRecurso, OfertaRecurso> _ofertas =
            new Dictionary<TipoRecurso, OfertaRecurso>();
        private readonly Dictionary<TipoRecurso, int> _stock = new Dictionary<TipoRecurso, int>();

        private Thread _hiloReabastecimiento;
        private volatile bool _reabasteciendo;
        private readonly ManualResetEventSlim _senalParar = new ManualResetEventSlim(false);

        public int StockMaximoPorLote { get; }
        public int MsEntreReabastecimientos { get; }

        /// <summary>Avisa que cambio el stock de un lote. Puede venir de otro hilo.</summary>
        public event Action<TipoRecurso, int> StockCambio;

        public TiendaRecursosModel(int stockMaximoPorLote = 5, int msEntreReabastecimientos = 10000)
        {
            StockMaximoPorLote = Math.Max(1, stockMaximoPorLote);
            MsEntreReabastecimientos = Math.Max(500, msEntreReabastecimientos);

            // El catalogo del mercader. Cambiar precios es cambiar estos numeros.
            //
            // Estan pensados contra los 60 de oro del arranque: un lote cuesta
            // menos que una herramienta, asi que al principio conviene gastarse
            // el oro en el hacha o el pico y recolectar, y las provisiones son
            // para cuando ya hay mina produciendo y el hambre aprieta.
            Registrar(new OfertaRecurso(TipoRecurso.Comida, "Carne", 10, 15));
            Registrar(new OfertaRecurso(TipoRecurso.Madera, "Madera", 10, 12));
        }

        private void Registrar(OfertaRecurso oferta)
        {
            _ofertas[oferta.Recurso] = oferta;
            _stock[oferta.Recurso] = StockMaximoPorLote;
        }

        // -------------------------------------------------------------------
        //  CONSULTAS
        // -------------------------------------------------------------------

        public List<OfertaRecurso> EnVenta()
        {
            lock (_candado) { return new List<OfertaRecurso>(_ofertas.Values); }
        }

        public OfertaRecurso OfertaDe(TipoRecurso recurso)
        {
            lock (_candado)
            {
                return _ofertas.TryGetValue(recurso, out OfertaRecurso o) ? o : null;
            }
        }

        public int StockDe(TipoRecurso recurso)
        {
            lock (_candado)
            {
                return _stock.TryGetValue(recurso, out int cantidad) ? cantidad : 0;
            }
        }

        // -------------------------------------------------------------------
        //  COMPRA
        // -------------------------------------------------------------------

        public ResultadoCompraRecurso Comprar(JugadorModel jugador, TipoRecurso recurso)
        {
            if (jugador == null) throw new ArgumentNullException(nameof(jugador));

            OfertaRecurso oferta;

            // --- Paso 1: validar y reservar el lote, todo bajo el mismo candado ---
            lock (_candado)
            {
                if (!_ofertas.TryGetValue(recurso, out oferta))
                    return ResultadoCompraRecurso.NoEstaEnVenta;

                if (!_stock.TryGetValue(recurso, out int cantidad) || cantidad <= 0)
                    return ResultadoCompraRecurso.SinStock;

                _stock[recurso] = cantidad - 1;
            }

            // El evento, ya fuera del lock: quien lo escuche puede hacer
            // cualquier cosa y no queremos que lo haga con el candado puesto.
            RegistroDeErrores.Avisar("TiendaRecursosModel.StockCambio",
                () => StockCambio?.Invoke(recurso, StockDe(recurso)));

            // --- Paso 2: cobrar, con el candado del jugador y no con el nuestro ---
            // GastarSiAlcanza comprueba y descuenta dentro de un solo lock: o
            // cobra completo o no cobra. Con TieneOro + Gastar por separado,
            // dos compras simultaneas podrian pasar ambas la comprobacion con
            // el mismo oro en la bolsa.
            if (!jugador.GastarSiAlcanza(oferta.Precio))
            {
                DevolverStock(recurso); // paso 3: compensacion
                return ResultadoCompraRecurso.OroInsuficiente;
            }

            // --- Entregar la mercancia ---
            jugador.Conseguir_Recursos(RecursoTipoHelper.ATexto(recurso), oferta.Unidades);

            return ResultadoCompraRecurso.Exito;
        }

        private void DevolverStock(TipoRecurso recurso)
        {
            lock (_candado)
            {
                if (_stock.TryGetValue(recurso, out int cantidad))
                    _stock[recurso] = Math.Min(StockMaximoPorLote, cantidad + 1);
            }

            RegistroDeErrores.Avisar("TiendaRecursosModel.StockCambio",
                () => StockCambio?.Invoke(recurso, StockDe(recurso)));
        }

        // -------------------------------------------------------------------
        //  HILO DE REABASTECIMIENTO
        // -------------------------------------------------------------------

        public void IniciarReabastecimiento()
        {
            lock (_candado)
            {
                if (_reabasteciendo) return;

                _reabasteciendo = true;
                _senalParar.Reset();

                _hiloReabastecimiento = new Thread(BucleReabastecimiento)
                {
                    IsBackground = true, // sin esto Unity se queda colgado al salir del Play
                    Name = "Tienda-Provisiones"
                };

                _hiloReabastecimiento.Start();
            }
        }

        public void DetenerReabastecimiento()
        {
            Thread hilo;

            lock (_candado)
            {
                if (!_reabasteciendo) return;

                _reabasteciendo = false;
                hilo = _hiloReabastecimiento;
                _hiloReabastecimiento = null;
            }

            // La señal despierta al hilo al instante en vez de esperar a que se
            // le acabe el Wait. Por eso se usa ManualResetEventSlim y no
            // Thread.Sleep: un Sleep de 10 segundos no se puede interrumpir.
            _senalParar.Set();

            if (hilo != null && hilo.IsAlive)
                hilo.Join(500);
        }

        // Cada vuelta va en try-catch: si una fallara, la excepcion mataria el
        // hilo y la tienda dejaria de reponer carne y madera para el resto de
        // la partida, sin ningun mensaje. Asi el fallo queda registrado y la
        // vuelta siguiente sigue reponiendo.
        private void BucleReabastecimiento()
        {
            while (_reabasteciendo)
            {
                try
                {
                    UnaReposicion();
                }
                catch (Exception ex)
                {
                    RegistroDeErrores.Reportar("TiendaRecursosModel.BucleReabastecimiento", ex);
                }
            }
        }

        private void UnaReposicion()
        {
            if (_senalParar.Wait(MsEntreReabastecimientos))
            {
                _reabasteciendo = false;
                return;
            }

            var repuestos = new List<TipoRecurso>();

            lock (_candado)
            {
                // Copia de las llaves: dentro del foreach se modifica el
                // diccionario y recorrerlo en vivo lanza excepcion.
                var llaves = new List<TipoRecurso>(_stock.Keys);

                foreach (TipoRecurso recurso in llaves)
                {
                    if (_stock[recurso] < StockMaximoPorLote)
                    {
                        _stock[recurso]++;
                        repuestos.Add(recurso);
                    }
                }
            }

            foreach (TipoRecurso recurso in repuestos)
                RegistroDeErrores.Avisar("TiendaRecursosModel.StockCambio",
                () => StockCambio?.Invoke(recurso, StockDe(recurso)));
        }

        // -------------------------------------------------------------------

        /// <summary>Mensaje listo para mostrarle al jugador.</summary>
        public static string Mensaje(ResultadoCompraRecurso resultado, OfertaRecurso oferta)
        {
            string nombre = oferta != null ? oferta.Nombre : "eso";

            switch (resultado)
            {
                case ResultadoCompraRecurso.Exito:
                    return "Compraste " + (oferta != null ? oferta.Unidades + " de " : "") + nombre;
                case ResultadoCompraRecurso.NoEstaEnVenta:
                    return nombre + " no esta a la venta.";
                case ResultadoCompraRecurso.SinStock:
                    return "Se agoto " + nombre + ". El mercader esta reponiendo.";
                case ResultadoCompraRecurso.OroInsuficiente:
                    return "Te falta oro para " + nombre + ".";
                default:
                    return "";
            }
        }
    }
}
