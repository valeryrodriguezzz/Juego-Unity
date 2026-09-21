namespace ImperiosEnGuerra.Modelo.Armas
{
    // =====================================================================
    //  ARMAS COMPRABLES (herramientas) - solo el Trabajador las adquiere
    // =====================================================================

    /// <summary>Hacha: la herramienta de la madera.</summary>
    public sealed class HachaModel : ArmaModel
    {
        public HachaModel() : base(TipoArma.Hacha)
        {
            MsEntreReparaciones = 2000;
        }

        public override float MultiplicadorRecoleccion(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Madera: return 2.0f;  // talar arboles: lo suyo
                case TipoRecurso.Comida: return 0.75f; // sirve, pero es tosca
                default: return 0.25f; // a duras penas
            }
        }

        public override float MultiplicadorCombate => 0.9f;
    }

    /// <summary>Pico: la herramienta del oro.</summary>
    public sealed class PicoModel : ArmaModel
    {
        public PicoModel() : base(TipoArma.Pico)
        {
            MsEntreReparaciones = 2500;
        }

        public override float MultiplicadorRecoleccion(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Oro: return 2.0f; // minar roca: lo suyo
                default: return 0.25f;
            }
        }

        public override float MultiplicadorCombate => 0.7f;
    }

    /// <summary>Cuchillo: la herramienta de la comida.</summary>
    public sealed class CuchilloModel : ArmaModel
    {
        public CuchilloModel() : base(TipoArma.Cuchillo)
        {
            MsEntreReparaciones = 1500; // pequeño y facil de afilar
        }

        public override float MultiplicadorRecoleccion(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Comida: return 2.0f; // matar ovejas: lo suyo
                default: return 0.25f;
            }
        }

        public override float MultiplicadorCombate => 1.0f;
    }

    /// <summary>Martillo: la herramienta de la construccion.</summary>
    public sealed class MartilloModel : ArmaModel
    {
        public MartilloModel() : base(TipoArma.Martillo)
        {
            MsEntreReparaciones = 3000;
        }

        public override float MultiplicadorRecoleccion(TipoRecurso recurso)
        {
            return 0.5f; // no esta hecho para recolectar
        }

        public override float MultiplicadorCombate => 1.1f;

        // Lo unico que construye rapido.
        public override float MultiplicadorConstruccion => 2.0f;
    }

    // =====================================================================
    //  ARMAS FIJAS (combate) - vienen con el personaje y NO se compran
    // =====================================================================

    /// <summary>Lanza: alcance medio, del Lancero.</summary>
    public sealed class LanzaModel : ArmaModel
    {
        public LanzaModel() : base(TipoArma.Lanza)
        {
            MsEntreReparaciones = 2500;
        }

        public override float MultiplicadorRecoleccion(TipoRecurso recurso)
        {
            return 0.25f;
        }

        public override float MultiplicadorCombate => 1.3f;

        public override bool EsDeCombate => true;
    }

    /// <summary>Espada: el arma mas fuerte cuerpo a cuerpo, del Guerrero.</summary>
    public sealed class EspadaModel : ArmaModel
    {
        public EspadaModel() : base(TipoArma.Espada)
        {
            MsEntreReparaciones = 3000;
        }

        public override float MultiplicadorRecoleccion(TipoRecurso recurso)
        {
            return 0.25f;
        }

        public override float MultiplicadorCombate => 1.5f;

        public override bool EsDeCombate => true;
    }

    /// <summary>Arco: ataca a distancia, del Arquero.</summary>
    public sealed class ArcoModel : ArmaModel
    {
        public ArcoModel() : base(TipoArma.Arco)
        {
            MsEntreReparaciones = 2000;
        }

        public override float MultiplicadorRecoleccion(TipoRecurso recurso)
        {
            // Cazar con arco da algo de comida.
            return recurso == TipoRecurso.Comida ? 1.0f : 0.25f;
        }

        public override float MultiplicadorCombate => 1.2f;

        public override bool EsDeCombate => true;
    }
}