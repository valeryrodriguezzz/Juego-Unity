namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>
    /// Los 5 personajes seleccionables en el carrusel de la escena SeleccionJugador.
    ///
    /// IMPORTANTE: el valor numerico de cada personaje DEBE coincidir con la posicion
    /// del retrato dentro del array 'avatares[]' de CharacterSelectManager y con
    /// 'spritesCuerpo[]' de PlayerController.
    ///
    /// Si tus 5 personajes se llaman distinto, cambia SOLO los nombres de aqui
    /// (y la tabla de CatalogoArmas); el resto del sistema no se entera.
    /// </summary>
    public enum TipoPersonaje
    {
        Trabajador = 0,
        Guerrero = 1,
        Arquero = 2,
        Lancero = 3,
        Cazador = 4
    }

    /// <summary>
    /// Traduce entre las tres formas de nombrar al personaje que conviven en el
    /// proyecto: el indice del carrusel (int), el campo Rol de JugadorModel
    /// (string) y este enum.
    ///
    /// La FUENTE DE VERDAD es JugadorModel.Rol, porque vive en el Modelo y
    /// sobrevive los cambios de escena dentro de la Partida. El AvatarIndex
    /// solo sirve para elegir el sprite.
    /// </summary>
    public static class PersonajeInfo
    {
        public const int CantidadPersonajes = 5;

        /// <summary>
        /// Convierte el AvatarIndex del carrusel en TipoPersonaje.
        /// Si el indice viene fuera de rango (por ejemplo al probar la escena
        /// Juego directamente), devuelve Trabajador.
        /// </summary>
        public static TipoPersonaje DesdeAvatarIndex(int avatarIndex)
        {
            if (avatarIndex < 0 || avatarIndex >= CantidadPersonajes)
                return TipoPersonaje.Trabajador;

            return (TipoPersonaje)avatarIndex;
        }

        /// <summary>Del personaje al indice del carrusel.</summary>
        public static int AAvatarIndex(TipoPersonaje personaje)
        {
            return (int)personaje;
        }

        /// <summary>
        /// Del enum al string que se guarda en JugadorModel.Rol.
        /// Usa SIEMPRE esto para escribir el Rol y nunca tendras un
        /// "guerrero" en minuscula rondando por ahi.
        /// </summary>
        public static string ARol(TipoPersonaje personaje)
        {
            return Nombre(personaje);
        }

        /// <summary>
        /// Del Rol (string) al enum. Tolera mayusculas, minusculas y espacios.
        /// Si el Rol esta vacio o no se reconoce, devuelve Trabajador.
        /// </summary>
        public static TipoPersonaje DesdeRol(string rol)
        {
            TipoPersonaje resultado;
            return TryDesdeRol(rol, out resultado) ? resultado : TipoPersonaje.Trabajador;
        }

        /// <summary>Version que avisa si no reconocio el Rol.</summary>
        public static bool TryDesdeRol(string rol, out TipoPersonaje resultado)
        {
            resultado = TipoPersonaje.Trabajador;

            if (string.IsNullOrWhiteSpace(rol))
                return false;

            switch (rol.Trim().ToLowerInvariant())
            {
                case "trabajador": resultado = TipoPersonaje.Trabajador; return true;
                case "guerrero": resultado = TipoPersonaje.Guerrero; return true;
                case "arquero": resultado = TipoPersonaje.Arquero; return true;
                case "lancero": resultado = TipoPersonaje.Lancero; return true;
                case "cazador": resultado = TipoPersonaje.Cazador; return true;
                default: return false;
            }
        }

        public static string Nombre(TipoPersonaje personaje)
        {
            switch (personaje)
            {
                case TipoPersonaje.Trabajador: return "Trabajador";
                case TipoPersonaje.Guerrero: return "Guerrero";
                case TipoPersonaje.Arquero: return "Arquero";
                case TipoPersonaje.Lancero: return "Lancero";
                case TipoPersonaje.Cazador: return "Cazador";
                default: return "Desconocido";
            }
        }
    }
}