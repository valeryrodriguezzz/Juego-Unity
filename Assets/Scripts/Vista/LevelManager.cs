using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    // Escribe aqui el nombre EXACTO de tu escena de seleccion
    // (tal como aparece en Build Settings y en el archivo .unity)
    [Header("Nombre de la escena de seleccion de personaje")]
    public string characterSelectSceneName = "SeleccionJugador";

    void Start()
    {

    }

    void Update()
    {

    }

    public void BotonStart()
    {
        Debug.Log("¡EL BOTÓN SI RESPONDE AL CLICK!");
        SceneManager.LoadScene(characterSelectSceneName);
    }
}