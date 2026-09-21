using System;
using System.Collections.Generic;

namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>
    /// Ficha de datos de un arma: lo que NO cambia durante la partida.
    /// Es una clase de solo lectura (todas las propiedades tienen get privado sin set),
    /// por eso se puede leer desde varios hilos a la vez SIN lock.
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
    /// Tabla central de armas del juego. Es static y de SOLO LECTURA: se llena una
    /// unica vez en el constructor estatico y despues nadie la modifica.
    ///
    /// Esto es importante para la concurrencia: un diccionario estatico compartido
    /// entre hilos seria peligroso si alguien lo escribiera en tiempo de ejecucion,
    /// pero un diccionario que solo se lee es completamente seguro y no necesita lock.
    /// Por eso se expone como IReadOnlyDictionary y NUNCA se agrega nada despues.
    /// </summary>
    public static class CatalogoArmas
    {
        private static readonly Dictionary<TipoArma, InfoArma> _fichas;
        private static readonly Dictionary<TipoPersonaje, TipoArma> _armaPorDefecto;
        private static readonly HashSet<TipoPersonaje> _personajesQuePuedenComprar;

        static CatalogoArmas()
        {
            // ---- Las 7 armas -------------------------------------------------
            // (tipo, nombre, dano, durabilidad, precio en oro, se puede comprar)
            _fichas = new Dictionary<TipoArma, InfoArma>
            {
                { TipoArma.Hacha,    new InfoArma(TipoArma.Hacha,    "Hacha",    12, 60, 30, true)  },
                { TipoArma.Pico,     new InfoArma(TipoArma.Pico,     "Pico",     10, 80, 40, true)  },
                { TipoArma.Cuchillo, new InfoArma(TipoArma.Cuchillo, "Cuchillo",  8, 40, 20, true)  },
                { TipoArma.Martillo, new InfoArma(TipoArma.Martillo, "Martillo", 14, 70, 50, true)  },
 
                // Estas tres NO se venden: precio 0 y EsComprable = false.
                { TipoArma.Lanza,    new InfoArma(TipoArma.Lanza,    "Lanza",    16, 50,  0, false) },
                { TipoArma.Espada,   new InfoArma(TipoArma.Espada,   "Espada",   20, 60,  0, false) },
                { TipoArma.Arco,     new InfoArma(TipoArma.Arco,     "Arco",     15, 45,  0, false) }
            };

            // ---- Con que arma empieza cada personaje -------------------------
            // AJUSTA esta tabla a tus 5 personajes reales.
            _armaPorDefecto = new Dictionary<TipoPersonaje, TipoArma>
            {
                { TipoPersonaje.Trabajador, TipoArma.Hacha    }, // el unico que despues puede comprar
                { TipoPersonaje.Guerrero,   TipoArma.Espada   },
                { TipoPersonaje.Arquero,    TipoArma.Arco     },
                { TipoPersonaje.Lancero,    TipoArma.Lanza    },
                { TipoPersonaje.Cazador,    TipoArma.Cuchillo }
            };

            // ---- Quien puede comprar en la tienda ----------------------------
            // Solo el Trabajador. Los demas se quedan con su arma de por vida.
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

        /// <summary>Arma con la que nace el personaje.</summary>
        public static TipoArma ArmaPorDefecto(TipoPersonaje personaje)
        {
            return _armaPorDefecto.TryGetValue(personaje, out TipoArma arma)
                ? arma
                : TipoArma.Cuchillo;
        }

        /// <summary>true solo para el personaje que tiene permitido ir a la tienda.</summary>
        public static bool PuedeComprarArmas(TipoPersonaje personaje)
        {
            return _personajesQuePuedenComprar.Contains(personaje);
        }

        /// <summary>
        /// Lista de armas que ESE personaje puede llegar a comprar.
        /// Para los personajes con arma fija devuelve una lista vacia,
        /// asi el Controlador puede simplemente no mostrarles la tienda.
        /// </summary>
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
        /// Es el unico lugar del proyecto donde se hace "new HachaModel()", etc.,
        /// para que el resto del codigo trabaje siempre contra ArmaModel (polimorfismo).
        /// </summary>
        public static ArmaModel Crear(TipoArma tipo)
        {
            switch (tipo)
            {
                case TipoArma.Hacha: return new HachaModel();
                case TipoArma.Pico: return new PicoModel();
                case TipoArma.Cuchillo: return new CuchilloModel();
                case TipoArma.Martillo: return new MartilloModel();
                case TipoArma.Lanza: return new LanzaModel();
                case TipoArma.Espada: return new EspadaModel();
                case TipoArma.Arco: return new ArcoModel();
                default:
                    throw new ArgumentOutOfRangeException(nameof(tipo), "No hay clase concreta para: " + tipo);
            }
        }
    }
}