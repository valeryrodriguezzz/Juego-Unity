namespace ImperiosEnGuerra.Modelo.Armas
{
    /// <summary>
    /// El personaje del jugador.
    ///
    /// Hoy solo hay uno: el Pawn (Trabajador). Es el unico de Tiny Swords con
    /// animaciones de talar, picar y construir, asi que se quito la seleccion
    /// de personaje y con ella los otros cuatro (guerrero, arquero, lancero,
    /// cazador), que solo tenian idle, caminar y atacar.
    ///
    /// El enum se conserva en vez de borrarlo porque JugadorModel.Rol y el
    /// InventarioArmasModel trabajan con el, y porque deja la puerta abierta
    /// a las unidades entrenables sin tener que rehacer nada.
    /// </summary>
    public enum TipoPersonaje
    {
        Trabajador = 0
    }

    /// <summary>
    /// Traduce entre el enum y el campo Rol (string) de JugadorModel, que es
    /// la fuente de verdad: vive en el Modelo y sobrevive los cambios de escena.
    /// </summary>
    public static class PersonajeInfo
    {
        /// <summary>Del enum al string que se guarda en JugadorModel.Rol.</summary>
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
                case "trabajador":
                case "pawn":
                    resultado = TipoPersonaje.Trabajador;
                    return true;

                default:
                    return false;
            }
        }

        public static string Nombre(TipoPersonaje personaje)
        {
            switch (personaje)
            {
                case TipoPersonaje.Trabajador: return "Trabajador";
                default:                       return "Desconocido";
            }
        }
    }
}
