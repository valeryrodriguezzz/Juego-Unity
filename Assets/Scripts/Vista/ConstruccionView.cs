using System;
using ImperiosEnGuerra.Modelo;
using ImperiosEnGuerra.Modelo.Edificios;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ConstruccionView: VISTA para construir edificios y curarse en la Iglesia.
// Los hilos de construccion y produccion viven en EdificioModel (Modelo);
// aqui solo se pide construir y se muestran mensajes.
//
// COMO USARLO EN UNITY:
// 1. Crea un boton por edificio (Granja, Mina, Aserradero, Armeria, Iglesia)
//    y un boton "Curar" (usa la Iglesia).
// 2. Crea un TMP_Text para mensajes.
// 3. Crea un GameObject vacio "ConstruccionView" y arrastra este script.
// 4. Conecta los botones en el Inspector (los que no uses, dejalos vacios).
public class ConstruccionView : MonoBehaviour
{
    [SerializeField] private Button botonGranja;
    [SerializeField] private Button botonMina;
    [SerializeField] private Button botonAserradero;
    [SerializeField] private Button botonArmeria;
    [SerializeField] private Button botonIglesia;
    [SerializeField] private Button botonCurar;
    [SerializeField] private TMP_Text textoMensaje;

    private JugadorModel _jugador;
    private IglesiaModel _iglesia;

    private void Start()
    {
        if (PlayerSelectionManager.Instance != null && PlayerSelectionManager.Instance.Partida != null)
            _jugador = PlayerSelectionManager.Instance.Partida.Jugador;

        if (_jugador == null)
        {
            Debug.LogWarning("[ConstruccionView] No hay partida activa.");
            return;
        }

        Conectar(botonGranja, () => Construir(new GranjaModel()));
        Conectar(botonMina, () => Construir(new MinaModel()));
        Conectar(botonAserradero, () => Construir(new AserraderoModel()));
        Conectar(botonArmeria, () => Construir(new ArmeriaModel()));
        Conectar(botonIglesia, ConstruirIglesia);
        Conectar(botonCurar, Curar);

        // El evento llega desde el hilo de construccion: hay que pasar por el dispatcher
        _jugador.OnEdificioConstruido += AlConstruirse;
    }

    private void OnDestroy()
    {
        if (_jugador != null)
            _jugador.OnEdificioConstruido -= AlConstruirse;
    }

    private static void Conectar(Button boton, Action accion)
    {
        if (boton != null) boton.onClick.AddListener(() => accion());
    }

    private void Construir(EdificioModel edificio)
    {
        if (_jugador.Construir(edificio))
            Mostrar(edificio.Nombre + " en construccion (" + edificio.TiempoConstruccionSeg + " s)...");
        else
            Mostrar("No alcanzan los recursos para " + edificio.Nombre + " (" +
                    edificio.CostoOro + " oro, " + edificio.CostoMadera + " madera).");
    }

    private void ConstruirIglesia()
    {
        if (_iglesia != null) { Mostrar("Ya tienes una Iglesia."); return; }

        var nueva = new IglesiaModel();
        if (_jugador.Construir(nueva))
        {
            _iglesia = nueva;
            Mostrar("Iglesia en construccion (" + nueva.TiempoConstruccionSeg + " s)...");
        }
        else
        {
            Mostrar("No alcanzan los recursos para la Iglesia (" +
                    nueva.CostoOro + " oro, " + nueva.CostoMadera + " madera).");
        }
    }

    private void Curar()
    {
        if (_iglesia == null) { Mostrar("Primero construye la Iglesia."); return; }
        if (!_iglesia.Construido) { Mostrar("La Iglesia aun se esta construyendo."); return; }

        if (_iglesia.Curar(_jugador))
        {
            Mostrar("Te curaste " + _iglesia.CuracionPorUso + " de vida.");
            return;
        }

        double espera = _iglesia.SegundosParaPoderUsar();
        if (espera > 0) Mostrar("La Iglesia estara lista en " + Mathf.CeilToInt((float)espera) + " s.");
        else if (_jugador.Vida >= _jugador.VidaMax) Mostrar("Ya tienes la vida completa.");
        else Mostrar("No alcanza el oro (" + _iglesia.CostoOroPorUso + ").");
    }

    private void AlConstruirse(EdificioModel edificio)
    {
        string nombre = edificio.Nombre;
        MainThreadDispatcher.Encolar(() => Mostrar(nombre + " terminada."));
    }

    private void Mostrar(string mensaje)
    {
        if (textoMensaje != null) textoMensaje.text = mensaje;
    }
}
