namespace ImperiosEnGuerra.Modelo
{
    // =====================================================================
    //  Las 4 subclases concretas de RecursoModel.
    //
    //  DONDE VA: Assets/Scripts/Modelo/  (al lado de RecursoModel.cs)
    //
    //  Cada una solo tiene que hacer dos cosas:
    //    1. Pasarle sus datos al constructor de la clase base.
    //    2. Decir cuanto produce por segundo (ProduccionPorSegundo), que es
    //       lo que el hilo de regeneracion le va sumando al nodo.
    //
    //  Todo lo demas (el hilo, el lock, Recolectar, Gasto) ya lo heredan.
    //
    //  SI VALERY YA TIENE ALGUNA DE ESTAS CLASES: no copies esa. Borra la de
    //  aqui y deja la suya, pero revisa que el constructor reciba
    //  (string tipo, int cantidadInicial, float coordenadas), que es lo que
    //  RecursoNodoController espera.
    // =====================================================================

    /// <summary>
    /// Una roca de oro. Se mina con el pico.
    /// Regenera lento: el oro es el recurso escaso.
    /// </summary>
    public class OroModel : RecursoModel
    {
        public OroModel(string tipo, int cantidadInicial, float coordenadas)
            : base(tipo, cantidadInicial, coordenadas)
        {
        }

        /// <summary>Constructor corto: el tipo se pone solo.</summary>
        public OroModel(int cantidadInicial, float coordenadas)
            : base(RecursoTipoHelper.ATexto(TipoRecurso.Oro), cantidadInicial, coordenadas)
        {
        }

        protected override int ProduccionPorSegundo()
        {
            return 1;
        }
    }

    /// <summary>
    /// Un arbol. Se tala con el hacha.
    /// </summary>
    public class MaderaModel : RecursoModel
    {
        public MaderaModel(string tipo, int cantidadInicial, float coordenadas)
            : base(tipo, cantidadInicial, coordenadas)
        {
        }

        public MaderaModel(int cantidadInicial, float coordenadas)
            : base(RecursoTipoHelper.ATexto(TipoRecurso.Madera), cantidadInicial, coordenadas)
        {
        }

        protected override int ProduccionPorSegundo()
        {
            return 2;
        }
    }

    /// <summary>
    /// Una oveja o un rebaño. Se caza con el cuchillo.
    /// Es el que mas rapido repuebla.
    /// </summary>
    public class ComidaModel : RecursoModel
    {
        public ComidaModel(string tipo, int cantidadInicial, float coordenadas)
            : base(tipo, cantidadInicial, coordenadas)
        {
        }

        public ComidaModel(int cantidadInicial, float coordenadas)
            : base(RecursoTipoHelper.ATexto(TipoRecurso.Comida), cantidadInicial, coordenadas)
        {
        }

        protected override int ProduccionPorSegundo()
        {
            return 3;
        }
    }

    /// <summary>
    /// La tienda del mapa.
    ///
    /// OJO, ESTE ES DISTINTO A LOS OTROS TRES: las armas no crecen del suelo,
    /// se compran. Por eso ProduccionPorSegundo devuelve 0 y su hilo de
    /// regeneracion no hace nada util (ni hace falta arrancarlo).
    ///
    /// El stock de armas de la tienda lo maneja TiendaArmasModel, que tiene su
    /// propio hilo de reabastecimiento. Esta clase existe solo para que el
    /// edificio tienda tenga un Modelo como los demas nodos del mapa y pueda
    /// llevar la cuenta de cuantas armas ha vendido.
    /// </summary>
    public class ArmasModel : RecursoModel
    {
        public ArmasModel(string tipo, int cantidadInicial, float coordenadas)
            : base(tipo, cantidadInicial, coordenadas)
        {
        }

        public ArmasModel(int cantidadInicial, float coordenadas)
            : base(RecursoTipoHelper.ATexto(TipoRecurso.Armas), cantidadInicial, coordenadas)
        {
        }

        protected override int ProduccionPorSegundo()
        {
            return 0; // no se regenera solo
        }
    }
}