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
        // Contador estático para asignar Ids únicos automáticamente
        private static int _contadorId = 0;

        // ATRIBUTOS PRIVADOS (-):
        private int id;
        private string nombre;
        private string imperio;
        private float coordenada;
        private EstadoTerritorio estadoActual;
        private CivilizacionModel civilizacion;

        // --- PROPIEDADES PÚBLICAS ---

        public int Id
        {
            get { return id; }
        }

        // Nombre visible del territorio (igual al Imperio si no se da otro)
        public string Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        }

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

        // Dificultad del territorio: datos de la IA que defiende este territorio.
        public int VidaIA { get; private set; }
        public int FuerzaIA { get; private set; }
        public int MsEntreAtaquesIA { get; private set; }
        public int VidaCentroUrbano { get; private set; }

        // Propiedades calculadas para el Controlador
        public bool EsBase => estadoActual == EstadoTerritorio.Base;
        public bool EsConquistado => estadoActual == EstadoTerritorio.Conquistado;

        // --- CONSTRUCTOR ---
        public TerritorioModel(float coordenada, string imperio = "Ninguno",
            EstadoTerritorio estadoInicial = EstadoTerritorio.Neutral,
            CivilizacionModel civilizacion = null,
            int vidaIA = 100, int fuerzaIA = 10, int msEntreAtaquesIA = 2000,
            int vidaCentroUrbano = 400)
        {
            this.VidaIA           = vidaIA;
            this.FuerzaIA         = fuerzaIA;
            this.MsEntreAtaquesIA = msEntreAtaquesIA;
            this.VidaCentroUrbano = vidaCentroUrbano;
            this.id          = ++_contadorId;
            this.coordenada  = coordenada;
            this.imperio     = imperio;
            this.nombre      = imperio;   // por defecto el nombre es el imperio
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
            // Solo se permite conquistar zonas neutrales o arrebatárselas a otro (no la base propia).
            if (estadoActual == EstadoTerritorio.Neutral || estadoActual == EstadoTerritorio.Conquistado)
            {
                this.imperio = imperioConquistador;
                this.estadoActual = EstadoTerritorio.Conquistado;
            }
        }
    }
}