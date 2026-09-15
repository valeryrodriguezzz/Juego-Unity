using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Si NO usas TextMeshPro, borra esta linea y usa UnityEngine.UI + InputField normal

// COMO USARLO EN TU ESCENA DE SELECCION:
// 1) Crea un GameObject vacio (ej: "SelectManager") y pega este script ahi.
// 2) Arrastra tu campo de texto (InputField / TMP_InputField) al campo "Name Input Field".
// 3) Escribe en "Next Scene Name" el nombre EXACTO de tu escena de Grecia.
// 4) En cada boton de personaje, en su OnClick():
//    - Arrastra "SelectManager", elige SeleccionarPersonaje(string) y escribe
//      el "Imperio" o tipo de personaje que representa ese boton (ej: "Grecia").
// 5) En el boton "Confirmar/Continuar", en su OnClick():
//    - Arrastra "SelectManager" y elige ConfirmarSeleccion()

public class CharacterSelectManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameInputField;

    [Header("Escena a cargar al confirmar (nombre exacto en Build Settings)")]
    public string nextSceneName = "Grecia";

    private string imperioSeleccionado = "Grecia"; // valor por defecto

    // Conecta esta funcion al OnClick() de cada boton de personaje,
    // pasando el nombre del imperio/personaje que representa ese boton.
    public void SeleccionarPersonaje(string imperio)
    {
        imperioSeleccionado = imperio;
        Debug.Log("Personaje/Imperio seleccionado: " + imperio);
    }

    // Conecta esta funcion al OnClick() del boton "Confirmar" / "Continuar"
    public void ConfirmarSeleccion()
    {
        string nombreEscrito = (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            ? nameInputField.text
            : "Jugador";

        if (PlayerSelectionManager.Instance != null)
        {
            // Aqui es donde se crea tu objeto Jugador (Modelo) real,
            // con el nombre que escribiste conectado a la clase Personaje/Jugador.
            PlayerSelectionManager.Instance.CrearJugador(nombreEscrito, imperioSeleccionado);
        }
        else
        {
            Debug.LogWarning("No se encontro PlayerSelectionManager.Instance. " +
                "Asegurate de tener el GameObject 'GameManager' con ese script en la escena de Menu.");
        }

        SceneManager.LoadScene(nextSceneName);
    }
}