namespace ImperiosEnGuerra.Modelo.Edificios
{
    // ═══════════════════════════════════════════════════════════════════
    //  EDIFICIOS DE GRECIA
    //
    //  Cada edificio productor tiene un hilo asociado en RecursoController
    //  que le suma recursos al jugador cada cierto tiempo.
    // ═══════════════════════════════════════════════════════════════════

    /// Acrópolis: edificio principal de Grecia. Si es destruida, Grecia pierde la partida.
    /// Comienza ya construida al inicio del juego.
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
            RecursoQueProduce      = null;  // No produce recursos
            ProduccionPorSegundo   = 0;
        }
    }

    /// Cuartel: permite entrenar Hoplitas y Arqueros.
    /// No produce recursos, pero es necesario para el ejército.
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
            RecursoQueProduce      = null;
            ProduccionPorSegundo   = 0;
        }
    }

    /// Granja: produce Comida automáticamente.
    /// HILO: suma Comida al jugador cada segundo.
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
            RecursoQueProduce      = TipoRecurso.Comida;  // ← Produce Comida
            ProduccionPorSegundo   = 5;
        }
    }

    /// Armería: produce Armas automáticamente.
    /// HILO: suma Armas al jugador cada segundo.
    /// Las Armas son necesarias para entrenar Hoplitas y Arqueros.
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
            RecursoQueProduce      = TipoRecurso.Armas;   // ← Produce Armas
            ProduccionPorSegundo   = 3;
        }
    }

    /// Mina de Oro: produce Oro automáticamente.
    /// HILO: suma Oro al jugador cada segundo.
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
            CostoOro               = 0;     // No cuesta Oro (lógico)
            CostoMadera            = 80;
            RecursoQueProduce      = TipoRecurso.Oro;     // ← Produce Oro
            ProduccionPorSegundo   = 4;
        }
    }

    /// Aserradero: produce Madera automáticamente.
    /// HILO: suma Madera al jugador cada segundo.
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
            CostoMadera            = 0;     // No cuesta Madera (lógico)
            RecursoQueProduce      = TipoRecurso.Madera;  // ← Produce Madera
            ProduccionPorSegundo   = 4;
        }
    }

    // ═══════════════════════════════════════════════════
    //  EDIFICIO DEL IMPERIO PERSA (IA)
    // ═══════════════════════════════════════════════════

    /// Palacio Persa: Centro Urbano de la IA.
    /// Si es destruido, la IA pierde.
    public class PalacioPersaModel : EdificioModel
    {
        public PalacioPersaModel()
        {
            Nombre                 = "Palacio Persa";
            Vida                   = 600;
            VidaMax                = 600;
            Construido             = true;
            PorcentajeConstruccion = 100f;
            TiempoConstruccionSeg  = 0;
            CostoOro               = 0;
            CostoMadera            = 0;
            RecursoQueProduce      = null;
            ProduccionPorSegundo   = 0;
        }
    }
}
