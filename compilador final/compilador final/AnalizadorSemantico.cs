using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompiladorFinal
{
    public class AnalizadorSemantico
    {
        private List<Error> errores;
        private Stack<Dictionary<string, string>> ambitos;

        public AnalizadorSemantico()
        {
            errores = new List<Error>();
            ambitos = new Stack<Dictionary<string, string>>();
        }

        public List<Error> Analizar(List<Token> tokens)
        {
            errores.Clear();
            ambitos.Clear();
            ambitos.Push(new Dictionary<string, string>()); // Ámbito global

            for (int i = 0; i < tokens.Count; i++)
            {
                // Verificar asignación antes de declaración
                if (tokens[i].Tipo == "IDENTIFICADOR" && i + 1 < tokens.Count && tokens[i + 1].Tipo == "ASIGNACION")
                {
                    string nombreVariable = tokens[i].Valor;
                    bool encontrada = false;

                    foreach (var ambito in ambitos)
                    {
                        if (ambito.ContainsKey(nombreVariable))
                        {
                            encontrada = true;
                            break;
                        }
                    }

                    if (!encontrada)
                    {
                        errores.Add(new Error("Semántico", $"La variable '{nombreVariable}' no ha sido declarada",
                                            tokens[i].Linea, tokens[i].Columna));
                    }
                }

                // Registrar variables declaradas
                if (EsTipoDeclaracion(tokens[i].Tipo) && i + 1 < tokens.Count && tokens[i + 1].Tipo == "IDENTIFICADOR")
                {
                    string nombreVariable = tokens[i + 1].Valor;
                    string tipoVariable = tokens[i].Valor.ToLower();

                    if (ambitos.Peek().ContainsKey(nombreVariable))
                    {
                        errores.Add(new Error("Semántico", $"La variable '{nombreVariable}' ya ha sido declarada en este ámbito",
                                            tokens[i + 1].Linea, tokens[i + 1].Columna));
                    }
                    else
                    {
                        ambitos.Peek().Add(nombreVariable, tipoVariable);
                    }
                }

                // Manejo de ámbitos
                if (tokens[i].Tipo == "LLAVE_IZQ")
                {
                    ambitos.Push(new Dictionary<string, string>());
                }
                else if (tokens[i].Tipo == "LLAVE_DER" && ambitos.Count > 1)
                {
                    ambitos.Pop();
                }

                // Verificar operaciones matemáticas con tipos correctos
                if ((tokens[i].Tipo == "SUMA" || tokens[i].Tipo == "RESTA" || tokens[i].Tipo == "MULTIPLICACION" || tokens[i].Tipo == "DIVISION"))
                {
                    if (i > 0 && i < tokens.Count - 1)
                    {
                        string tipoIzq = ObtenerTipoVariable(tokens[i - 1]);
                        string tipoDer = ObtenerTipoVariable(tokens[i + 1]);

                        if (tipoIzq == "string" || tipoIzq == "char" || tipoDer == "string" || tipoDer == "char")
                        {
                            errores.Add(new Error("Semántico", "Operaciones matemáticas no permitidas con tipos string o char",
                                                tokens[i].Linea, tokens[i].Columna));
                        }
                    }
                }
            }

            return errores;
        }

        private bool EsTipoDeclaracion(string tipo)
        {
            return tipo == "INT" || tipo == "STRING" || tipo == "CHAR" || tipo == "FLOAT" || tipo == "DOUBLE";
        }

        private string ObtenerTipoVariable(Token token)
        {
            if (token.Tipo == "ENTERO") return "int";
            if (token.Tipo == "DECIMAL") return "float";
            if (token.Tipo == "CADENA") return "string";
            if (token.Tipo == "IDENTIFICADOR")
            {
                foreach (var ambito in ambitos)
                {
                    if (ambito.ContainsKey(token.Valor))
                        return ambito[token.Valor];
                }
            }
            return "desconocido";
        }
    }
}
