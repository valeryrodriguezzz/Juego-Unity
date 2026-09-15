using UnityEngine;

//MODELO

public abstract class PersonajeModel
{
    public string Nombre;
    public string Rol;        // tipo de personaje elegido
    public string Imperio;    // Ej: "Grecia"
    public int Vida;
    public int VidaMax;
    public int Monedas;
    public int Nivel;
    public int NivelFuerza;

    protected PersonajeModel(string nombre, string rol, string imperio)
    {
        Nombre = nombre;
        Rol = rol;
        Imperio = imperio;

        VidaMax = 100;
        Vida = VidaMax;
        Monedas = 0;
        Nivel = 1;
        NivelFuerza = 1;
    }

    public virtual void Atacar()
    {
        Debug.Log(Nombre + " ataca.");
    }

    public virtual void Movimiento()
    {
        Debug.Log(Nombre + " se mueve.");
    }

    public virtual void Conseguir_Recursos()
    {
        Debug.Log(Nombre + " recolecta recursos.");
    }

    public virtual void Curarse()
    {
        Vida = Mathf.Min(Vida + 10, VidaMax);
        Debug.Log(Nombre + " se cura. Vida actual: " + Vida);
    }

    public virtual void Construir()
    {
        Debug.Log(Nombre + " construye.");
    }

    public virtual void Gasto_Recursos()
    {
        Debug.Log(Nombre + " gasta recursos.");
    }
}