using ImperiosEnGuerra.Modelo;

namespace ImperiosEnGuerra.Controlador
{
    /// <summary>
    /// CONTROLADOR: el atajo para dejar constancia de lo que pasa en el juego.
    ///
    /// POR QUE HACIA FALTA
    /// log_partida.txt ya existia, pero solo se escribia desde PartidaModel, y
    /// ahi dentro las unicas acciones que se anotaban eran los ataques por
    /// turnos y los edificios terminados. Cuando el combate se paso al mapa
    /// (el enemigo camina y pega desde la escena), aquel hilo de ataques dejo
    /// de usarse, asi que el registro se quedaba practicamente en blanco.
    ///
    /// El problema no era el archivo sino quien lo llamaba: talar, picar,
    /// cazar, comprar, comer, curarse, construir y pelear ocurren todos en
    /// Controladores de escena, y ninguno tenia a mano la PartidaModel.
    ///
    /// COMO FUNCIONA
    /// Una linea desde cualquier Controlador:
    ///
    ///     Bitacora.Anotar("Recoleccion", "+8 de Madera con el Hacha");
    ///
    /// Por dentro busca la partida y llama a PartidaModel.RegistrarAccion, que
    /// es quien de verdad guarda la linea y dispara OnAccionRegistrada. Ese
    /// evento ya estaba enganchado a ArchivoController.RegistrarAccion desde
    /// que se crea la partida, asi que la linea acaba en log_partida.txt sin
    /// tocar nada mas.
    ///
    /// Si todavia no hay partida (por ejemplo en el Menu), no hace nada: es
    /// preferible perder una linea de registro a reventar el juego por no
    /// poder escribirla.
    /// </summary>
    public static class Bitacora
    {
        /// <summary>Anota una accion del jugador.</summary>
        public static void Anotar(string accion, string resultado)
        {
            PartidaModel partida = Partida();
            if (partida == null) return;

            string quien = partida.Jugador != null ? partida.Jugador.Nombre : "Jugador";

            partida.RegistrarAccion(quien, accion, resultado);
        }

        /// <summary>Anota una accion de otro (un enemigo, el sistema...).</summary>
        public static void Anotar(string quien, string accion, string resultado)
        {
            PartidaModel partida = Partida();
            if (partida == null) return;

            partida.RegistrarAccion(quien, accion, resultado);
        }

        private static PartidaModel Partida()
        {
            return PlayerSelectionManager.Instance != null
                ? PlayerSelectionManager.Instance.Partida
                : null;
        }
    }
}
