using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// CONTROLADOR: le pone una pared invisible al borde de la meseta.
///
/// EL PROBLEMA QUE RESUELVE
/// Los tiles de pasto quedaron caminables para que el jugador pueda subir por
/// la rampa y andar por encima de la montaña. Pero eso deja la meseta abierta
/// por el norte y por los lados, donde el dibujo no tiene pared de roca: el
/// muñeco puede entrar por detras sin usar la rampa y se ve rarisimo.
///
/// LA REGLA
/// Recorre el Tilemap y, por cada casilla CAMINABLE que da al vacio, levanta
/// un muro en ese lado. Con dos excepciones:
///   - Las casillas que ya chocan por si mismas (la roca) se saltan: no hace
///     falta reforzar lo que ya bloquea.
///   - Las casillas de la RAMPA se saltan enteras, y por eso la entrada
///     queda abierta.
/// El sur no necesita nada porque ahi siempre esta la fila de roca dibujada.
///
/// No hay coordenadas escritas a mano: se calculan del propio mapa. Por eso
/// el mismo script sirve igual en Roma, Persia, Egipto y Vikingos, y si
/// mañana repintas la montaña el borde se recalcula solo.
///
/// COMO USARLO EN UNITY:
/// 1. Selecciona el objeto "Montaña" (el que tiene el Tilemap y el
///    Tilemap Collider 2D).
/// 2. Add Component -> BordeMapaController.
/// 3. En "Tiles Que Dejan Pasar" pon Size 4 y arrastra los cuatro tiles de la
///    rampa desde
///    Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles:
///       Tilemap_color1_32   Tilemap_color1_33
///       Tilemap_color1_38   Tilemap_color1_39
///    (Si dejas la lista vacia se tapa TODO el contorno y no se podra subir.)
/// 4. Dale Play. En la consola sale cuantos muros creo.
///
/// Para verlos: con el juego corriendo, despliega "Montaña" en la Hierarchy y
/// vas a encontrar un hijo llamado BordesInvisibles con un muro por cada lado.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class BordeMapaController : MonoBehaviour
{
    [Header("Por donde SI se puede pasar")]
    [Tooltip("Los tiles de la rampa. Esas casillas no reciben muro, y son la " +
             "unica forma de subir a la meseta.")]
    [SerializeField] private TileBase[] tilesQueDejanPasar;

    [Header("Ajustes")]
    [Tooltip("Grosor del muro, en unidades del mundo. Delgado esta bien: solo " +
             "tiene que impedir el paso, no se ve.")]
    [SerializeField] private float grosor = 0.15f;

    [Tooltip("Cuanto se mete el muro hacia adentro de la casilla. Subelo un " +
             "poco si el muñeco alcanza a asomarse por el borde.")]
    [SerializeField] private float metidoHaciaAdentro = 0f;

    [Tooltip("Deja los muros tambien en el lado de abajo. Normalmente no hace " +
             "falta porque ahi esta la pared de roca, que ya choca.")]
    [SerializeField] private bool tambienElLadoSur;

    private static readonly Vector3Int[] Lados =
    {
        new Vector3Int( 0,  1, 0),  // norte
        new Vector3Int( 0, -1, 0),  // sur
        new Vector3Int( 1,  0, 0),  // este
        new Vector3Int(-1,  0, 0)   // oeste
    };

    private void Awake()
    {
        Construir();
    }

    private void Construir()
    {
        Tilemap mapa = GetComponent<Tilemap>();

        var dejanPasar = new HashSet<TileBase>();
        if (tilesQueDejanPasar != null)
            foreach (TileBase t in tilesQueDejanPasar)
                if (t != null) dejanPasar.Add(t);

        if (dejanPasar.Count == 0)
            Debug.LogWarning("[Borde] La lista Tiles Que Dejan Pasar esta vacia, " +
                             "asi que se va a tapar todo el contorno y no se " +
                             "podra subir. Arrastra ahi los tiles de la rampa.");

        var contenedor = new GameObject("BordesInvisibles");
        contenedor.transform.SetParent(transform, false);

        Vector3 celda = mapa.layoutGrid != null ? mapa.layoutGrid.cellSize : Vector3.one;
        int creados = 0;

        // cellBounds ya viene recortado a lo que de verdad esta pintado, asi
        // que esto no recorre el mapa entero sino solo la montaña.
        foreach (Vector3Int pos in mapa.cellBounds.allPositionsWithin)
        {
            TileBase actual = mapa.GetTile(pos);
            if (actual == null) continue;

            // La rampa se salta entera: es la entrada.
            if (dejanPasar.Contains(actual)) continue;

            // Lo que ya choca no necesita ayuda.
            if (Choca(actual)) continue;

            foreach (Vector3Int lado in Lados)
            {
                if (!tambienElLadoSur && lado.y < 0) continue;

                // Solo hay borde si al lado no hay nada pintado. Si hay otra
                // casilla (aunque sea la rampa) el paso es interno y se deja.
                if (mapa.GetTile(pos + lado) != null) continue;

                CrearMuro(contenedor.transform, mapa.GetCellCenterWorld(pos), lado, celda);
                creados++;
            }
        }

        Debug.Log("[Borde] " + creados + " muros invisibles en " + gameObject.name +
                  ". Entradas abiertas: " + dejanPasar.Count + " tile(s) de rampa.");
    }

    /// <summary>
    /// Un Tile normal dice si choca en su campo Collider Type. Si el tile es
    /// de otro tipo (un Rule Tile, por ejemplo) no se puede consultar, y se
    /// asume que choca: es el lado seguro, porque como mucho no se le pone un
    /// muro que probablemente no necesitaba.
    /// </summary>
    private static bool Choca(TileBase t)
    {
        Tile normal = t as Tile;
        return normal == null || normal.colliderType != Tile.ColliderType.None;
    }

    private void CrearMuro(Transform padre, Vector3 centroCelda, Vector3Int lado, Vector3 celda)
    {
        var go = new GameObject("Muro_" + Nombre(lado));
        go.transform.SetParent(padre, false);
        go.layer = gameObject.layer;

        bool horizontal = lado.y != 0;

        // El muro se pega por dentro de la casilla, contra el lado que da al
        // vacio: medio ancho de celda hacia alla, menos medio grosor.
        float mitad = horizontal ? celda.y * 0.5f : celda.x * 0.5f;
        float desplazamiento = mitad - grosor * 0.5f - metidoHaciaAdentro;

        go.transform.position = centroCelda + new Vector3(lado.x, lado.y, 0f) * desplazamiento;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = horizontal
            ? new Vector2(celda.x, grosor)
            : new Vector2(grosor, celda.y);
    }

    private static string Nombre(Vector3Int lado)
    {
        if (lado.y > 0) return "N";
        if (lado.y < 0) return "S";
        return lado.x > 0 ? "E" : "O";
    }
}
