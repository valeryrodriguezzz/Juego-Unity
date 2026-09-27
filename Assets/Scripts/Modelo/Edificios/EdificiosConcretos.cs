namespace ImperiosEnGuerra.Modelo.Edificios
{
    // ═══════════════════════════════════════════════════════════════════
    //  EDIFICIOS DE GRECIA
    //
    //  Cada edificio declara su costo, tiempo de construccion y (si es
    //  productor) que recurso produce y cuanto por segundo.
    //  Los HILOS estan en EdificioModel (Modelo): uno construye el edificio
    //  y, al terminar, otro suma el recurso al jugador cada segundo.
    //  Se usan con jugador.Construir(edificio). El Controlador solo lo pide
    //  y llama jugador.DetenerEdificios() al cerrar.
    // ═══════════════════════════════════════════════════════════════════

    // Acrópolis: edificio principal de Grecia. Si es destruida, Grecia pierde la partida.
    // Comienza ya construida al inicio del juego.
    public class AcropolisModel : EdificioModel
    {
        public AcropolisModel()
        {
            Nombre                 = "Acrópolis";
            Vida                   = 600;
            VidaMax                = 600;
            Construido             = true;
            PorcentajeConstruccion = 100f;
            TiempoConstruccionSeg  = 0;
            CostoOro               = 0;
            CostoMadera            = 0;
            RecursoQueProduce      = "";   // No produce recursos
            ProduccionPorSegundo   = 0;
        }
    }

    // Cuartel: permite entrenar unidades.
    // No produce recursos, pero es necesario para el ejército.
    public class CuartelModel : EdificioModel
    {
        public CuartelModel()
        {
            Nombre                 = "Cuartel";
            Vida                   = 300;
            VidaMax                = 300;
            Construido             = false;
            PorcentajeConstruccion = 0f;
            TiempoConstruccionSeg  = 20;
            CostoOro               = 120;
            CostoMadera            = 80;
            RecursoQueProduce      = "";
            ProduccionPorSegundo   = 0;
        }
    }

    // Granja: produce Comida automáticamente.
    // HILO: suma Comida al jugador cada segundo.
    public class GranjaModel : EdificioModel
    {
        public GranjaModel()
        {
            Nombre                 = "Granja";
            Vida                   = 200;
            VidaMax                = 200;
            Construido             = false;
            PorcentajeConstruccion = 0f;
            TiempoConstruccionSeg  = 15;
            CostoOro               = 50;
            CostoMadera            = 100;
            RecursoQueProduce      = "Comida";
            ProduccionPorSegundo   = 5;
        }
    }

    // Armería: produce Armas automáticamente.
    // HILO: suma Armas al jugador cada segundo.
    public class ArmeriaModel : EdificioModel
    {
        public ArmeriaModel()
        {
            Nombre                 = "Armería";
            Vida                   = 250;
            VidaMax                = 250;
            Construido             = false;
            PorcentajeConstruccion = 0f;
            TiempoConstruccionSeg  = 25;
            CostoOro               = 150;
            CostoMadera            = 120;
            RecursoQueProduce      = "Armas";
            ProduccionPorSegundo   = 3;
        }
    }

    // Mina de Oro: produce Oro automáticamente.
    // HILO: suma Oro al jugador cada segundo.
    public class MinaModel : EdificioModel
    {
        public MinaModel()
        {
            Nombre                 = "Mina de Oro";
            Vida                   = 180;
            VidaMax                = 180;
            Construido             = false;
            PorcentajeConstruccion = 0f;
            TiempoConstruccionSeg  = 18;
            CostoOro               = 0;
            CostoMadera            = 80;
            RecursoQueProduce      = "Oro";
            ProduccionPorSegundo   = 4;
        }
    }

    // Aserradero: produce Madera automáticamente.
    // HILO: suma Madera al jugador cada segundo.
    public class AserraderoModel : EdificioModel
    {
        public AserraderoModel()
        {
            Nombre                 = "Aserradero";
            Vida                   = 180;
            VidaMax                = 180;
            Construido             = false;
            PorcentajeConstruccion = 0f;
            TiempoConstruccionSeg  = 18;
            CostoOro               = 40;
            CostoMadera            = 0;
            RecursoQueProduce      = "Madera";
            ProduccionPorSegundo   = 4;
        }
    }


    //  IGLESIA (jugador)

    // Iglesia: permite al jugador curarse (sin superar su VidaMax).
    // Cada uso cuesta oro y tiene un tiempo de espera para que no sea curacion infinita.
    // No necesita hilo: el jugador la usa con un boton. Es segura entre hilos porque Curarse y GastarSiAlcanza
    // de JugadorModel ya son atomicos(se ejecuta por completo de principio a fin sin interrupciones, o no se ejecuta en absoluto).
    public class IglesiaModel : EdificioModel
    {
        private readonly object _lock = new object();
        private System.DateTime _ultimoUso = System.DateTime.MinValue;

        public int CuracionPorUso { get; private set; }
        public int CostoOroPorUso { get; private set; }
        public int EsperaSeg { get; private set; }

        public IglesiaModel()
        {
            Nombre                 = "Iglesia";
            Vida                   = 250;
            VidaMax                = 250;
            Construido             = false;
            PorcentajeConstruccion = 0f;
            TiempoConstruccionSeg  = 20;
            CostoOro               = 100;
            CostoMadera            = 60;
            RecursoQueProduce      = "";
            ProduccionPorSegundo   = 0;

            CuracionPorUso = 30;
            CostoOroPorUso = 20;
            EsperaSeg      = 10;
        }

        /// <summary>
        /// Permite que cada iglesia del mapa tenga sus propios numeros sin
        /// tocar esta clase: la del monasterio de Roma puede curar mas caro
        /// que la que construye el jugador en su base.
        /// </summary>
        public void Configurar(int curacionPorUso, int costoOroPorUso, int esperaSeg)
        {
            lock (_lock)
            {
                if (curacionPorUso > 0) CuracionPorUso = curacionPorUso;
                if (costoOroPorUso >= 0) CostoOroPorUso = costoOroPorUso;
                if (esperaSeg >= 0) EsperaSeg = esperaSeg;
            }
        }

        // Segundos que faltan para poder usarla otra vez (0 = disponible)
        public double SegundosParaPoderUsar()
        {
            lock (_lock)
            {
                double faltan = EsperaSeg - (System.DateTime.UtcNow - _ultimoUso).TotalSeconds;
                return faltan > 0 ? faltan : 0;
            }
        }

        // Cura al jugador. Devuelve false si no esta construida, sigue en espera,
        // el jugador ya tiene la vida completa o no le alcanza el oro.
        public bool Curar(JugadorModel jugador)
        {
            if (jugador == null || !Construido) return false;

            lock (_lock)
            {
                if ((System.DateTime.UtcNow - _ultimoUso).TotalSeconds < EsperaSeg) return false;
                if (jugador.Vida >= jugador.VidaMax) return false;
                if (!jugador.GastarSiAlcanza(monedas: CostoOroPorUso)) return false;

                jugador.Curarse(CuracionPorUso);
                _ultimoUso = System.DateTime.UtcNow;
                return true;
            }
        }
    }

    // ═══════════════════════════════════════════════════
    //  FABRICA
    //
    //  Convierte una clave de texto en el edificio que toca. La usa el panel
    //  de construccion y, sobre todo, la carga de partidas guardadas: en el
    //  archivo solo cabe el nombre, y de ahi hay que volver a sacar el objeto
    //  con sus hilos.
    //
    //  La clave NO es el Nombre del edificio: "Mina de Oro" lleva espacios y
    //  "Armería" lleva tilde, y las dos cosas se rompen al viajar por un
    //  archivo de texto. Se usa una palabra sin adornos.
    // ═══════════════════════════════════════════════════

    public static class FabricaEdificios
    {
        public static EdificioModel Crear(string clave)
        {
            if (string.IsNullOrEmpty(clave)) return null;

            switch (clave.Trim().ToLowerInvariant())
            {
                case "granja":      return new GranjaModel();
                case "mina":        return new MinaModel();
                case "aserradero":  return new AserraderoModel();
                case "armeria":     return new ArmeriaModel();
                case "iglesia":     return new IglesiaModel();
                case "cuartel":     return new CuartelModel();
                default:            return null;
            }
        }

        public static string Clave(EdificioModel edificio)
        {
            if (edificio is GranjaModel)     return "granja";
            if (edificio is MinaModel)       return "mina";
            if (edificio is AserraderoModel) return "aserradero";
            if (edificio is ArmeriaModel)    return "armeria";
            if (edificio is IglesiaModel)    return "iglesia";
            if (edificio is CuartelModel)    return "cuartel";
            return "";
        }
    }

    // ═══════════════════════════════════════════════════
    //  EDIFICIO PRINCIPAL DE LA IA (cualquier territorio)
    // ═══════════════════════════════════════════════════

    // Centro Urbano generico de la IA. Su nombre y su vida dependen del territorio
    // (ver TerritorioModel). Si es destruido, el jugador conquista el territorio.
    public class CentroUrbanoModel : EdificioModel
    {
        public CentroUrbanoModel(string nombre, int vida)
        {
            Nombre                 = nombre;
            Vida                   = vida;
            VidaMax                = vida;
            Construido             = true;
            PorcentajeConstruccion = 100f;
            TiempoConstruccionSeg  = 0;
            CostoOro               = 0;
            CostoMadera            = 0;
            RecursoQueProduce      = "";
            ProduccionPorSegundo   = 0;
        }
    }
}

