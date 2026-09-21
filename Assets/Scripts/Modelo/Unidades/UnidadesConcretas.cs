namespace ImperiosEnGuerra.Modelo.Unidades
{
    // ══════════════════════════════════════════════════════════
    //  UNIDADES GENÉRICAS
    //
    //  El nombre de cada unidad se asigna según la civilización
    //  al momento de crearla. Las estadísticas son las mismas
    //  para todas las civilizaciones (por simplicidad).
    // ══════════════════════════════════════════════════════════

    public class UnidadMeleeModel : UnidadModel
    {
        public UnidadMeleeModel(string civilizacion)
        {
            // El nombre depende de la civilización
            Nombre          = CivilizacionInfo.NombreUnidadMelee(civilizacion); //No se que se deba cambiar aca

            // Estadísticas iguales para todas las civilizaciones
            Vida            = 120;
            VidaMax         = 120;
            Ataque          = 22;
            Defensa         = 15;
            RangoMovimiento = 3;
            RangoAtaque     = 1;    // Cuerpo a cuerpo
            CostoOro        = 60;
            CostoComida     = 30;
            CostoArmas      = 20;
            TiempoEntrenamientoSeg = 12;
        }
    }

    public class UnidadRangoModel : UnidadModel
    {
        public UnidadRangoModel(string civilizacion)
        {
            Nombre          = CivilizacionInfo.NombreUnidadRango(civilizacion);//No se que se deba cambiar aca

            Vida            = 70;
            VidaMax         = 70;
            Ataque          = 28;
            Defensa         = 4;
            RangoMovimiento = 2;
            RangoAtaque     = 4;    // Ataca desde lejos
            CostoOro        = 55;
            CostoComida     = 20;
            CostoArmas      = 25;
            TiempoEntrenamientoSeg = 10;
        }
    }
}
