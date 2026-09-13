using System.Collections.Generic;
using ImperiosEnGuerra.Modelo.Edificios;

namespace ImperiosEnGuerra.Modelo
{
    public class JugadorModel
    {
        public string Nombre { get; set; }
        public bool EsIA { get; set; }
        public Civilizacion Civilizacion { get; set; }

        // ── Recursos ──────────────────────────────────────────────────
        // Se generan automáticamente mediante hilos en RecursoController
        public int Oro    { get; set; } = 200;
        public int Madera { get; set; } = 150;
        public int Comida { get; set; } = 100;
        public int Armas  { get; set; } = 0;

        // ── Unidades y Edificios ───────────────────────────────────────
        public List<UnidadModel>   Unidades  { get; set; } = new List<UnidadModel>();
        public List<EdificioModel> Edificios { get; set; } = new List<EdificioModel>();

        // El edificio principal — si cae, el jugador pierde
        public EdificioModel EdificioPrincipal { get; set; }

        // ── Estado ────────────────────────────────────────────────────
        public bool Perdio => EdificioPrincipal == null || !EdificioPrincipal.EstaEnPie;

        public JugadorModel(string nombre, Civilizacion civilizacion, bool esIA = false)
        {
            Nombre       = nombre;
            Civilizacion = civilizacion;
            EsIA         = esIA;
        }

        public bool TieneRecursos(int oro = 0, int madera = 0,
                                  int comida = 0, int armas = 0)
        {
            return Oro    >= oro   &&
                   Madera >= madera &&
                   Comida >= comida &&
                   Armas  >= armas;
        }

        public void GastarRecursos(int oro = 0, int madera = 0,
                                   int comida = 0, int armas = 0)
        {
            Oro    -= oro;
            Madera -= madera;
            Comida -= comida;
            Armas  -= armas;
        }

        public void AgregarRecurso(TipoRecurso tipo, int cantidad)
        {
            switch (tipo)
            {
                case TipoRecurso.Oro:    Oro    += cantidad; break;
                case TipoRecurso.Madera: Madera += cantidad; break;
                case TipoRecurso.Comida: Comida += cantidad; break;
                case TipoRecurso.Armas:  Armas  += cantidad; break;
            }
        }
    }
}
