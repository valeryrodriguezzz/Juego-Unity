namespace ImperiosEnGuerra.Modelo
{
    public enum Civilizacion
    {
        Grecia,   // Siempre el jugador humano
        Persia,   // Posible oponente IA
        Roma,     // Posible oponente IA
        Egipto,   // Posible oponente IA
        Vikingos  // Posible oponente IA
    }

    public static class CivilizacionInfo
    {
        public static string NombreEdificioPrincipal(Civilizacion civ)
        {
            switch (civ)
            {
                case Civilizacion.Grecia:   return "Acrópolis";
                case Civilizacion.Persia:   return "Palacio Persa";
                case Civilizacion.Roma:     return "Capitolio";
                case Civilizacion.Egipto:   return "Gran Pirámide";
                case Civilizacion.Vikingos: return "Gran Salón";
                default:                    return "Centro Urbano";
            }
        }

        public static string NombreUnidadMelee(Civilizacion civ)
        {
            switch (civ)
            {
                case Civilizacion.Grecia:   return "Hoplita";
                case Civilizacion.Persia:   return "Inmortal";
                case Civilizacion.Roma:     return "Legionario";
                case Civilizacion.Egipto:   return "Guardia del Faraón";
                case Civilizacion.Vikingos: return "Berserker";
                default:                    return "Guerrero";
            }
        }

        public static string NombreUnidadRango(Civilizacion civ)
        {
            switch (civ)
            {
                case Civilizacion.Grecia:   return "Arquero";
                case Civilizacion.Persia:   return "Arquero Persa";
                case Civilizacion.Roma:     return "Ballestero";
                case Civilizacion.Egipto:   return "Arquero del Nilo";
                case Civilizacion.Vikingos: return "Lanzador de Hachas";
                default:                    return "Arquero";
            }
        }

        public static Civilizacion ElegirOponenteAleatorio()
        {
            // Civilizaciones disponibles como oponente (todas menos Grecia)
            Civilizacion[] oponentes = {
                Civilizacion.Persia,
                Civilizacion.Roma,
                Civilizacion.Egipto,
                Civilizacion.Vikingos
            };

            System.Random rnd = new System.Random();
            return oponentes[rnd.Next(oponentes.Length)];
        }
    }
}
