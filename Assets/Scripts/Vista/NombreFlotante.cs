using System;
using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;

// VISTA: muestra el nombre del jugador flotando sobre su cabeza.
//
// COMO USARLO EN UNITY:
//   1. Selecciona el GameObject Jugador.
//   2. Add Component -> NombreFlotante.
//   3. Play. No hay que crear nada mas.
//
// POR QUE NO USA UN CANVAS:
// un Canvas en World Space obliga a acertar cuatro cosas a la vez (Render
// Mode, Scale 0.01, Width/Height en pixeles y Sorting Layer) y si una falla
// el texto simplemente no se ve. Aqui se usa TextMeshPro 3D, que es un
// objeto normal del mundo, como un sprite: se posiciona en unidades, se
// ordena con Sorting Layer y ya. Mucho menos que puede salir mal.
//
// Si el nombre se ve muy grande o muy chico, mueve "Tamano Letra".

public class NombreFlotante : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Tamaño de la letra. 4 es un buen punto de partida. " +
             "Si se ve gigante baja a 2; si no se lee sube a 6 u 8.")]
    public float tamanoLetra = 4f;

    [Tooltip("Que tan arriba del personaje flota el nombre, en unidades.")]
    public float altura = 1.2f;

    public Color colorTexto = Color.white;

    [Tooltip("Arrastra aqui un TMP Font Asset (por ejemplo 'dogica SDF'). " +
             "Si lo dejas vacio se usa la fuente por defecto de TextMeshPro.")]
    public TMP_FontAsset fuente;

    [Header("Si no hay partida (probando la escena Juego sola)")]
    public string nombrePorDefecto = "Jugador";

    [Header("Avanzado")]
    [Tooltip("Si ya tienes un texto hecho a mano, arrastralo aqui. " +
             "Dejalo vacio y el script crea uno solo (recomendado).")]
    public TMP_Text textoExistente;

    private TMP_Text _texto;

    private void Start()
    {
        // Primer mensaje, antes de cualquier cosa que pueda fallar: si este
        // no sale en la consola, el script no se esta ejecutando y el
        // problema es que el componente no esta puesto o esta desmarcado.
        Debug.Log("[Nombre] Arrancando en el objeto '" + gameObject.name + "'.");

        _texto = textoExistente != null ? textoExistente : CrearTexto3D();

        if (_texto == null)
        {
            Debug.LogError("[Nombre] No pude crear el texto. Revisa que TextMeshPro " +
                           "este importado (Window > TextMeshPro > Import TMP Essential Resources).");
            enabled = false;
            return;
        }

        _texto.text = ObtenerNombre();

        Debug.Log("[Nombre] Listo: \"" + _texto.text + "\" en " + _texto.transform.position +
                  " | Jugador en " + transform.position +
                  " | tamaño " + _texto.fontSize);
    }

    private TMP_Text CrearTexto3D()
    {
        try
        {
            var go = new GameObject("NombreJugador");

            // Hijo del Jugador: se mueve con el sin que nadie lo actualice
            // cada frame. El flipX del sprite no afecta a los hijos, asi que
            // el nombre nunca se va a ver escrito al reves.
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, altura, 0f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = tamanoLetra;
            tmp.color = colorTexto;

            // La fuente se asigna solo si la pusiste; si no, TextMeshPro
            // deja la suya por defecto y el texto igual se ve.
            if (fuente != null)
                tmp.font = fuente;

            // Ancho generoso para que no corte nombres largos.
            var rect = tmp.rectTransform;
            if (rect != null) rect.sizeDelta = new Vector2(6f, 1.5f);

            // Que se dibuje POR ENCIMA del mapa. Un TextMeshPro 3D es un
            // MeshRenderer, asi que usa las mismas Sorting Layers que los
            // sprites: se le pone la capa del personaje y un orden alto.
            var mr = go.GetComponent<MeshRenderer>();
            var spriteJugador = GetComponent<SpriteRenderer>();

            if (mr != null)
            {
                if (spriteJugador != null)
                    mr.sortingLayerID = spriteJugador.sortingLayerID;

                mr.sortingOrder = 100;
            }

            return tmp;
        }
        catch (Exception ex)
        {
            Debug.LogError("[Nombre] Fallo creando el texto: " + ex.Message);
            return null;
        }
    }

    private string ObtenerNombre()
    {
        if (PlayerSelectionManager.Instance == null || PlayerSelectionManager.Instance.Partida == null)
        {
            Debug.LogWarning("[Nombre] No hay partida (probando la escena Juego sola). " +
                             "Se usa el nombre por defecto.");
            return nombrePorDefecto;
        }

        JugadorModel jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        if (jugador == null || string.IsNullOrWhiteSpace(jugador.Nombre))
            return nombrePorDefecto;

        return jugador.Nombre;
    }

    // Permite mover los valores en el Inspector MIENTRAS el juego corre y ver
    // el cambio al instante, sin tener que parar y volver a darle Play.
    private void OnValidate()
    {
        if (_texto == null) return;

        _texto.fontSize = tamanoLetra;
        _texto.color = colorTexto;
        if (fuente != null) _texto.font = fuente;
        _texto.transform.localPosition = new Vector3(0f, altura, 0f);
    }
}
