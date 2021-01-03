using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Compilador{

    public partial class FormCompilador : Form {

        List<String> contenido = new List<string>();        // Lista de string, contiene todas las lineas
        List<string> token = new List<string>();            // Lista de string, contiene todos los tokens
        List<string> lexico = new List<string>();           // Lista de string, contiene el valor lexico de los tokens

        int nivel;
        rama inicio = new rama();
        rama actual;

        public FormCompilador()
        {
            InitializeComponent();
        }

        private void toolStripButtonAnalizar_Click(object sender, EventArgs e) {

            listBoxErrores.Items.Clear();                               // Limpiar cuadro de errores
            listViewResultados.Items.Clear();                           // Limpiar resultados
            contenido.Clear();                                          // Limpiar lista contenido

            String codigo = textBoxCodigo.Text;                         // Poner todo el código en la variable
            String palabra = "";                                        // Variable auxiliar                    
            codigo += '\n';                                             // Concatenar un salto de linea                                            

            foreach (Char c in codigo) {                                 // Por cada caracter en el código
                if (c == '\n') {                                         // Si se encuantra un salto de linea                                                      
                    String p = "";                                      // Variable local 

                    if (palabra.Contains("//")) {
                        p = palabra.Substring(0, palabra.Length - 2);   // Enviar la palabra sin las diagonales de comentarios
                    }
                    else {
                        p += palabra;                                   // p = palabra, palabra contiene la linea actual
                    }
                    contenido.Add(p);                                   // Añadir p a la lista    
                    palabra = "";                                       // Limpiar la palabra            
                }
                else if (c == '\r' || c == '\t')                        // Condición para ignorar otros caracteres especiales
                { }
                else
                {
                    if (palabra.Contains("//")) { }                       // Eliminar comentario, pero no //
                    else if (c == ' ' && palabra.Length == 0) { }         // Eliminar espacios iniciales    
                    else {
                        palabra += c;                                   // Sumar el caracter a la linea}
                    }
                }
            }
            Cargar();                                                   // Función cargar 
        }

        private void Cargar() {

            int i = 1;                                                  // Contador de linea
            analizaLlaves();
            nivel = 0;
            inicio.variables.Clear();
            inicio.hojas.Clear();
            inicio.nivel = 0;
            actual = inicio;

            foreach (String s in contenido) {                             // Por cada linea en la lista        
                listViewResultados.Items.Add(i.ToString());             // Imprimir el número de linea
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(s);     // Imprimir linea
                                                                                                  //aqui va analizar linea
                dividirLinea(s, i);
                i++;                                                    // Contador++            
            }
            // Función analizar corchetes

            Variables(inicio);

        }

        private bool analizaLlaves() {
            int linea = 1;                                              // Contador de linea int corchete = 0;                                           // Contador de llave                    
            bool error = true;
            Stack<int> llaves = new Stack<int>();

            foreach (String s in contenido) {                            // Por cada linea en el código   

                if (s.Contains('{')) {                                   // Si la linea contiene llaves {
                    foreach (Char c in s) {                              // Por cada caracter en la linea
                        if (c == '{') {                                    // Si el caracter es {        
                            llaves.Push(linea);
                        }
                    }
                }

                if (s.Contains('}')) {                                   // Si la linea contiene }                                                           
                    foreach (Char c in s) {                              // POr cada caracter en la linea    
                        if (llaves.Count > 0 )
                        {                              // Si el contador es > 0
                            if (c == '}') {                              // Si el caracter es }
                                llaves.Pop();                         // Restar al contador            
                            }
                            
                        }
                        else if (c== '}' && llaves.Count==0)
                        {
                            listBoxErrores.Items.Add(linea + ": falta abrir llave"); // Si el contador es < 0, mostrar mensaje 
                            error = false;
                        }
                    }
                }
                linea++;                                                // Siguiente linea
            }

            int i = 0;
            while (llaves.Count > 0)
            {
                i = llaves.Pop();
                listBoxErrores.Items.Add(i + ": falta cerrar llave");  // Si el contador es < 0, mostrar mensaje    
                error = false;
            }

            return error;
        }

        private void dividirLinea(String s, int nl)
        {  
            token.Clear();
            lexico.Clear();
            string linea = s;
            string palabra = "";
            bool comilla = false;

            // Separar token
            foreach (char c in s)
            {
                if ((c == '"' || c == '\'') && comilla == false)
                {
                    if (palabra != "")
                    {
                        token.Add(palabra);
                        palabra = "";
                    }
                    comilla = true;
                    palabra += '"';
                }
                else if ((c == '"' || c == '\'') && comilla == true)
                {
                    comilla = false;
                    palabra += c;
                    token.Add(palabra);
                    palabra = "";
                }
                else if (c == ' ' && comilla == false)
                {
                    if (palabra != "")
                    {
                        token.Add(palabra);
                        palabra = "";
                    }
                }
                else if (c == ';' || c == '<' || c == '>' || c == '=' || c == '!')
                {                 // Buscar simbolos lógicos y ;
                    if (palabra != "")
                    {
                        token.Add(palabra);
                        palabra = "";
                    }
                    token.Add(c.ToString());
                }
                else if (c == '+' || c == '-' || c == '*' || c == '/')
                {                                 // Buscar simbolos aritméticos
                    if (palabra != "")
                    {
                        token.Add(palabra);
                        palabra = "";
                    }
                    token.Add(c.ToString());
                }
                else if (c == '(' || c == ')' || c == '{' || c == '}' || c == '[' || c == ']')
                {     // Buscar simbolos de agrupación
                    if (palabra != "")
                    {
                        token.Add(palabra);
                        palabra = "";
                    }
                    token.Add(c.ToString());
                }
                else
                {                                                                               // Buscar palabras reservadas e identificadores
                    palabra += c;
                }
            }
            if (palabra != "")
            {
                token.Add(palabra);
            }

            // Validaciones de doble operador
            int cont = 0;
            while (cont < token.Count())
            {
                if (token[cont] == "+")
                {             // Validación del ++
                    if (token[cont + 1] == "+")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = "++";
                    }
                }
                if (token[cont] == "-")
                {             // Validación del --
                    if (token[cont + 1] == "-")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = "--";
                    }
                }
                if (token[cont] == "<")
                {             // Validacion del <=
                    if (token[cont + 1] == "=")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = "<=";
                    }
                }
                if (token[cont] == ">")
                {             // Validacion del >=
                    if (token[cont + 1] == "=")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = ">=";
                    }
                }
                if (token[cont] == "=")
                {             // Validación del ==
                    if (token[cont + 1] == "=")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = "==";
                    }
                }
                if (token[cont] == "!")
                {             // Validacion del !=
                    if (token[cont + 1] == "=")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = "!=";
                    }
                }
                if (token[cont] == "<")
                {             // Validacion del <<
                    if (token[cont + 1] == "<")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = "<<";
                    }
                }
                if (token[cont] == ">")
                {             // Validacion del >>
                    if (token[cont + 1] == ">")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = ">>";
                    }
                }
                if(token[cont] == "else")
                {
                    if(token.Count>cont+1)
                    if(token[cont+1] == "if")
                    {
                        token.RemoveAt(cont + 1);
                        token[cont] = "else if";
                    }
                }
                if (cont < token.Count())
                {
                    cont++;
                }
            }

            // Imprimir token en consola
            foreach (string a in token)
            {
                if(a == "else if")
                {
                    lexico.Add("else if");
                }
                else if (!a.Contains('"'))
                {
                    lexico.Add(Lexico(a));
                }
                else
                {
                    lexico.Add("Cadena");
                }
            }

            if (lexico.Count > 0)
            {
                Automatas(lexico, token, nl);
            }


            for (int i = 0; i < token.Count; i++)
            {
                listViewResultados.Items.Add("");
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(token[i].ToString());
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(lexico[i].ToString());
            }
        }

        private void Variables(rama z)
        {
            foreach (variable a in z.variables)
            {
                for(int i = 0; i < a.getNivel(); i++)
                {
                    Console.Write("\t");
                }
                Console.WriteLine(a.getNivel() + " nivel. " + a.getTipo() + ": " + a.getNombre());

                if(a.getUso() == false)
                {
                    listBoxErrores.Items.Add("Warning: La variable (" + a.getNombre() + ") nunca fue utilizada");
                }
            }

            foreach(rama a in z.hojas)
            {
                Variables(a);
            }

        }

        private string Lexico(string token)
        {
            string palabra = token;                                 // Pasar a un string la palabra a buscar

            // Sección para identificar digitos del tamaño de un int 
            int temp = 0;                                           // Int temporal para comparar si se buscan números
            if (int.TryParse(palabra, out temp))
            {                   // Comparacion para identificar números
                return "numero";                                    // Mensaje de numero
            }

            // Identificar palabras reservadas    
            String line;                    // String auxiliar para leer sobre un archivo
            try
            {                            // Abrir el archivo Token.txt, este contiene la lista de palabras reservadas
                StreamReader sr = new StreamReader("Token.txt");

                // Leer el primer elemento del archivo Token.txt
                line = sr.ReadLine();
                if (line == palabra)
                {           // Condición, si lo buscado pertenece a los Token
                    return palabra;             // Es una palabra reservada
                }

                // En caso de no ser la primer palabra, buscar hasta el fin del archivo
                while (line != null)
                {
                    // Leer la siguiente linea
                    line = sr.ReadLine();

                    // Comparar contra la palabra buscada, si es un Token se avisa al Label result
                    if (line == palabra)
                    {
                        return palabra;
                    }
                }
                // Cerrar el archivo
                sr.Close();
            }
            // En caso de que no se pueda abrir el archivo, mostrar en consola el error ocurrido
            catch (Exception exept)
            {
                Console.WriteLine("Exception: " + exept.Message);
            }
            // Mensaje para finalizar la operación
            finally { }

            // Sección para posibles identificadores de variables
            if ((palabra[0] >= 'A' && palabra[0] <= 'Z') || (palabra[0] >= 'a' && palabra[0] <= 'z') || (palabra[0] == '_' && palabra.Length > 1))
            {
                return "id";
            }

            // Sección para operadores aritmeticos
            if (palabra == "+" || palabra == "-" || palabra == "*" || palabra == "/" || palabra == "=")
            {
                return "aritmetico";
            }

            // Sección para operadores lógicos
            if (palabra == "<" || palabra == ">" || palabra == "<=" || palabra == ">=" || palabra == "==" || palabra == "!=")
            {
                return "logico";
            }

            // Seccion para conjunción y disyunción
            if (palabra == "&&" || palabra == "||")
            {
                return "condicional";
            }

            // Encadenadores de cout y cin
            if (palabra == "<<")
            {
                return "escritor";
            }

            if (palabra == ">>")
            {
                return "lector";
            }

            // Sección para símbolos de agrupación y punto y coma
            if (palabra == "(" || palabra == ")" || palabra == "{" || palabra == "}" || palabra == ";")
            {
                return palabra;
            }

            return "error";
        }

        private void Automatas(List<string> lex, List<string> tok, int nl)
        {
            bool res;
            if (lex[0] == "cout")
            {
                res = Cout(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Sintaxis cout invalida");
                }
            }
            else if (lex[0] == "int")
            {
                res = Int(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion int invalida");
                }
            }
            else if (lex[0] == "char")
            {
                res = Char(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion char invalida");
                }
            }
            else if (lex[0] == "if")
            {
                res = If(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    nivel++;
                    rama newrama = new rama();
                    newrama.nivel = nivel;
                    newrama.raiz = actual;
                    actual.hojas.Add(newrama);
                    actual = newrama;

                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion if invalida");
                }
            }
            else if (lex[0] == "while")
            {
                res = While(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    nivel++;
                    rama newrama = new rama();
                    newrama.nivel = nivel;
                    newrama.raiz = actual;
                    actual.hojas.Add(newrama);
                    actual = newrama;

                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion while invalida");
                }
            }
            else if (lex[0] == "do")
            {
                res = Do(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    nivel++;
                    rama newrama = new rama();
                    newrama.nivel = nivel;
                    newrama.raiz = actual;
                    actual.hojas.Add(newrama);
                    actual = newrama;

                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion do invalida");
                }
            }
            else if (lex[0] == "}"&&lex.Contains("while"))
            {
                res = DoWhile(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    nivel--;

                    if (actual.raiz != null)
                    {
                        actual = actual.raiz;
                    }
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion while invalida");
                }
            }
            else if (lex[0] == "id" && tok.Contains("="))
            {
                res = Asignacion(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion de asignacion invalida");
                }
            }
            else if (lex[0] == "cin")
            {
                res = Cin(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion cin invalida");
                }
            }
            else if (lex[0] == "}")
            {
                res = Llave(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    nivel--;

                    if(actual.raiz != null)
                    {
                        actual = actual.raiz;
                    }

                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Cierre de llave invalido");
                }
            }
            else if (lex[0] == "for")
            {
                res = For(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    nivel++;
                    rama newrama = new rama();
                    newrama.nivel = nivel;
                    newrama.raiz = actual;
                    actual.hojas.Add(newrama);
                    actual = newrama;

                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion For invalida");
                }
            }
            else if (lex[0] == "else" || lex[0] == "else if")
            {
                if(lex[0] == "else if"){
                    lex[0] = "if";
                    res = If(lex, tok, nl);
                }
                else {
                    res = Else(lex, tok, nl);
                }
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    nivel++;
                    rama newrama = new rama();
                    newrama.nivel = nivel;
                    newrama.raiz = actual;
                    actual.hojas.Add(newrama);
                    actual = newrama;

                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion Else invalida");
                }
            }
            else if(lex[0] == "id" && (tok.Contains("++") || tok.Contains("--")))
            {
                res = false;
                if(lex[0] == "id")
                {
                    if(tok[1] == "++" || tok[1] == "--")
                    {
                        if(lex[2] == ";")
                        {
                            res = true;
                        }
                    }
                }
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion de asignacion invalida");
                }
            }
            //////////////////////
            else if (lex[0] == "#include")
            {
                res = Include(lex, tok, nl);
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems.Add(res.ToString());
                if (res)
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.GreenYellow;
                }
                else
                {
                    listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                    listBoxErrores.Items.Add(nl + ": Declaracion Include invalida");
                }
            }
            //////////////////////////////////////
            else
            {
                listViewResultados.Items[listViewResultados.Items.Count - 1].SubItems[0].BackColor = Color.Red;
                listBoxErrores.Items.Add(nl + ": Instruccion no reconocida");
            }


            //return true;
        }

        private bool Asignacion(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i]=="id")
                        {
                            estado = 1;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 1:
                        if (tok[i] == "=")
                        {
                            estado = 2;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 2:
                        estado = 3;
                        List<string> subLex = new List<string>();
                        List<string> subTok = new List<string>();
                        for (int j = i; j < lex.Count; j++)
                        {
                            if (j < lex.Count)
                            {
                                if (lex[j] != ";")
                                {
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else
                                {
                                    i = j - 1;
                                    break;
                                }
                            }
                            else if (j == lex.Count)
                            {
                                if (lex[j] == ";")
                                {
                                    i = j - 1;
                                    break;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }

                        if (subLex.Count > 1)
                        {
                            bool tx = Operacion(subLex, subTok, nl);
                            if (!tx)
                            {
                                return false;
                            }
                        }
                        else if (subLex.Count == 1)
                        {
                            if (!(subLex[0] == "numero" || subLex[0] == "id"))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 3:
                        if (lex[i]==";")
                        {
                            estado = 4;
                        }
                        break;
                    case 4:
                        return false;
                }
            }
            return true;
        }

        private bool Llave(List<string> lex, List<string> tok, int nl)
        {
            foreach(String s in lex)
            {
                if(s != "}")
                {
                    return false;
                }
            }
            return true;
        }

        private bool Cin(List<string> lex, List<string> tok, int nl)
        {
            if (lex.Count != 4)
            {
                return false;
            }
            else if (lex[0]=="cin"&&tok[1]==">>"&& lex[2] == "id" && tok[3] == ";")
            {
                return true;
            }
            return false ;
        }

        private bool Int (List<string> lex, List<string> tok, int nl)
        {
            variable a = new variable();
            int estado = 0;
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i] == "int")
                        {
                            a.setTipo("int");
                            estado = 1;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 1:
                        if (lex[i] == "id")
                        {
                            rama recorrer = actual;

                            while (recorrer != null)
                            {
                                foreach (variable z in recorrer.variables)
                                {
                                    if (z.getNombre() == tok[i])
                                    {
                                        listBoxErrores.Items.Add(nl + ": Nombre de variable duplicado");
                                        return false;
                                    }
                                }

                                recorrer = recorrer.raiz;
                            }

                            a.setNombre(tok[i]);
                            estado = 2;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 2:
                        if (tok[i]=="=")
                        {
                            estado = 3;
                        }
                        else if(tok[i] == ";")
                        {
                            estado = 5;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 3:
                        estado = 4;
                        List<string> subLex = new List<string>();
                        List<string> subTok = new List<string>();
                        for (int j = i; j < lex.Count; j++)
                        {
                            if (j < lex.Count)
                            {
                                if (lex[j] != ";")
                                {
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else
                                {
                                    i = j - 1;
                                    break;
                                }
                            }
                            else if (j == lex.Count)
                            {
                                if (lex[j] == ";")
                                {
                                    i = j - 1;
                                    break;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }

                        if(subLex.Count>1){
                            bool tx = Operacion(subLex, subTok, nl);
                            if (!tx)
                            {
                                return false;
                            }
                        }
                        else if (subLex.Count==1)
                        {
                            if (!(subLex[0]=="numero"||subLex[0]=="id"))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 4:
                        
                        if (lex[i]==";")
                        {
                            estado = 5;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 5:
                        return false;
                }
            }
            if (estado == 5)
            {
                a.setNivel(nivel);
                actual.variables.Add(a);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool Char (List<string> lex, List<string> tok, int nl)
        {
            variable a = new variable();
            int estado = 0;
            for (int i = 0; i < lex.Count; i++){
                switch (estado){
                    case 0:
                        if (lex[i] == "char") {
                            a.setTipo("char");
                            estado = 1;
                        }
                        else{
                            return false;
                        }
                        break;
                    case 1:
                        if (lex[i] == "id"){
                            rama recorrer = actual;

                            while (recorrer != null)
                            {
                                foreach (variable z in recorrer.variables)
                                {
                                    if (z.getNombre() == tok[i])
                                    {
                                        listBoxErrores.Items.Add(nl + ": Nombre de variable duplicado");
                                        return false;
                                    }
                                }

                                recorrer = recorrer.raiz;
                            }

                            a.setNombre(tok[i]);
                            estado = 2;
                        }
                        else {
                            return false;
                        }
                        break;
                    case 2:
                        if (tok[i] == "=") {
                            estado = 3;
                        }
                        else if (tok[i] == ";") {
                            estado = 5;
                        }
                        else {
                            return false;
                        }
                        break;
                    case 3:
                        estado = 4;
                        if(lex[i] != "Cadena")
                        {
                            if(lex[i] != "id")
                            {
                                return false;
                            }
                        }
                        if(lex[i] == "Cadena" && tok[i].Length != 3)
                        {
                            return false;
                        }

                        break;
                    case 4:

                        if (lex[i] == ";")
                        {
                            estado = 5;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 5:
                        return false;
                }
            }
            if (estado == 5)
            {
                a.setNivel(nivel);
                actual.variables.Add(a);
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool Do(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i]=="do")
                        {
                            estado = 1;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 1:
                        if (lex[i] == "{")
                        {
                            estado = 2;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 2:
                        return false;
                }
            }
            if(estado == 2)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        private bool Cout(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i]=="cout")
                        {
                            estado = 1;
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    case 1:
                        if (tok[i] == "<<")
                        {
                            estado = 2;
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    case 2:
                        estado = 3;
                        List<string> subLex = new List<string>();
                        List<string> subTok = new List<string>();
                        for (int j = i ; j < lex.Count; j++)
                        {
                            if (j < lex.Count)
                            {
                                if (lex[j]!=";")
                                {
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else
                                {
                                    i = j-1;
                                    break;
                                }
                            }
                            else if (j == lex.Count)
                            {
                                if (lex[j] == ";")
                                {
                                    i = j-1;
                                    break;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        bool tx = Texto(subLex, subTok, nl);
                        if (!tx)
                        {
                            return false;
                        }


                        break;

                    case 3:
                        if (lex[i]==";" && i == lex.Count-1)
                        {
                            estado = 4;
                        }
                        else
                        {
                            return false;
                        }

                        break;

                }
            }
            
            if(estado == 4)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        private bool If(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            Stack<string> parentesis = new Stack<string>();
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i] == "if")
                        {
                            estado = 1;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 1:
                        if (lex[i] == "(")
                        {
                            estado = 2;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 2:

                        estado = 3;
                        List<string> subLex = new List<string>();
                        List<string> subTok = new List<string>();
                        Stack<string> auxParenesis = new Stack<string>();

                        for (int j = i; j < lex.Count; j++)
                        {
                            if (j < lex.Count)
                            {
                                if (lex[j] != ")")
                                {
                                    if (lex[j] == "(")
                                    {
                                        auxParenesis.Push("(");
                                    }
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else if (auxParenesis.Count > 0)
                                {
                                    auxParenesis.Pop();
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else
                                {
                                    i = j;
                                    break;
                                }
                            }
                            else if (j == lex.Count)
                            {
                                if (lex[j] == ")")
                                {
                                    i = j;
                                    break;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        i--;
                        bool tx = Validacion(subLex, subTok, 0);
                        if (!tx)
                        {
                            return false;
                        }

                        break;
                    case 3:
                        if (lex[i] == ")")
                        {
                            estado = 4;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 4:
                        if (lex[i] == "{")
                        {
                            estado = 5;
                            break;
                        }
                        else
                        {
                            break;
                        }
                }
            }
            if (estado == 5)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        private bool While(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            Stack<string> parentesis = new Stack<string>();
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i] == "while")
                        {
                            estado = 1;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 1:
                        if (lex[i] == "(")
                        {
                            estado = 2;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 2:

                        estado = 3;
                        List<string> subLex = new List<string>();
                        List<string> subTok = new List<string>();
                        Stack<string> auxParenesis = new Stack<string>();

                        for (int j = i; j < lex.Count; j++)
                        {
                            if (j < lex.Count)
                            {
                                if (lex[j] != ")")
                                {
                                    if (lex[j] == "(")
                                    {
                                        auxParenesis.Push("(");
                                    }
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else if (auxParenesis.Count > 0)
                                {
                                    auxParenesis.Pop();
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else
                                {
                                    i = j;
                                    break;
                                }
                            }
                            else if (j == lex.Count)
                            {
                                if (lex[j] == ")")
                                {
                                    i = j;
                                    break;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        i--;
                        bool tx = Validacion(subLex, subTok, 0);
                        if (!tx)
                        {
                            return false;
                        }

                        break;
                    case 3:
                        if (lex[i] == ")")
                        {
                            estado = 4;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 4:
                        if (lex[i] == "{")
                        {
                            estado = 5;
                            break;
                        }
                        else
                        {
                            break;
                        }
                }
            }
            if (estado == 5)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private bool DoWhile(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            Stack<string> parentesis = new Stack<string>();
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i] == "}")
                        {
                            estado = 1;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 1:
                        if (lex[i] == "while")
                        {
                            estado = 2;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 2:
                        if (lex[i] == "(")
                        {
                            estado = 3;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 3:

                        estado = 4;
                        List<string> subLex = new List<string>();
                        List<string> subTok = new List<string>();
                        Stack<string> auxParenesis = new Stack<string>();

                        for (int j = i; j < lex.Count; j++)
                        {
                            if (j < lex.Count)
                            {
                                if (lex[j] != ")")
                                {
                                    if (lex[j] == "(")
                                    {
                                        auxParenesis.Push("(");
                                    }
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else if (auxParenesis.Count > 0)
                                {
                                    auxParenesis.Pop();
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else
                                {
                                    i = j;
                                    break;
                                }
                            }
                            else if (j == lex.Count)
                            {
                                if (lex[j] == ")")
                                {
                                    i = j;
                                    break;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        i--;
                        bool tx = Validacion(subLex, subTok, 0);
                        if (!tx)
                        {
                            return false;
                        }

                        break;
                    case 4:
                        if (lex[i] == ")")
                        {
                            estado = 5;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 5:
                        if (lex[i] == ";")
                        {
                            estado = 6;
                            break;
                        }
                        else
                        {
                            break;
                        }
                }
            }
            if (estado == 6)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private bool For(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            Stack<string> parentesis = new Stack<string>();
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i] == "for")
                        {
                            estado = 1;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 1:
                        if (lex[i] == "(")
                        {
                            estado = 2;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 2:
                        estado = 3;
                        List<string> subLex = new List<string>();
                        List<string> subTok = new List<string>();
                        Stack<string> auxParenesis = new Stack<string>();

                        for (int j = i; j < lex.Count; j++)
                        {
                            if (j < lex.Count)
                            {
                                if (lex[j] != ")")
                                {
                                    if (lex[j] == "(")
                                    {
                                        auxParenesis.Push("(");
                                    }
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else if (auxParenesis.Count > 0)
                                {
                                    auxParenesis.Pop();
                                    subLex.Add(lex[j]);
                                    subTok.Add(tok[j]);
                                }
                                else
                                {
                                    i = j;
                                    break;
                                }
                            }
                            else if (j == lex.Count)
                            {
                                if (lex[j] == ")")
                                {
                                    i = j;
                                    break;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        i--;

                        List<string> cond = new List<string>();
                        int n = 0;
                        if(subLex.Count>0)
                        while (subLex[n] != ";")
                        {
                            cond.Add(subLex[n]);
                            n++;
                            if (subLex.Count == n)
                            {
                                return false;
                            }
                        }
                        cond.Add(";");

                        bool tx = Int(cond, subTok, 0);
                        if (!tx)
                        {
                            return false;
                        }

                        while (subLex[0] != ";")
                        {
                            subLex.RemoveAt(0);
                            subTok.RemoveAt(0);
                        }
                        subLex.RemoveAt(0);
                        subTok.RemoveAt(0);

                        cond.Clear();
                        n = 0;
                        //
                        
                        while (subLex[n] != ";")
                        {
                            cond.Add(subLex[n]);
                            n++;
                            if (subLex.Count == n)
                            {
                                return false;
                            }
                        }

                        tx = Validacion(cond, subTok, 0);
                        if (!tx)
                        {
                            return false;
                        }

                        while (subLex[0] != ";")
                        {
                            subLex.RemoveAt(0);
                            subTok.RemoveAt(0);
                        }
                        subLex.RemoveAt(0);
                        subTok.RemoveAt(0);

                        if (subLex.Count > 0)
                        {
                            if (subLex[0] == "id" && subTok.Count > 1)
                            {
                                if (subTok[1] != "++" && subTok[1] != "--")
                                {
                                    if (!Operacion(subLex, subTok, nl))
                                    {
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    case 3:
                        if (lex[i] == ")")
                        {
                            estado = 4;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    case 4:
                        if (lex[i] == "{")
                        {
                            estado = 5;
                            break;
                        }
                        else
                        {
                            break;
                        }
                }
            }
            if (estado == 5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool Else(List<string> lex, List<string> tok, int nl)
        {
            if (lex[0] == "else") {
                if (lex.Count > 1)
                if(lex[1] == "{"){
                    return true;
                }
            }
            return false;
        }

        ////////
        private bool Include(List<string> lex, List<string> tok, int nl)
        {
            if (lex[0] == "#include")
            {
                if (lex.Count == 2)
                    if (lex[1] == "Cadena")
                    {
                        return true;
                    }
            }
            return false;
        }
        //////////////////////

        private bool Validacion(List<string> lex, List<string> tok, int nl)
        {
            int cont = nl;
            bool check;
            string tipo = "";
            try
            {
                if (lex[nl] == "id" || lex[nl] == "numero" || lex[nl] == "Cadena")
                {
                    // IF para comparar variables (existencia y uso)
                    if (lex[nl] == "id")
                    {
                        check = false;
                        rama recorrer = actual;

                        while (recorrer != null)
                        {
                            foreach (variable z in recorrer.variables)
                            {
                                if(tok[nl] == z.getNombre())
                                {
                                    z.setUso(true);
                                    check = true;
                                    tipo = z.getTipo();
                                    break;
                                }
                            }
                            recorrer = recorrer.raiz;
                        }
                        if(check == false)
                        {
                            listBoxErrores.Items.Add("Variable " + tok[nl] + " no delarada");
                            return false;
                        }
                    }

                    nl++;
                    if (lex[nl] == "logico")
                    {
                        nl++;
                        if ((lex[nl] == "id" || lex[nl] == "numero" || lex[nl] == "Cadena") && lex.Count-1==nl)
                        {
                            // IF para comparar variables (existencia y uso)
                            if (lex[nl] == "id")
                            {
                                check = false;
                                rama recorrer = actual;

                                while (recorrer != null)
                                {
                                    foreach (variable z in recorrer.variables)
                                    {
                                        if (tok[nl] == z.getNombre() && tipo == z.getTipo())
                                        {
                                            z.setUso(true);
                                            check = true;
                                            break;
                                        }
                                    }
                                    recorrer = recorrer.raiz;
                                }
                                if (check == false)
                                {
                                    listBoxErrores.Items.Add("Variable " + tok[nl] + " no delarada");
                                    return false;
                                }
                            }
                            return true;
                        }
                        if (lex[nl + 1] == "condicional")
                        {
                            nl++;
                            return Validacion(lex,tok, nl + 1);
                        }
                    }
                }
            }
            catch (Exception exept)
            {
                listBoxErrores.Items.Add("Error de sintaxis: Operación logica erronea o inexistente");
                return false;
            }

            return false;
        }

        private bool Operacion(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            string tipo = "";
            bool check;
            Stack<string> parentesis = new Stack<string>();
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i]=="(")
                        {
                            parentesis.Push("(");
                            estado = 0;
                        }
                        else if (lex[i] == "id"|| lex[i] == "numero")
                        {
                            // IF para comparar variables (existencia y uso)
                            if (lex[i] == "id")
                            {
                                check = false;
                                rama recorrer = actual;

                                while (recorrer != null)
                                {
                                    foreach (variable z in recorrer.variables)
                                    {
                                        if (tok[i] == z.getNombre())
                                        {
                                            z.setUso(true);
                                            tipo = z.getTipo();
                                            check = true;
                                            break;
                                        }
                                    }
                                    recorrer = recorrer.raiz;
                                }
                                if (check == false)
                                {
                                    listBoxErrores.Items.Add("Variable " + tok[i] + " no delarada");
                                    return false;
                                }
                            }

                            estado = 1;
                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                    case 1:
                        if (lex[i] == "aritmetico")
                        {
                            estado = 2;
                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                    case 2:
                        if (lex[i] == "(")
                        {
                            parentesis.Push("(");
                            estado = 0;
                        }
                        else if (lex[i] == "id" || lex[i] == "numero")
                        {
                            // IF para comparar variables (existencia y uso)
                            if (lex[i] == "id")
                            {
                                check = false;
                                rama recorrer = actual;

                                while (recorrer != null)
                                {
                                    foreach (variable z in recorrer.variables)
                                    {
                                        if (tok[i] == z.getNombre() && tipo == z.getTipo())
                                        {
                                            z.setUso(true);
                                            check = true;
                                            break;
                                        }
                                    }
                                    recorrer = recorrer.raiz;
                                }
                                if (check == false)
                                {
                                    listBoxErrores.Items.Add("Variable " + tok[i] + " no delarada");
                                    return false;
                                }
                            }

                            estado = 3;
                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                    case 3:
                        if (lex[i] == "aritmetico")
                        {                            
                            estado = 2;
                        }
                        else if (lex[i] == ")")
                        {
                            if (parentesis.Count>0)
                            {
                                parentesis.Pop();
                            }
                            else
                            {
                                //listBoxErrores.Items.Add(nl+": falta abrir el parentesis");
                                return false;
                            }
                            estado = 4;
                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                    case 4:
                        if (lex[i] == "aritmetico")
                        {
                            estado = 2;
                        }
                        else if (lex[i] == ")")
                        {
                            if (parentesis.Count > 0)
                            {
                                parentesis.Pop();
                            }
                            else
                            {
                                //listBoxErrores.Items.Add(nl + ": falta abrir el parentesis");
                                return false;
                            }
                            estado = 4;
                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                }
            }
            if (estado==0|estado==1|estado==2)
            {
                return false;
            }
            if(parentesis.Count == 0)
            {
                return true;
            }
            else
            {
                //listBoxErrores.Items.Add(nl + ": falta cerrar el parentesis");
                return false;
            }
            
        }

        private bool Texto(List<string> lex, List<string> tok, int nl)
        {
            int estado = 0;
            string tipo = "";
            bool check;
            Stack<string> parentesis = new Stack<string>();
            for (int i = 0; i < lex.Count; i++)
            {
                switch (estado)
                {
                    case 0:
                        if (lex[i]=="Cadena"|| lex[i] == "id"||lex[i]=="numero")
                        {
                            // IF para comparar variables (existencia y uso)
                            if (lex[i] == "id")
                            {
                                check = false;
                                rama recorrer = actual;

                                while (recorrer != null)
                                {
                                    foreach (variable z in recorrer.variables)
                                    {
                                        if (tok[i] == z.getNombre())
                                        {
                                            tipo = z.getTipo();
                                            z.setUso(true);
                                            check = true;
                                            break;
                                        }
                                    }
                                    recorrer = recorrer.raiz;
                                }
                                if (check == false)
                                {
                                    listBoxErrores.Items.Add("Variable " + tok[i] + " no delarada");
                                    return false;
                                }
                            }

                            estado = 1;
                        }
                        else if (lex[i] == "(")
                        {
                            estado = 1;
                            List<string> subLex = new List<string>();
                            List<string> subTok = new List<string>();
                            Stack<string> auxParenesis = new Stack<string>();

                            for (int j=i+1;j<lex.Count;j++)
                            {
                                if (j < lex.Count)
                                {
                                    if (lex[j]!=")")
                                    {
                                        if (lex[j]=="(")
                                        {
                                            auxParenesis.Push("(");
                                        }
                                        subLex.Add(lex[j]);
                                        subTok.Add(tok[j]);
                                    }
                                    else if (auxParenesis.Count>0)
                                    {
                                        auxParenesis.Pop();
                                        subLex.Add(lex[j]);
                                        subTok.Add(tok[j]);
                                    }
                                    else
                                    {
                                        i = j;
                                        break;
                                    }
                                }
                                else if (j== lex.Count)
                                {
                                    if (lex[j]==")")
                                    {
                                        i = j;
                                        break;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }

                            bool op = Operacion(subLex,subTok, nl);
                            if (!op)
                            {
                                return false;
                            }

                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                    case 1:
                        if (lex[i] == "aritmetico")
                        {
                            if (tok[i] == "+")
                            {
                                estado = 2;
                            }
                            else
                            {
                                return false;
                            }
                           
                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                    case 2:
                        if (lex[i] == "Cadena" || lex[i] == "id" || lex[i] == "numero")
                        {
                            // IF para comparar variables (existencia y uso)
                            if (lex[i] == "id")
                            {
                                check = false;
                                rama recorrer = actual;

                                while (recorrer != null)
                                {
                                    foreach (variable z in recorrer.variables)
                                    {
                                        if (tok[i] == z.getNombre() && tipo == z.getTipo())
                                        {
                                            z.setUso(true);
                                            check = true;
                                            break;
                                        }
                                    }
                                    recorrer = recorrer.raiz;
                                }
                                if (check == false)
                                {
                                    listBoxErrores.Items.Add("Variable " + tok[i] + " no delarada");
                                    return false;
                                }
                            }

                            estado = 1;
                        }
                        else if (lex[i] == "(")
                        {
                            estado = 1;
                            List<string> subLex = new List<string>();
                            List<string> subTok = new List<string>();
                            Stack<string> auxParenesis = new Stack<string>();

                            for (int j = i + 1; j < lex.Count; j++)
                            {
                                if (j < lex.Count)
                                {
                                    if (lex[j] != ")")
                                    {
                                        if (lex[j] == "(")
                                        {
                                            auxParenesis.Push("(");
                                        }
                                        subLex.Add(lex[j]);
                                        subTok.Add(tok[j]);
                                    }
                                    else if (auxParenesis.Count > 0)
                                    {
                                        auxParenesis.Pop();
                                        subLex.Add(lex[j]);
                                        subTok.Add(tok[j]);
                                    }
                                    else
                                    {
                                        i = j;
                                        break;
                                    }
                                }
                                else if (j == lex.Count)
                                {
                                    if (lex[j] == ")")
                                    {
                                        i = j;
                                        break;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }

                            bool op = Operacion(subLex, subTok, nl);
                            if (!op)
                            {
                                return false;
                            }

                        }
                        else
                        {
                            //listBoxErrores.Items.Add(nl + ": Expresion invalida");
                            return false;
                        }
                        break;
                }
            }
            if (estado == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.InitialDirectory = "c:\\";
            op.Filter = "All files (*.*)|*.*|C++ (*.cpp)|*.cpp";
            op.FilterIndex = 2;
            op.RestoreDirectory = true;
            if (op.ShowDialog() == DialogResult.OK)
            { 
                StreamReader sr = new StreamReader(op.FileName);
                textBoxCodigo.Text = sr.ReadToEnd();
                sr.Close();  
            }
            
        }

        private void guardarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "(*.cpp)|*.cpp";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName))
                {
                    writer.WriteLine(textBoxCodigo.Text);
                    writer.Close();
                }  
            }
        }
    }
}
