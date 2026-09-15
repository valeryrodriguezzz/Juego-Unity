using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// COMO USARLO EN TU ESCENA DE SELECCION (diseno de carrusel):
// 1) Crea un GameObject vacio (ej: "SelectManager") y pega este script ahi.
// 2) En el Inspector:
//    - Arrastra tu InputField_Nombre al campo "Name Input Field".
//    - Arrastra el objeto "PersonajeImagen" al campo "Personaje Image".
//    - En "Avatares" (lista), agrega los RETRATOS (cabezas) de cada personaje.
//      IMPORTANTE: el orden aqui debe coincidir con el orden de los sprites
//      de cuerpo completo que pongas despues en PlayerController (escena Juego).
//    - Escribe el nombre exacto de tu siguiente escena en "Next Scene Name".
// 3) BotonSiguiente -> On Click() -> SelectManager -> Siguiente()
// 4) BotonAnterior  -> On Click() -> SelectManager -> Anterior()
// 5) BotonJugar      -> On Click() -> SelectManager -> ConfirmarSeleccion()

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameInputField;
    public Image personajeImage;

    [Header("Retratos para el carrusel (cabezas, en el mismo orden que los sprites de cuerpo)")]
    public Sprite[] avatares;

    [Header("Escena a cargar al confirmar")]
    public string nextSceneName = "Juego";

    private int indiceActual = 0;

    private void Start()
    {
        MostrarAvatarActual();
    }

    public void Siguiente()
    {
        if (avatares == null || avatares.Length == 0) return;

        indiceActual++;
        if (indiceActual >= avatares.Length) indiceActual = 0;
        MostrarAvatarActual();
    }

    public void Anterior()
    {
        if (avatares == null || avatares.Length == 0) return;

        indiceActual--;
        if (indiceActual < 0) indiceActual = avatares.Length - 1;
        MostrarAvatarActual();
    }

    private void MostrarAvatarActual()
    {
        if (personajeImage != null && avatares.Length > 0)
        {
            personajeImage.sprite = avatares[indiceActual];
        }
    }

    public void ConfirmarSeleccion()
    {
        string nombreEscrito = (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            ? nameInputField.text
            : "Jugador";

        if (PlayerSelectionManager.Instance != null)
        {
            // Pasamos el INDICE, no el sprite, porque el sprite del carrusel
            // (retrato) y el sprite del cuerpo en el mapa son distintos.
            PlayerSelectionManager.Instance.CrearPartida(nombreEscrito, indiceActual);
        }
        else
        {
            Debug.LogWarning("No se encontro PlayerSelectionManager.Instance. " +
                "Asegurate de tener el GameObject 'GameManager' con ese script en la escena de Menu.");
        }

        SceneManager.LoadScene(nextSceneName);
    }
}