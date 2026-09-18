using ImperiosEnGuerra.Modelo;

namespace ImperiosEnGuerra.Modelo.Recursos
{
	// Enumeración para definir los tipos de armas sin necesidad de crear subclases
	public enum TipoArma
	{
		Espada,
		Arco,
		Lanza,
		Cuchillo,
		Hacha
	}

	public class ArmaModel : RecursoModel
	{
		// Propiedades públicas para acceder a las estadísticas del arma
		public float Fuerza { get; set; }
		public float Rango { get; set; }
		public TipoArma Tipo { get; set; }

		// Constructor que inicializa tanto las propiedades del RecursoModel como las del ArmaModel
		public ArmaModel(string nombre, int cantidad, float fuerza, float rango, TipoArma tipo)
			: base(nombre, cantidad)
		{
			Fuerza = fuerza;
			Rango = rango;
			Tipo = tipo;
		}

		public void Dano()
		{
			// Lógica genérica para infligir daño
		}

		public void Compra()
		{
			// Lógica genérica de compra del recurso
		}
	}
}