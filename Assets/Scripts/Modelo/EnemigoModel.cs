using UnityEngine;

public class EnemigoModel : PersonajeModel
{
    public EnemigoModel(string nombre, string imperio) : base(nombre, "Enemigo", imperio)
    {
    }

    public override void Atacar()
    {
        Debug.Log(Nombre + " (Enemigo) ataca con fuerza " + NivelFuerza);
    }

    // Aqui mas adelante conectaremos la logica de IA (hilos) que
    // decida cuando llamar a Atacar(), Movimiento(), Construir(), etc.
}