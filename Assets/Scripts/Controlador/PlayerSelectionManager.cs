using UnityEngine;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Armas;

// Puente que sobrevive entre escenas (Menu -> Seleccion -> Juego).
// Guarda la PartidaModel completa + el INDICE del avatar elegido
// (no el sprite directo, porque el retrato del carrusel y el sprite
// de cuerpo completo en el mapa son imagenes distintas).
//
// COMO USARLO:
// 1) En tu escena de Menu, crea un GameObject vacio llamado "GameManager".
// 2) Arrastra este script sobre ese GameObject.

public class PlayerSelectionManager : MonoBehaviour
{
    public static PlayerSelectionManager Instance { get; private set; }

    // La partida completa: Jugador (siempre Grecia) + MapaMundial
    public PartidaModel Partida { get; private set; }

    // Indice del personaje elegido en el carrusel (0, 1, 2...).
    // Se usa despues para buscar el sprite de CUERPO correspondiente
    // en la escena Juego (ver PlayerController).
    public int AvatarIndex { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Se llama desde la escena de seleccion de personaje al confirmar
    public void CrearPartida(string nombreJugador, int avatarIndex)
    {
        AvatarIndex = avatarIndex;
        Partida = new PartidaModel(nombreJugador);

        // <-- AGREGAR estas dos lineas, con la Partida ya creada
        TipoPersonaje personaje = PersonajeInfo.DesdeAvatarIndex(avatarIndex);
        Partida.Jugador.Rol = PersonajeInfo.ARol(personaje);

        Debug.Log("Partida creada. Jugador: " + Partida.Jugador.Nombre +
                   " (Civilizacion: " + Partida.Jugador.Civilizacion +
                   ", Rol: " + Partida.Jugador.Rol +
                   ", AvatarIndex: " + avatarIndex + ")");
    }

}