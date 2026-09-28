using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ImperiosEnGuerra.Modelo.Armas;
using ImperiosEnGuerra.Modelo.Edificios;

namespace ImperiosEnGuerra.Modelo
{

    /// MODELO: la foto de una partida en un momento dado, y como se convierte
    /// en texto y vuelve.
    ///
    /// Es C# puro, sin una sola linea de Unity: se puede leer, escribir y
    /// probar fuera del juego. El Controlador (GuardadoController) es el que
    /// se encarga de meter y sacar este texto de un archivo, y el que sabe
    /// cosas de la escena (donde esta parado el muñequito) que aqui solo
    /// llegan como numeros.
    ///
    /// EL FORMATO
    /// Una linea por dato, clave=valor, y las listas repiten la clave:
    ///
    ///     nombre=Diana
    ///     escena=Juego
    ///     oro=240
    ///     arma=Hacha;38
    ///     arma=Pico;60
    ///     conquistado=Egipto
    ///     edificio=Juego;Granja;-304.10;-79.90;1
    ///
    /// Se eligio texto plano y no un formato binario por dos razones: se
    /// puede abrir con el Bloc de notas para revisar que se guardo, que en
    /// una sustentacion vale mucho, y va en la misma linea que los otros
    /// archivos del proyecto (configuracion.txt, log_partida.txt).


    public class PartidaGuardadaModel
    {
        public const int VERSION = 1;

        // --- quien y cuando ---
        public string Nombre = "Jugador";
        public string Fecha = "";

        // --- donde ---
        public string Escena = "Juego";
        public float X;
        public float Y;

        // --- estado del jugador ---
        public int Vida = 100;
        public int VidaMax = 100;
        public int Oro;
        public int Madera;
        public int Comida;
        public int Armamento;      // el recurso "Armas", no las herramientas
        public int Nivel = 1;
        public int NivelFuerza = 10;

        // -1 = no habia hambre que guardar
        public int Hambre = -1;

        // --- herramientas ---
        public string ArmaEquipada = "";
        public readonly List<ArmaGuardada> Armas = new List<ArmaGuardada>();

        // --- progreso ---
        public readonly List<string> Conquistados = new List<string>();
        public readonly List<EdificioGuardado> Edificios = new List<EdificioGuardado>();

        public class ArmaGuardada
        {
            public string Tipo;
            public int Durabilidad;
        }

        public class EdificioGuardado
        {
            public string Escena;
            public string Tipo;
            public float X;
            public float Y;
            public bool Construido;
        }

        // ────────────────────────────────────────────────────────────────
        //  Capturar
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Copia todo lo que se puede saber mirando solo al Modelo. La escena,
        /// la posicion, el hambre y los edificios los pone el Controlador
        /// despues, porque esos solo se saben desde la escena de Unity.
        /// </summary>
        public static PartidaGuardadaModel Capturar(PartidaModel partida)
        {
            var g = new PartidaGuardadaModel();

            if (partida == null || partida.Jugador == null) return g;

            JugadorModel j = partida.Jugador;

            g.Nombre = j.Nombre;
            g.Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

            g.Vida = j.Vida;
            g.VidaMax = j.VidaMax;
            g.Oro = j.Oro;
            g.Madera = j.Madera;
            g.Comida = j.Comida;
            g.Armamento = j.Armas;
            g.Nivel = j.Nivel;
            g.NivelFuerza = j.NivelFuerza;

            if (j.Armamento != null)
            {
                ArmaModel equipada = j.Armamento.Equipada;
                g.ArmaEquipada = equipada != null ? equipada.Tipo.ToString() : "";

                foreach (ArmaModel a in j.Armamento.Listar())
                    g.Armas.Add(new ArmaGuardada { Tipo = a.Tipo.ToString(), Durabilidad = a.Durabilidad });
            }

            if (partida.MapaMundial != null)
            {
                // Se guarda el Nombre y NO el Imperio. Al conquistar un
                // territorio, ConquistarTerritorio le cambia el Imperio a
                // "Grecia": si se guardara ese, los cuatro territorios
                // conquistados quedarian escritos como "Grecia" y al cargar no
                // habria forma de saber cual era cual. El Nombre no cambia.
                foreach (TerritorioModel t in partida.MapaMundial.Territorios)
                    if (t.EsConquistado) g.Conquistados.Add(t.Nombre);
            }

            return g;
        }

        // ────────────────────────────────────────────────────────────────
        //  Aplicar
        // ────────────────────────────────────────────────────────────────
        /// Vuelca esta foto sobre una partida recien creada. Igual que arriba,
        /// aqui solo se toca el Modelo: la posicion en el mapa y el hambre los
        /// aplica el Controlador cuando la escena ya esta cargada.
        public void Aplicar(PartidaModel partida)
        {
            if (partida == null || partida.Jugador == null) return;

            JugadorModel j = partida.Jugador;

            j.VidaMax = VidaMax;
            j.Vida = Vida;
            j.Nivel = Nivel;
            j.NivelFuerza = NivelFuerza;

            // Se asignan directo: las cuatro propiedades toman el lock del
            // JugadorModel por dentro, asi que aunque un hilo de produccion ya
            // este corriendo no se pierde ni se pisa ningun valor.
            j.Oro = Oro;
            j.Madera = Madera;
            j.Comida = Comida;
            j.Armas = Armamento;

            // El arsenal normalmente lo crea JugadorArmasController al entrar
            // al mapa, pero aqui todavia estamos en el Menu: si no existe, se
            // crea ya. Aquel lo encontrara hecho y lo reutilizara, que es
            // justo lo que hace cuando se vuelve de otro territorio.
            if (j.Armamento == null)
                j.Armamento = new InventarioArmasModel(TipoPersonaje.Trabajador);

            {
                foreach (ArmaGuardada guardada in Armas)
                {
                    TipoArma tipo;
                    if (!TryTipoArma(guardada.Tipo, out tipo)) continue;

                    if (!j.Armamento.Posee(tipo))
                        j.Armamento.Agregar(CatalogoArmas.Crear(tipo));

                    foreach (ArmaModel a in j.Armamento.Listar())
                        if (a.Tipo == tipo) a.RestaurarDurabilidad(guardada.Durabilidad);
                }

                TipoArma equipar;
                if (TryTipoArma(ArmaEquipada, out equipar) && j.Armamento.Posee(equipar))
                    j.Armamento.Equipar(equipar);
            }

            if (partida.MapaMundial != null)
            {
                foreach (TerritorioModel t in partida.MapaMundial.Territorios)
                    if (!t.EsBase && Conquistados.Contains(t.Nombre))
                        t.ConquistarTerritorio("Grecia");
            }
        }


        /// Recrea los EdificioModel guardados y los deja produciendo. Devuelve
        /// la lista emparejada con Edificios, en el mismo orden, para que el
        /// Controlador sepa donde poner cada uno en el mapa.

        public List<EdificioModel> RecrearEdificios(PartidaModel partida)
        {
            var creados = new List<EdificioModel>();

            if (partida == null || partida.Jugador == null) return creados;

            foreach (EdificioGuardado g in Edificios)
            {
                EdificioModel e = FabricaEdificios.Crear(g.Tipo);

                if (e == null) { creados.Add(null); continue; }

                if (g.Construido)
                {
                    // Ya estaba terminado: se marca como tal y se le vuelve a
                    // arrancar el hilo de produccion, que es lo que hace que
                    // una granja siga dando comida despues de cargar.
                    e.RestaurarComoConstruido(partida.Jugador);
                }

                partida.Jugador.AgregarEdificioRestaurado(e);
                creados.Add(e);
            }

            return creados;
        }

        private static bool TryTipoArma(string texto, out TipoArma tipo)
        {
            tipo = TipoArma.Hacha;

            if (string.IsNullOrEmpty(texto)) return false;

            foreach (TipoArma t in (TipoArma[])Enum.GetValues(typeof(TipoArma)))
            {
                if (string.Equals(t.ToString(), texto, StringComparison.OrdinalIgnoreCase))
                {
                    tipo = t;
                    return true;
                }
            }

            return false;
        }

        // ────────────────────────────────────────────────────────────────
        //  Texto
        // ────────────────────────────────────────────────────────────────

        public string ATexto()
        {
            var sb = new StringBuilder();

            sb.AppendLine("version=" + VERSION);
            sb.AppendLine("nombre=" + Limpiar(Nombre));
            sb.AppendLine("fecha=" + Limpiar(Fecha));
            sb.AppendLine("escena=" + Limpiar(Escena));
            sb.AppendLine("x=" + N(X));
            sb.AppendLine("y=" + N(Y));
            sb.AppendLine("vida=" + Vida);
            sb.AppendLine("vidamax=" + VidaMax);
            sb.AppendLine("oro=" + Oro);
            sb.AppendLine("madera=" + Madera);
            sb.AppendLine("comida=" + Comida);
            sb.AppendLine("armamento=" + Armamento);
            sb.AppendLine("nivel=" + Nivel);
            sb.AppendLine("fuerza=" + NivelFuerza);
            sb.AppendLine("hambre=" + Hambre);
            sb.AppendLine("equipada=" + Limpiar(ArmaEquipada));

            foreach (ArmaGuardada a in Armas)
                sb.AppendLine("arma=" + Limpiar(a.Tipo) + ";" + a.Durabilidad);

            foreach (string c in Conquistados)
                sb.AppendLine("conquistado=" + Limpiar(c));

            foreach (EdificioGuardado e in Edificios)
                sb.AppendLine("edificio=" + Limpiar(e.Escena) + ";" + Limpiar(e.Tipo) + ";" +
                              N(e.X) + ";" + N(e.Y) + ";" + (e.Construido ? "1" : "0"));

            return sb.ToString();
        }

        public static PartidaGuardadaModel DeTexto(string texto)
        {
            var g = new PartidaGuardadaModel();

            if (string.IsNullOrEmpty(texto)) return g;

            string[] lineas = texto.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (string cruda in lineas)
            {
                string linea = cruda.Trim();
                if (linea.Length == 0) continue;

                int igual = linea.IndexOf('=');
                if (igual <= 0) continue;

                string clave = linea.Substring(0, igual).Trim().ToLowerInvariant();
                string valor = linea.Substring(igual + 1).Trim();

                switch (clave)
                {
                    case "nombre":      g.Nombre = valor; break;
                    case "fecha":       g.Fecha = valor; break;
                    case "escena":      g.Escena = valor; break;
                    case "x":           g.X = F(valor); break;
                    case "y":           g.Y = F(valor); break;
                    case "vida":        g.Vida = E(valor, g.Vida); break;
                    case "vidamax":     g.VidaMax = E(valor, g.VidaMax); break;
                    case "oro":         g.Oro = E(valor, 0); break;
                    case "madera":      g.Madera = E(valor, 0); break;
                    case "comida":      g.Comida = E(valor, 0); break;
                    case "armamento":   g.Armamento = E(valor, 0); break;
                    case "nivel":       g.Nivel = E(valor, 1); break;
                    case "fuerza":      g.NivelFuerza = E(valor, 10); break;
                    case "hambre":      g.Hambre = E(valor, -1); break;
                    case "equipada":    g.ArmaEquipada = valor; break;

                    case "arma":
                    {
                        string[] p = valor.Split(';');
                        if (p.Length >= 2)
                            g.Armas.Add(new ArmaGuardada { Tipo = p[0], Durabilidad = E(p[1], 0) });
                        break;
                    }

                    case "conquistado":
                        if (valor.Length > 0) g.Conquistados.Add(valor);
                        break;

                    case "edificio":
                    {
                        string[] p = valor.Split(';');
                        if (p.Length >= 5)
                            g.Edificios.Add(new EdificioGuardado
                            {
                                Escena = p[0],
                                Tipo = p[1],
                                X = F(p[2]),
                                Y = F(p[3]),
                                Construido = p[4] == "1"
                            });
                        break;
                    }
                }
            }

            return g;
        }

        /// <summary>Una linea para la lista del menu.</summary>
        public string Resumen()
        {
            return Nombre + "   -   " + Escena +
                   "   -   " + Oro + " oro, " + Madera + " madera" +
                   "   -   " + Conquistados.Count + "/4 territorios";
        }

        // Los saltos de linea y los = romperian el formato; el ; separa campos.
        private static string Limpiar(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";

            return s.Replace("\r", " ").Replace("\n", " ")
                    .Replace("=", " ").Replace(";", " ").Trim();
        }

        private static string N(float v)
        {
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static float F(string s)
        {
            float v;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f;
        }

        private static int E(string s, int porDefecto)
        {
            int v;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : porDefecto;
        }
    }
}
