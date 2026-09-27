using System;
using ImperiosEnGuerra.Modelo.Armas;
using UnityEngine;

/// <summary>
/// CONTROLADOR: cambia las animaciones del Pawn segun la herramienta que
/// lleve equipada. Va en el GameObject Jugador.
///
/// Tiny Swords trae al Pawn dibujado con cada herramienta (hacha, pico,
/// martillo, cuchillo), asi que si el jugador se equipa el pico, se le ve
/// caminando y golpeando con el pico.
///
/// COMO LO DETECTA
/// Mira en cada Update que arma tiene puesta el inventario y, si cambio desde
/// la ultima vez, le cambia el Animator Override Controller. Comparar dos
/// referencias 60 veces por segundo no cuesta nada.
///
/// Antes esto iba por evento y pasaba por el MainThreadDispatcher, porque
/// quien equipa puede ser la tienda o la mochila y el evento podia llegar
/// desde otro hilo. El problema es que asi quedaba dependiendo de que el
/// MainThreadDispatcher existiera y estuviera activo en ESA escena: si algo
/// fallaba ahi, el arma se equipaba bien en el Modelo pero el muñeco no
/// cambiaba nunca, y sin un solo error en consola. Preguntando en Update no
/// depende de nada externo y funciona igual en las cinco escenas.
///
/// COMO PREPARARLO EN UNITY:
/// 1. Ten listo tu Animator Controller normal (el base), con los tres estados:
///    Idle, correr y atacar, y los parametros EstaCorriendo y Atacar.
/// 2. Por cada herramienta, en Project: clic derecho -> Create -> Animation ->
///    Animator Override Controller.
/// 3. Abre cada uno, arrastra tu controller base al campo Controller de
///    arriba, y en la lista que aparece reemplaza los tres clips por los de
///    esa herramienta. Son tres arrastres por herramienta.
/// 4. Selecciona el Jugador -> Add Component -> AnimacionPorArmaController.
/// 5. En Animaciones Por Arma pon Size 4 y en cada elemento elige la
///    herramienta y arrastra su Override Controller.
///
/// Lo que dejes sin asignar se queda con la animacion base, asi que puedes
/// ir completandolo de a poco sin que nada se rompa.
/// </summary>
public class AnimacionPorArmaController : MonoBehaviour
{
    [Serializable]
    public class AnimacionDeArma
    {
        public TipoArma arma;
        public RuntimeAnimatorController animaciones;
    }

    [Header("Un Animator Override Controller por herramienta")]
    [SerializeField] private AnimacionDeArma[] animacionesPorArma;

    [Header("Diagnostico")]
    [Tooltip("Escribe en consola cada vez que cambia de animaciones. Util " +
             "mientras se configura; despues se puede desmarcar.")]
    [SerializeField] private bool avisarEnConsola = true;

    private Animator _animator;
    private JugadorArmasController _armas;
    private RuntimeAnimatorController _animacionesBase;

    // La ultima arma que se le aplico al Animator. Se guarda para no reasignar
    // el controller en cada frame: hacerlo reinicia la maquina de estados y el
    // muñeco se quedaria congelado en el primer frame de su animacion.
    private ArmaModel _ultimaAplicada;
    private bool _yaAplicoAlgunaVez;

    private void Awake()
    {
        // El Animator normalmente esta en el mismo objeto, pero si alguien lo
        // pone en el hijo del sprite tambien se encuentra.
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _armas = GetComponent<JugadorArmasController>();

        // Se guarda el controller original para volver a el cuando no hay
        // herramienta equipada (asi empieza el Pawn) o cuando la que lleva no
        // tiene animaciones propias todavia.
        if (_animator != null)
            _animacionesBase = _animator.runtimeAnimatorController;
    }

    private void Start()
    {
        if (_animator == null)
            Debug.LogError("[AnimacionArma] No encontre ningun Animator en " + name +
                           " ni en sus hijos. Sin eso no puedo cambiar las animaciones.");

        if (_armas == null)
            Debug.LogError("[AnimacionArma] Falta el JugadorArmasController en " + name +
                           ". Este script tiene que ir en el MISMO objeto.");

        if (avisarEnConsola)
            Debug.Log("[AnimacionArma] Listo en " + name + ". Overrides configurados: "
                      + Configurados() + " de 4. Base: "
                      + (_animacionesBase != null ? _animacionesBase.name : "ninguno"));
    }

    private void Update()
    {
        if (_animator == null || _armas == null) return;

        InventarioArmasModel inventario = _armas.Inventario;
        if (inventario == null) return;

        // Equipada ya es thread-safe por dentro (toma su propio candado), asi
        // que se puede leer aqui aunque los hilos de las armas esten
        // trabajando al mismo tiempo.
        ArmaModel actual = inventario.Equipada;

        // Solo se toca el Animator cuando de verdad cambio el arma.
        if (_yaAplicoAlgunaVez && ReferenceEquals(actual, _ultimaAplicada)) return;

        _ultimaAplicada = actual;
        _yaAplicoAlgunaVez = true;

        Aplicar(actual);
    }

    private void Aplicar(ArmaModel arma)
    {
        RuntimeAnimatorController destino;
        string motivo;

        if (arma == null)
        {
            // Sin herramienta: el muñeco con las manos vacias.
            destino = _animacionesBase;
            motivo = "sin herramienta, animaciones base";
        }
        else
        {
            RuntimeAnimatorController encontrado = Buscar(arma.Tipo);

            // Si esa herramienta no tiene override, se deja la base: asi el
            // muñeco sigue moviendose aunque falten sets por hacer.
            destino = encontrado != null ? encontrado : _animacionesBase;
            motivo = encontrado != null
                ? arma.Nombre + " -> " + encontrado.name
                : arma.Nombre + " no tiene override asignado, se queda con la base";
        }

        if (destino != null && !ReferenceEquals(_animator.runtimeAnimatorController, destino))
            _animator.runtimeAnimatorController = destino;

        if (avisarEnConsola)
            Debug.Log("[AnimacionArma] " + motivo);
    }

    private RuntimeAnimatorController Buscar(TipoArma tipo)
    {
        if (animacionesPorArma == null) return null;

        foreach (AnimacionDeArma a in animacionesPorArma)
            if (a != null && a.arma == tipo) return a.animaciones;

        return null;
    }

    private int Configurados()
    {
        if (animacionesPorArma == null) return 0;

        int n = 0;
        foreach (AnimacionDeArma a in animacionesPorArma)
            if (a != null && a.animaciones != null) n++;

        return n;
    }
}
