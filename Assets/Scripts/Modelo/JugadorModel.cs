using System.Collections.Generic;
using ImperiosEnGuerra.Modelo.Edificios;

namespace ImperiosEnGuerra.Modelo
{
    public class JugadorModel
    {
        // ATRIBUTOS PRIVADOS (-)
        private string nombre;
        private string rol;
        private CivilizacionModel civilizacion;
        private int vida;
        private int vidaMax;
        private int oro;
        private int madera;
        private int comida;
        private int armas;
        private int nivel;
        private int nivelFuerza;
        private bool esIA;

        // --- PROPIEDADES PÚBLICAS ---
        public string Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        }

        public string Rol
        {
            get { return rol; }
            set { rol = value; }
        }

        public CivilizacionModel Civilizacion
        {
            get { return civilizacion; }
            set { civilizacion = value; }
        }

        public int Vida
        {
            get { return vida; }
            set { vida = value; }
        }

        public int VidaMax
        {
            get { return vidaMax; }
            set { vidaMax = value; }
        }

        public int Oro
        {
            get { return oro; }
            set { oro = value; }
        }

        public int Madera
        {
            get { return madera; }
            set { madera = value; }
        }

        public int Comida
        {
            get { return comida; }
            set { comida = value; }
        }

        public int Armas
        {
            get { return armas; }
            set { armas = value; }
        }

        public int Nivel
        {
            get { return nivel; }
            set { nivel = value; }
        }

        public int NivelFuerza
        {
            get { return nivelFuerza; }
            set { nivelFuerza = value; }
        }

        public bool EsIA
        {
            get { return esIA; }
            set { esIA = value; }
        }

        // Unidades y Edificios del jugador
        public List<UnidadModel> Unidades { get; set; } = new List<UnidadModel>();
        public List<EdificioModel> Edificios { get; set; } = new List<EdificioModel>();
        public EdificioModel EdificioPrincipal { get; set; }

        public bool Perdio => EdificioPrincipal == null || !EdificioPrincipal.EstaEnPie;

        // --- CONSTRUCTOR ---
        public JugadorModel(string nombre, CivilizacionModel civilizacion, bool esIA = false)
        {
            this.nombre = nombre;
            this.civilizacion = civilizacion;
            this.esIA = esIA;
            this.vida = 100;
            this.vidaMax = 100;
            this.oro = 200;  // Empieza con 200 de oro
            this.madera = 150;
            this.comida = 100;
            this.armas = 0;
            this.nivel = 1;
            this.nivelFuerza = 10;
        }

        // MÉTODOS:

        // El Controlador implementa la lógica completa

        public void Atacar(JugadorModel objetivo)
        {
            // Reduce la vida del objetivo según NivelFuerza
            objetivo.Vida -= nivelFuerza;
        }

        public void Movimiento(int nuevaFila, int nuevaColumna)
        {
            // El Controlador valida y mueve el personaje en el mapa
        }

        public void Conseguir_Recursos(string tipoRecurso, int cantidad)
        {
            // Llamado por el hilo de recolección cuando produce recursos
            switch (tipoRecurso)
            {
                case "Oro": oro += cantidad; break;
                case "Madera": madera += cantidad; break;
                case "Comida": comida += cantidad; break;
            }
        }

        public void Curarse(int cantidad)
        {
            vida += cantidad;
            if (vida > vidaMax) vida = vidaMax; // No supera el máximo
        }

        public void Construir()
        {
            // El Controlador valida si tiene recursos y construye
        }

        public void Gasto_Recursos(int monedas = 0, int madera = 0,
                                    int comida = 0, int armas = 0)
        {
            this.oro -= monedas;
            this.madera -= madera;
            this.comida -= comida;
            this.armas -= armas;
        }

        public bool TieneRecursos(int monedas = 0, int madera = 0,
                                   int comida = 0, int armas = 0)
        {
            return this.oro >= monedas &&
                   this.madera >= madera &&
                   this.comida >= comida &&
                   this.armas >= armas;
        }

        public override bool Equals(object obj)
        {
            return obj is JugadorModel model &&
                   oro == model.oro;
        }
    }
}