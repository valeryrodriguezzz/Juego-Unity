using System;

namespace ImperiosEnGuerra.Modelo
{
    // Estados posibles del territorio:
    public enum EstadoTerritorio
    {
        Neutral,
        Base,
        Conquistado
    }

    public class TerritorioModel
    {
        // ATRIBUTOS PRIVADOS (-):
        private string imperio;
        private float coordenada;
        private EstadoTerritorio estadoActual;

        private CivilizacionModel civilizacion;

        // --- PROPIEDADES PÚBLICAS ---
        // (Permiten leer o modificar los datos desde los Controladores)
        public string Imperio
        {
            get { return imperio; }
            set { imperio = value; }
        }

        public float Coordenada
        {
            get { return coordenada; }
            set { coordenada = value; }
        }

        public CivilizacionModel Civilizacion
        {
            get { return civilizacion; }
            set { civilizacion = value; }
        }

        // --- CONSTRUCTOR ---
        public TerritorioModel(float coordenada, string imperio = "Ninguno", EstadoTerritorio estadoInicial = EstadoTerritorio.Neutral, CivilizacionModel civilizacion = null)
        {
            this.coordenada = coordenada;
            this.imperio = imperio;
            this.estadoActual = estadoInicial;
            this.civilizacion = civilizacion;
        }

        public EstadoTerritorio Estado()
        {
            return estadoActual;
        }

        // Conquista:
        public void ConquistarTerritorio(string imperioConquistador)
        {
            // La base no debería cambiar a estado "Conquistado" de esta forma, solo se permite conquistar zonas neutrales o arrebatárselas a otro.
            if (estadoActual == EstadoTerritorio.Neutral || estadoActual == EstadoTerritorio.Conquistado)
            {
                this.imperio = imperioConquistador;
                this.estadoActual = EstadoTerritorio.Conquistado;
            }
        }
    }
}