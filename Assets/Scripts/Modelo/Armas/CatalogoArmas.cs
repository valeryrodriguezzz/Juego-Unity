using System;
using System.Collections.Generic;

namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>
    /// Ficha de datos de un arma: lo que NO cambia durante la partida.
    /// Todas sus propiedades son de solo lectura, por eso se puede consultar
    /// desde varios hilos a la vez SIN lock.
    /// </summary>
    public sealed class InfoArma
    {
        public TipoArma Tipo { get; }
        public string Nombre { get; }
        public int DanoBase { get; }
        public int DurabilidadMaxima { get; }
        public int PrecioOro { get; }
        public bool EsComprable { get; }

        public InfoArma(TipoArma tipo, string nombre, int danoBase,
                        int durabilidadMaxima, int precioOro, bool esComprable)
        {
            Tipo = tipo;
            Nombre = nombre;
            DanoBase = danoBase;
            DurabilidadMaxima = durabilidadMaxima;
            PrecioOro = precioOro;
            EsComprable = esComprable;
        }
    }

    /// <summary>
    /// Tabla central de las armas del juego. Es static y de SOLO LECTURA: se
    /// llena una unica vez en el constructor estatico y nadie la modifica
    /// despues.
    ///
    /// Eso importa para la concurrencia: un diccionario estatico compartido
    /// entre hilos seria peligroso si alguien lo escribiera en tiempo de
    /// ejecucion, pero uno que solo se lee es seguro y no necesita lock.
    /// </summary>
    public static class CatalogoArmas
    {
        private static readonly Dictionary<TipoArma, InfoArma> _fichas;
        private static readonly Dictionary<TipoPersonaje, TipoArma> _armaPorDefecto;
        private static readonly HashSet<TipoPersonaje> _personajesQuePuedenComprar;

        static CatalogoArmas()
        {
            // ---- Las 4 herramientas -----------------------------------------
            // (tipo, nombre, daño, durabilidad, precio en oro, se puede comprar)
            _fichas = new Dictionary<TipoArma, InfoArma>
            {
                { TipoArma.Hacha,    new InfoArma(TipoArma.Hacha,    "Hacha",    12, 60, 30, true) },
                { TipoArma.Pico,     new InfoArma(TipoArma.Pico,     "Pico",     10, 80, 40, true) },
                { TipoArma.Cuchillo, new InfoArma(TipoArma.Cuchillo, "Cuchillo",  8, 40, 20, true) },
                { TipoArma.Martillo, new InfoArma(TipoArma.Martillo, "Martillo", 14, 70, 50, true) }
            };

            // ---- Con que herramienta empieza ---------------------------------
            // CON NINGUNA: el Pawn arranca con las manos vacias y tiene que
            // comprar sus cuatro herramientas en la tienda con el oro inicial.
            // Por eso este diccionario queda vacio. Si algun dia quieres que
            // alguien nazca con algo, se agrega aqui y todo lo demas se adapta solo.
            _armaPorDefecto = new Dictionary<TipoPersonaje, TipoArma>();

            // ---- Quien puede comprar -----------------------------------------
            _personajesQuePuedenComprar = new HashSet<TipoPersonaje>
            {
                TipoPersonaje.Trabajador
            };
        }

        /// <summary>Vista de solo lectura de todas las armas del juego.</summary>
        public static IReadOnlyDictionary<TipoArma, InfoArma> Fichas => _fichas;

        public static InfoArma Ficha(TipoArma tipo)
        {
            if (!_fichas.TryGetValue(tipo, out InfoArma info))
                throw new ArgumentOutOfRangeException(nameof(tipo), "Arma no registrada en el catalogo: " + tipo);

            return info;
        }

        public static string Nombre(TipoArma tipo) => Ficha(tipo).Nombre;

        public static int Precio(TipoArma tipo) => Ficha(tipo).PrecioOro;

        public static bool EsComprable(TipoArma tipo) => Ficha(tipo).EsComprable;

        /// <summary>
        /// Herramienta con la que nace el personaje, si es que nace con alguna.
        /// Devuelve false cuando empieza con las manos vacias, que es el caso
        /// del Pawn: tiene que comprarlas todas.
        /// </summary>
        public static bool TryArmaPorDefecto(TipoPersonaje personaje, out TipoArma arma)
        {
            return _armaPorDefecto.TryGetValue(personaje, out arma);
        }

        /// <summary>true para el personaje que tiene permitido ir a la tienda.</summary>
        public static bool PuedeComprarArmas(TipoPersonaje personaje)
        {
            return _personajesQuePuedenComprar.Contains(personaje);
        }

        /// <summary>Lista de armas que ese personaje puede llegar a comprar.</summary>
        public static List<TipoArma> ArmasComprablesPara(TipoPersonaje personaje)
        {
            var resultado = new List<TipoArma>();
            if (!PuedeComprarArmas(personaje))
                return resultado;

            foreach (var par in _fichas)
            {
                if (par.Value.EsComprable)
                    resultado.Add(par.Key);
            }

            return resultado;
        }

        /// <summary>
        /// Fabrica: crea la instancia concreta que corresponde al tipo.
        /// Es el unico lugar del proyecto donde se hace "new HachaModel()",
        /// para que el resto del codigo trabaje siempre contra ArmaModel.
        /// </summary>
        public static ArmaModel Crear(TipoArma tipo)
        {
            switch (tipo)
            {
                case TipoArma.Hacha:    return new HachaModel();
                case TipoArma.Pico:     return new PicoModel();
                case TipoArma.Cuchillo: return new CuchilloModel();
                case TipoArma.Martillo: return new MartilloModel();
                default:
                    throw new ArgumentOutOfRangeException(nameof(tipo), "No hay clase concreta para: " + tipo);
            }
        }
    }
}
