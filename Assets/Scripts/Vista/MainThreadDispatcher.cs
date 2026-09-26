using System;
using System.Collections.Concurrent;
using UnityEngine;

// MainThreadDispatcher: permite que los hilos del Modelo actualicen la UI de Unity.
//
// POR QUE EXISTE:
// Unity solo permite tocar su API (Text, Image, Debug.Log, etc.) desde el hilo
// principal. Los hilos de RecursoModel, ArmaModel, TiendaArmasModel y PartidaModel
// corren en segundo plano y NO pueden tocar Unity directamente o se lanza una excepcion.
//
// COMO FUNCIONA:
// Los hilos llaman MainThreadDispatcher.Encolar(() => { codigo de Unity aqui });
// y en el proximo Update() (hilo principal) ese codigo se ejecuta de forma segura.
//
// COMO USARLO EN UNITY:
// 1. Crea un GameObject vacio en la escena llamado "MainThreadDispatcher".
// 2. Arrastra este script sobre ese GameObject.
// 3. Marca el GameObject como DontDestroyOnLoad para que sobreviva entre escenas.
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> _cola = new ConcurrentQueue<Action>();

    private static MainThreadDispatcher _instancia;

    private void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        _instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    // Los hilos llaman este metodo para encolar trabajo.
    // Es thread-safe: ConcurrentQueue no necesita lock.
    public static void Encolar(Action accion)
    {
        if (accion != null)
            _cola.Enqueue(accion);
    }

    // Update() corre en el hilo principal de Unity.
    // Aqui si se puede tocar la UI, los GameObjects, etc.
    private void Update()
    {
        while (_cola.TryDequeue(out Action accion))
        {
            try { accion(); }
            catch (Exception ex)
            {
                Debug.LogWarning("[MainThreadDispatcher] Error ejecutando accion: " + ex.Message);
            }
        }
    }
}
