using UnityEngine;

public class JugadorModel : PersonajeModel
{
    // El rol para un Jugador siempre es "Jugador"; por eso no lo pedimos
    // como parámetro, solo el nombre (elegido en la UI) y el imperio.
    public JugadorModel(string nombre, string imperio) : base(nombre, "Jugador", imperio)
    {
    }

    public override void Atacar()
    {
        Debug.Log(Nombre + " (Jugador) ataca con fuerza " + NivelFuerza);
    }

    // Puedes sobreescribir aquí cualquier otro método si el Jugador
    // se comporta distinto al comportamiento base de Personaje.
}