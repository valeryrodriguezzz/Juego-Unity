using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// CONTROLADOR de la escena SeleccionJugador.
//
// Ya NO hay carrusel: el jugador siempre es el Pawn, porque es el unico
// personaje de Tiny Swords con animaciones de talar, picar y construir.
// Esta pantalla quedo solo para ponerle el nombre al personaje.
//
// COMO USARLO EN LA ESCENA:
// 1) El GameObject "SelectManager" lleva este script.
// 2) En el Inspector:
//    - Name Input Field -> tu InputField_Nombre
//    - Personaje Image  -> el objeto PersonajeImagen (opcional)
//    - Retrato Pawn     -> la carita del Pawn (opcional: si lo dejas vacio,
//                          se queda la imagen que ya pusiste en el Inspector
//                          del propio objeto PersonajeImagen)
//    - Next Scene Name  -> "Juego"
// 3) BotonJugar -> On Click() -> SelectManager -> ConfirmarSeleccion()
//
// QUE HAY QUE BORRAR DE LA ESCENA:
//    BotonSiguiente y BotonAnterior ya no hacen nada: eliminalos del Canvas.

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameInputField;
    public Image personajeImage;

    [Header("Retrato del Pawn (opcional)")]
    [Tooltip("Si lo asignas, se pone en Personaje Image al abrir la escena. " +
             "Si lo dejas vacio, se respeta la imagen que ya tenga el objeto.")]
    public Sprite retratoPawn;

    [Header("Escena a cargar al confirmar")]
    public string nextSceneName = "Juego";

    private void Start()
    {
        if (personajeImage != null && retratoPawn != null)
            personajeImage.sprite = retratoPawn;
    }

    public void ConfirmarSeleccion()
    {
        string nombreEscrito = (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            ? nameInputField.text
            : "Jugador";

        if (PlayerSelectionManager.Instance != null)
        {
            PlayerSelectionManager.Instance.CrearPartida(nombreEscrito);
        }
        else
        {
            Debug.LogWarning("No se encontro PlayerSelectionManager.Instance. " +
                "Asegurate de tener el GameObject 'GameManager' con ese script en la escena.");
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
