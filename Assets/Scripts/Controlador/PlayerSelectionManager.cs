using UnityEngine;

// Este script sigue siendo el "puente" que sobrevive entre escenas
// (Menu -> Seleccion -> Grecia), pero ahora guarda tu objeto Jugador
// real del Modelo, no strings sueltos.
//
// COMO USARLO:
// 1) En tu escena de Menu, crea un GameObject vacio llamado "GameManager".
// 2) Arrastra este script sobre ese GameObject.

public class PlayerSelectionManager : MonoBehaviour
{
    public static PlayerSelectionManager Instance { get; private set; }

    // El Jugador (Modelo) que se va a usar en la partida
    public JugadorModel JugadorActual { get; private set; }

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
    public void CrearJugador(string nombre, string imperio)
    {
        JugadorActual = new JugadorModel(nombre, imperio);
        Debug.Log("Jugador creado: " + JugadorActual.Nombre + " - Imperio: " + JugadorActual.Imperio);
    }
}