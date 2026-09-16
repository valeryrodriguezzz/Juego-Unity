namespace ImperiosEnGuerra.Modelo
{
    public class CivilizacionModel
    {
        // ATRIBUTOS PRIVADOS (-):
        private string imperio;

        public string Imperio
        {
            get { return imperio; }
            set { imperio = value; }
        }

        public CivilizacionModel(string imperio)
        {
            this.imperio = imperio;
        }
    }
}