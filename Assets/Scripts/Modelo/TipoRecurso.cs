using System;

namespace ImperiosEnGuerra.Modelo
{
    /// <summary>
    /// Los 4 recursos del juego.
    ///
    /// DONDE GUARDARLO: Assets/Scripts/Modelo/ (al lado de RecursoModel.cs),
    /// NO dentro de la carpeta Armas. Va en el namespace ImperiosEnGuerra.Modelo
    /// porque no es algo exclusivo de las armas.
    ///
    /// POR QUE EXISTE: tu RecursoModel identifica el recurso con un string
    /// ("Oro", "Madera"...). Eso funciona, pero el compilador no te avisa si
    /// escribes "madera" o "Maderra": el bug aparece en tiempo de ejecucion y
    /// es dificil de encontrar. Con un enum, un error de escritura no compila.
    ///
    /// NO tienes que cambiar RecursoModel: la clase RecursoTipoHelper de abajo
    /// traduce entre el string que ya usas y el enum. Si mas adelante quieren
    /// migrar RecursoModel al enum, el resto del sistema sigue funcionando igual.
    /// </summary>
    public enum TipoRecurso
    {
        Oro,
        Madera,
        Comida,
        Armas
    }

    /// <summary>
    /// Puente entre el string de RecursoModel.Tipo y el enum TipoRecurso.
    /// Es una clase static de solo lectura, asi que se puede usar desde
    /// cualquier hilo sin lock.
    /// </summary>
    public static class RecursoTipoHelper
    {
        /// <summary>
        /// Convierte "Oro", "oro", "ORO", " Madera " ... al enum.
        /// Si no reconoce el texto lanza excepcion, para que el error salga
        /// de una vez y no se traduzca en un recurso equivocado en silencio.
        /// </summary>
        public static TipoRecurso Desde(string tipo)
        {
            if (TryDesde(tipo, out TipoRecurso resultado))
                return resultado;

            throw new ArgumentException("Tipo de recurso no reconocido: '" + tipo + "'", nameof(tipo));
        }

        /// <summary>Version que no lanza excepcion: devuelve false si no lo reconoce.</summary>
        public static bool TryDesde(string tipo, out TipoRecurso resultado)
        {
            resultado = TipoRecurso.Oro;

            if (string.IsNullOrWhiteSpace(tipo))
                return false;

            switch (tipo.Trim().ToLowerInvariant())
            {
                case "oro":
                    resultado = TipoRecurso.Oro;
                    return true;

                case "madera":
                    resultado = TipoRecurso.Madera;
                    return true;

                case "comida":
                    resultado = TipoRecurso.Comida;
                    return true;

                case "armas":
                case "arma":
                    resultado = TipoRecurso.Armas;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Del enum al string, para pasarselo al constructor de RecursoModel.
        /// Usa SIEMPRE esto al crear los nodos y nunca vuelves a tener
        /// un "Madera" mal escrito rondando por el proyecto.
        /// </summary>
        public static string ATexto(TipoRecurso tipo)
        {
            switch (tipo)
            {
                case TipoRecurso.Oro: return "Oro";
                case TipoRecurso.Madera: return "Madera";
                case TipoRecurso.Comida: return "Comida";
                case TipoRecurso.Armas: return "Armas";
                default: return tipo.ToString();
            }
        }
    }
}
