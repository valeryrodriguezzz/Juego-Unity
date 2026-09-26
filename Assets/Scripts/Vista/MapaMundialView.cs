using System;
using System.Collections.Generic;
using ImperiosEnGuerra.Controlador;
using ImperiosEnGuerra.Modelo;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// MapaMundialView: VISTA del mapa mundial (escena "Juego").
// Une los botones de los territorios con MapaMundialController.
//
// COMO USARLO EN UNITY:
// 1. En la escena, cada territorio es un Button (Egipto, Persia, Roma, Vikingos).
// 2. Crea un boton "Atacar" y un TMP_Text para mensajes.
// 3. Crea un GameObject vacio "MapaMundialView" y arrastra este script.
// 4. En el Inspector, en "Territorios" agrega un elemento por boton:
//    escribe el nombre EXACTO ("Egipto", "Persia", "Roma", "Vikingos") y arrastra su Button.
// 5. Cada territorio debe tener una escena con ESE mismo nombre, agregada en Build Settings.
public class MapaMundialView : MonoBehaviour
{
    [Serializable]
    public class BotonTerritorio
    {
        public string imperio;
        public Button boton;
    }

    [SerializeField] private List<BotonTerritorio> territorios = new List<BotonTerritorio>();
    [SerializeField] private Button botonAtacar;
    [SerializeField] private TMP_Text textoMensaje;

    private MapaMundialController _controlador;
    private PartidaModel _partida;

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null)
            _partida = PlayerSelectionManager.Instance.Partida;

        if (_partida == null)
        {
            Debug.LogWarning("[MapaMundialView] No hay partida activa.");
            return;
        }

        _controlador = new MapaMundialController(_partida);
        _controlador.OnTerritorioSeleccionado += t => Mostrar("Territorio seleccionado: " + t.Nombre);
        _controlador.OnAccionInvalida += Mostrar;
        // La escena de cada territorio se llama igual que el territorio
        _controlador.OnBatallaIniciada += t => SceneManager.LoadScene(t.Nombre);
        _controlador.OnJuegoGanado += () => Mostrar("Grecia conquisto todos los territorios!");

        foreach (var entrada in territorios)
        {
            if (entrada.boton == null) continue;
            string imperio = entrada.imperio;
            entrada.boton.onClick.AddListener(() => _controlador.SeleccionarTerritorio(imperio));
        }

        if (botonAtacar != null)
            botonAtacar.onClick.AddListener(_controlador.ConfirmarAtaque);

        ActualizarBotones();
    }

    // Deshabilita los territorios que ya se conquistaron (al volver de una batalla ganada)
    private void ActualizarBotones()
    {
        var lista = _controlador.ObtenerTerritorios();
        foreach (var entrada in territorios)
        {
            if (entrada.boton == null) continue;
            var t = lista.Find(x => x.Nombre == entrada.imperio);
            entrada.boton.interactable = t != null && !t.EsBase && !t.EsConquistado;
        }
    }

    private void Mostrar(string mensaje)
    {
        if (textoMensaje != null) textoMensaje.text = mensaje;
        Debug.Log("[Mapa] " + mensaje);
    }
}
