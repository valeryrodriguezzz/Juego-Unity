namespace ImperiosEnGuerra.Modelo.Armas
{
    // =====================================================================
    //  Las 4 herramientas concretas.
    //
    //  Cada una hereda de ArmaModel y solo define en que es buena. Todo lo
    //  demas (la durabilidad, el lock, el hilo de mantenimiento que la va
    //  reparando sola) lo trae ya hecho de la clase padre.
    //
    //  El multiplicador es lo que hace que importe llevar la herramienta
    //  correcta: el hacha saca el doble de madera, pero con el pico apenas
    //  sacas una cuarta parte.
    // =====================================================================

    /// <summary>Hacha: la herramienta de la madera. Es con la que nace el Pawn.</summary>
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
                default:                 return 0.25f; // a duras penas
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
                case TipoRecurso.Oro: return 2.0f; // minar la roca: lo suyo
                default:              return 0.25f;
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
                case TipoRecurso.Comida: return 2.0f; // cazar ovejas: lo suyo
                default:                 return 0.25f;
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

        // El unico que construye rapido. Lo usara el sistema de construccion
        // cuando se conecte con los edificios.
        public override float MultiplicadorConstruccion => 2.0f;
    }
}
