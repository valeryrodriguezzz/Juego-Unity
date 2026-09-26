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
        // El nombre se pasa directamente: "Hoplita" (Grecia), "Inmortal" (Persia), etc.
        public UnidadMeleeModel(string nombre)
        {
            Nombre          = nombre;

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
        // El nombre se pasa directamente: "Arquero" (Grecia), "Arquero Persa" (Persia), etc.
        public UnidadRangoModel(string nombre)
        {
            Nombre          = nombre;

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
