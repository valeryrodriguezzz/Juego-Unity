using System;
using System.Collections.Generic;
using ImperiosEnGuerra.Modelo.Edificios;

namespace ImperiosEnGuerra.Modelo
{
    /// ===================================================================
    /// los recursos los tocan varios hilos a la vez (el hilo de
    ///  regeneracion de cada nodo, el jugador recolectando, la tienda
    ///  cobrando, y mas adelante el hilo de la IA). Sin lock, dos hilos leen
    ///  el mismo valor, cada uno le suma lo suyo y el ultimo pisa al otro:
    ///  se pierden recursos sin que nadie se entere. Es el error clasico de
    ///  "leer-modificar-escribir".
    /// ===================================================================
    public class JugadorModel
    {
        // Un solo candado para todo el estado numerico del jugador.
        // Uno solo y no uno por recurso, porque asi se puede cobrar
        // "30 oro y 10 madera" de forma atomica (todo o nada). Con cuatro
        // candados separados eso seria imposible.
        private readonly object _lock = new object();

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

        // Vida con lock. Cuando el combate corra en su propio hilo
        // (la IA atacando), dos golpes simultaneos sin lock restan uno solo.
        public int Vida
        {
            get { lock (_lock) { return vida; } }
            set { lock (_lock) { vida = value; } }
        }

        public int VidaMax
        {
            get { lock (_lock) { return vidaMax; } }
            set { lock (_lock) { vidaMax = value; } }
        }

        // Los cuatro recursos con lock. La firma es la misma de
        // antes, asi que cualquier codigo que hiciera jugador.Oro sigue igual.
        public int Oro
        {
            get { lock (_lock) { return oro; } }
            set { lock (_lock) { oro = value; } }
        }

        public int Madera
        {
            get { lock (_lock) { return madera; } }
            set { lock (_lock) { madera = value; } }
        }

        public int Comida
        {
            get { lock (_lock) { return comida; } }
            set { lock (_lock) { comida = value; } }
        }

        public int Armas
        {
            get { lock (_lock) { return armas; } }
            set { lock (_lock) { armas = value; } }
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

        // Regla del juego: se pierde unicamente al quedarse sin vida.
        public bool Perdio => Vida <= 0;

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

        // El descuento va dentro del lock del objetivo.
        public void Atacar(JugadorModel objetivo)
        {
            // Reduce la vida del objetivo según NivelFuerza
            int fuerza = NivelFuerza;

            lock (objetivo._lock)
            {
                objetivo.vida -= fuerza;
                if (objetivo.vida < 0) objetivo.vida = 0;
            }
        }

        public void Conseguir_Recursos(string tipoRecurso, int cantidad)
        {
            // Llamado por el hilo de recolección cuando produce recursos
            if (cantidad == 0 || string.IsNullOrWhiteSpace(tipoRecurso)) return;

            lock (_lock)
            {
                switch (tipoRecurso.Trim().ToLowerInvariant())
                {
                    case "oro": oro += cantidad; if (oro < 0) oro = 0; break;
                    case "madera": madera += cantidad; if (madera < 0) madera = 0; break;
                    case "comida": comida += cantidad; if (comida < 0) comida = 0; break;
                    case "armas": armas += cantidad; if (armas < 0) armas = 0; break;
                }
            }
        }

        public void Curarse(int cantidad)
        {
            lock (_lock)
            {
                vida += cantidad;
                if (vida > vidaMax) vida = vidaMax; // No supera el máximo
            }
        }

        // Se dispara desde el hilo de construccion del edificio (NO el principal de Unity)
        public event Action<EdificioModel> OnEdificioConstruido;

        // Construye un edificio: cobra el costo (oro y madera, todo o nada) y arranca su
        // hilo de construccion dentro del modelo. Devuelve false si no alcanzan los recursos
        // o el edificio ya esta construido / en construccion.
        public bool Construir(EdificioModel edificio)
        {
            if (edificio == null) return false;

            // Suscribirse ANTES de iniciar, por si el edificio termina al instante
            edificio.OnConstruccionTerminada += AlTerminarConstruccion;

            if (!edificio.IniciarConstruccion(this))
            {
                edificio.OnConstruccionTerminada -= AlTerminarConstruccion;
                return false;
            }

            edificio.EsDeJugador = !esIA;
            lock (_lock)
            {
                if (!Edificios.Contains(edificio)) Edificios.Add(edificio);
            }
            return true;
        }

        private void AlTerminarConstruccion(EdificioModel edificio)
        {
            OnEdificioConstruido?.Invoke(edificio);
        }

        // Copia de la lista, segura de recorrer aunque otro hilo agregue edificios
        public List<EdificioModel> ObtenerEdificios()
        {
            lock (_lock) { return new List<EdificioModel>(Edificios); }
        }

        // Apaga los hilos de construccion y produccion de todos los edificios del jugador
        public void DetenerEdificios()
        {
            foreach (var edificio in ObtenerEdificios())
                edificio.Detener();
        }

        // [7] CAMBIO: con lock y sin dejar los recursos en negativo.
        // Antes, Gasto_Recursos(500) con 200 de oro dejaba al jugador en -300.
        public void Gasto_Recursos(int monedas = 0, int madera = 0,
                                    int comida = 0, int armas = 0)
        {
            lock (_lock)
            {
                this.oro -= monedas;
                this.madera -= madera;
                this.comida -= comida;
                this.armas -= armas;

                if (this.oro < 0) this.oro = 0;
                if (this.madera < 0) this.madera = 0;
                if (this.comida < 0) this.comida = 0;
                if (this.armas < 0) this.armas = 0;
            }
        }

        public bool TieneRecursos(int monedas = 0, int madera = 0,
                                   int comida = 0, int armas = 0)
        {
            lock (_lock)
            {
                return this.oro >= monedas &&
                       this.madera >= madera &&
                       this.comida >= comida &&
                       this.armas >= armas;
            }
        }


        // Comprueba y cobra DENTRO del mismo lock, y devuelve si alcanzo.
        // Si no alcanza, no descuenta nada.
        //
        // Por que no basta con lo que ya habia: hacer
        //     if (jugador.TieneRecursos(30)) jugador.Gasto_Recursos(30);
        // son dos operaciones separadas. Dos hilos con 30 de oro en la bolsa
        // pueden pasar los dos por el 'if' antes de que alguno cobre, y
        // terminan comprando los dos con el mismo oro. Es el error de
        // "comprobar y despues actuar" (check-then-act), el mismo que
        // corregimos en RecursoModel.Recolectar().
        public bool GastarSiAlcanza(int monedas = 0, int madera = 0,
                                     int comida = 0, int armas = 0)
        {
            lock (_lock)
            {
                if (this.oro < monedas || this.madera < madera ||
                    this.comida < comida || this.armas < armas)
                    return false;

                this.oro -= monedas;
                this.madera -= madera;
                this.comida -= comida;
                this.armas -= armas;
                return true;
            }
        }

        // El Equals anterior decia que
        // dos jugadores son el MISMO si tienen el mismo oro. Con eso, el jugador
        // y la IA se vuelven "iguales" en cuanto empatan en oro, y cualquier
        // lista.Contains(jugador) o Dictionary con jugadores da resultados
        // equivocados. Ademas, sobreescribir Equals sin GetHashCode genera el
        // warning CS0659 y rompe los diccionarios.
        //
        // Si prefieren dejarlo como estaba, borren estos dos metodos: para un
        // jugador, la comparacion por referencia (la de por defecto) es la
        // correcta, porque no hay dos jugadores distintos que sean "el mismo".
        public override bool Equals(object obj)
        {
            return obj is JugadorModel model &&
                   nombre == model.nombre &&
                   esIA == model.esIA;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (nombre != null ? nombre.GetHashCode() : 0);
            hash = hash * 31 + esIA.GetHashCode();
            return hash;
        }
    }
}
