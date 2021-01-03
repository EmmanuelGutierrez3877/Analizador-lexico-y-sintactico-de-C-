using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{
    class variable
    {
        private string nombre = "";
        private string tipo = "";
        private int nivel = 0;
        private bool uso = false;

        public void setNombre(string aux)
        {
            this.nombre = aux;
        }

        public string getNombre()
        {
            return this.nombre;
        }

        public void setUso(bool aux)
        {
            this.uso = aux;
        }

        public bool getUso()
        {
            return this.uso;
        }

        public void setTipo(string aux)
        {
            this.tipo = aux;
        }

        public string getTipo()
        {
            return this.tipo;
        }

        public void setNivel(int aux)
        {
            this.nivel = aux;
        }

        public int getNivel() {
            return this.nivel;
        }
    }

    class rama
    {
        public rama raiz = null;
        public int nivel = 0;
        public List<rama> hojas = new List<rama>();
        public List<variable> variables = new List<variable>();
    }
}
