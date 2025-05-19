using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompiladorFinal
{
        public class AnalizadorSintactico
        {
            private List<Token> tokens;
            private int posicionActual;
            private List<Error> errores;
            private Stack<Dictionary<string, string>> ambitos;

            public AnalizadorSintactico()
            {
                errores = new List<Error>();
                ambitos = new Stack<Dictionary<string, string>>();
            }

            public List<Error> Analizar(List<Token> tokens)
            {
                this.tokens = tokens;
                posicionActual = 0;
                errores.Clear();
                ambitos.Clear();

                try
                {
                    // Iniciar ámbito global
                    EntrarAmbito();
                    AnalizarClase();
                    SalirAmbito();
                }
                catch (Exception ex)
                {
                    errores.Add(new Error("Sintáctico", $"Error inesperado: {ex.Message}", 0, 0));
                }

                return errores;
            }

            private void EntrarAmbito()
            {
                ambitos.Push(new Dictionary<string, string>());
            }

            private void SalirAmbito()
            {
                ambitos.Pop();
            }

            private Dictionary<string, string> AmbitoActual => ambitos.Peek();

            private void AnalizarClase()
            {
                if (!Consumir("CLASS", "Se esperaba la palabra reservada 'class'"))
                    return;

                if (!Consumir("IDENTIFICADOR", "Se esperaba el nombre de la clase"))
                    return;

                if (!Consumir("LLAVE_IZQ", "Se esperaba '{' después del nombre de la clase"))
                    return;

                // Nuevo ámbito para el contenido de la clase
                EntrarAmbito();

                while (posicionActual < tokens.Count && !EsTipoActual("LLAVE_DER"))
                {
                    AnalizarDeclaracion();
                }

                SalirAmbito();

                if (!Consumir("LLAVE_DER", "Se esperaba '}' para cerrar la clase"))
                    return;
            }

            private void AnalizarDeclaracion()
            {
                if (EsTipoActual("IF"))
                {
                    AnalizarIf();
                }
                else if (EsTipoActual("FOR"))
                {
                    AnalizarFor();
                }
                else if (EsTipoActual("WHILE"))
                {
                    AnalizarWhile();
                }
                else if (EsTipoActual("INT") || EsTipoActual("STRING") || EsTipoActual("CHAR") ||
                         EsTipoActual("FLOAT") || EsTipoActual("DOUBLE"))
                {
                    AnalizarDeclaracionVariable();
                }
                else if (EsTipoActual("PRINT"))
                {
                    AnalizarPrint();
                }
                else if (EsTipoActual("IDENTIFICADOR"))
                {
                    AnalizarAsignacion();
                }
                else
                {
                    AgregarError("Sintáctico", $"Declaración no válida: {tokens[posicionActual].Valor}",
                                tokens[posicionActual].Linea, tokens[posicionActual].Columna);
                    Avanzar();
                }
            }

        private void AnalizarIf()
        {
            if (!Consumir("IF", "Se esperaba 'if'"))
                return;

            if (!Consumir("PARENTESIS_IZQ", "Se esperaba '(' después de 'if'"))
                return;

            // Guardar posición inicial de la condición
            int inicioCondicion = posicionActual;

            // Analizar la condición completa
            AnalizarCondicion();

            // Verificar variables usadas en la condición
            VerificarVariablesEnRango(inicioCondicion, posicionActual - 1);

            if (!Consumir("PARENTESIS_DER", "Se esperaba ')' después de la condición"))
                return;

            if (!Consumir("LLAVE_IZQ", "Se esperaba '{' después de la condición"))
                return;

            // Nuevo ámbito para el cuerpo del if
            EntrarAmbito();

            while (posicionActual < tokens.Count && !EsTipoActual("LLAVE_DER"))
            {
                AnalizarDeclaracion();
            }

            SalirAmbito();

            if (!Consumir("LLAVE_DER", "Se esperaba '}' para cerrar el bloque if"))
                return;
        }

        private void AnalizarFor()
        {
            if (!Consumir("FOR", "Se esperaba 'for'"))
                return;

            if (!Consumir("PARENTESIS_IZQ", "Se esperaba '(' después de 'for'"))
                return;

            // --- Fase 1: Inicialización ---
            int inicioInicializacion = posicionActual;
            if (Consumir("INT", "Se esperaba 'int' en la declaración del for"))
            {
                string nombreVariable = tokens[posicionActual].Valor;
                if (!Consumir("IDENTIFICADOR", "Se esperaba identificador en el for"))
                    return;

                // Registrar variable del for en el ámbito actual
                AmbitoActual[nombreVariable] = "int";

                // Manejar asignación inicial (i = 0)
                if (EsTipoActual("ASIGNACION"))
                {
                    Avanzar();
                    int inicioAsignacion = posicionActual;
                    AnalizarExpresion();
                    VerificarVariablesEnRango(inicioAsignacion, posicionActual - 1);
                }
            }
            else if (EsTipoActual("IDENTIFICADOR"))
            {
                // Para variables ya declaradas
                VerificarVariableDeclarada(tokens[posicionActual]);
                Avanzar();
                if (EsTipoActual("ASIGNACION"))
                {
                    Avanzar();
                    int inicioAsignacion = posicionActual;
                    AnalizarExpresion();
                    VerificarVariablesEnRango(inicioAsignacion, posicionActual - 1);
                }
            }

            if (!Consumir("PUNTO_COMA", "Se esperaba ';' después de la inicialización"))
                return;

            // --- Fase 2: Condición ---
            int inicioCondicion = posicionActual;
            if (!EsTipoActual("PUNTO_COMA")) // Permitir condición vacía
            {
                AnalizarCondicion();
            }
            VerificarVariablesEnRango(inicioCondicion, posicionActual - 1);

            if (!Consumir("PUNTO_COMA", "Se esperaba ';' después de la condición"))
                return;

            // --- Fase 3: Incremento ---
            int inicioIncremento = posicionActual;
            if (!EsTipoActual("PARENTESIS_DER")) // Permitir incremento vacío
            {
                if (Consumir("IDENTIFICADOR", "Se esperaba identificador en el incremento"))
                {
                    VerificarVariableDeclarada(tokens[posicionActual - 1]);

                    if (EsTipoActual("SUMA"))
                    {
                        Avanzar();
                        if (EsTipoActual("SUMA")) // i++
                        {
                            Avanzar();
                        }
                        else if (EsTipoActual("ASIGNACION")) // i +=
                        {
                            Avanzar();
                            AnalizarExpresion();
                            VerificarVariablesEnRango(inicioIncremento, posicionActual - 1);
                        }
                    }
                }
            }

            if (!Consumir("PARENTESIS_DER", "Se esperaba ')' después del incremento"))
                return;

            if (!Consumir("LLAVE_IZQ", "Se esperaba '{' para el cuerpo del for"))
                return;

            // Nuevo ámbito para el cuerpo del for
            EntrarAmbito();

            while (posicionActual < tokens.Count && !EsTipoActual("LLAVE_DER"))
            {
                AnalizarDeclaracion();
            }

            SalirAmbito();

            if (!Consumir("LLAVE_DER", "Se esperaba '}' para cerrar el bloque for"))
                return;
        }

        private void VerificarVariablesEnRango(int inicio, int fin)
        {
            for (int i = inicio; i <= fin; i++)
            {
                if (tokens[i].Tipo == "IDENTIFICADOR")
                {
                    VerificarVariableDeclarada(tokens[i]);
                }
            }
        }

        private void VerificarVariableDeclarada(Token tokenVariable)
        {
            if (tokenVariable.Tipo != "IDENTIFICADOR") return;

            string nombreVariable = tokenVariable.Valor;
            bool declarada = false;

            // Buscar en todos los ámbitos (del más interno al más externo)
            foreach (var ambito in ambitos)
            {
                if (ambito.ContainsKey(nombreVariable))
                {
                    declarada = true;
                    break;
                }
            }

            if (!declarada)
            {
                AgregarError("Semántico", $"La variable '{nombreVariable}' no ha sido declarada",
                            tokenVariable.Linea, tokenVariable.Columna);
            }
        }

        private void AnalizarWhile()
        {
            if (!Consumir("WHILE", "Se esperaba 'while'"))
                return;

            if (!Consumir("PARENTESIS_IZQ", "Se esperaba '(' después de 'while'"))
                return;

            // Verificar variables en la condición
            int inicioCondicion = posicionActual;
            AnalizarCondicion();
            int finCondicion = posicionActual - 1;

            // Verificar variables usadas en la condición
            VerificarVariablesEnCondicion(inicioCondicion, finCondicion);

            if (!Consumir("PARENTESIS_DER", "Se esperaba ')' después de la condición"))
                return;

            if (!Consumir("LLAVE_IZQ", "Se esperaba '{' para el cuerpo del while"))
                return;

            // Nuevo ámbito para el cuerpo del while
            EntrarAmbito();

            while (posicionActual < tokens.Count && !EsTipoActual("LLAVE_DER"))
            {
                AnalizarDeclaracion();
            }

            SalirAmbito();

            if (!Consumir("LLAVE_DER", "Se esperaba '}' para cerrar el bloque while"))
                return;
        }

        private void VerificarVariablesEnCondicion(int inicio, int fin)
        {
            for (int i = inicio; i <= fin; i++)
            {
                if (tokens[i].Tipo == "IDENTIFICADOR")
                {
                    string nombreVariable = tokens[i].Valor;
                    bool variableDeclarada = false;

                    // Buscar en todos los ámbitos (del más interno al más externo)
                    foreach (var ambito in ambitos)
                    {
                        if (ambito.ContainsKey(nombreVariable))
                        {
                            variableDeclarada = true;
                            break;
                        }
                    }

                    if (!variableDeclarada)
                    {
                        AgregarError("Semántico", $"La variable '{nombreVariable}' no ha sido declarada",
                                    tokens[i].Linea, tokens[i].Columna);
                    }
                }
            }
        }

        private void AnalizarCondicion()
        {
            AnalizarExpresion();

            if (EsTipoActual("MENOR") || EsTipoActual("MAYOR") ||
                EsTipoActual("MENOR_IGUAL") || EsTipoActual("MAYOR_IGUAL") ||
                EsTipoActual("IGUALDAD") || EsTipoActual("DIFERENTE"))
            {
                Avanzar();
                AnalizarExpresion();
            }
            else if (EsTipoActual("AND_LOGICO") || EsTipoActual("OR_LOGICO"))
            {
                Avanzar();
                AnalizarCondicion();
            }
        }

        private void AnalizarDeclaracionVariable()
        {
            string tipo = tokens[posicionActual].Valor;
            Avanzar();

            if (!Consumir("IDENTIFICADOR", "Se esperaba nombre de variable"))
                return;

            string nombreVariable = tokens[posicionActual - 1].Valor;

            if (AmbitoActual.ContainsKey(nombreVariable))
            {
                AgregarError("Semántico", $"La variable '{nombreVariable}' ya fue declarada en este ámbito",
                            tokens[posicionActual - 1].Linea, tokens[posicionActual - 1].Columna);
            }
            else
            {
                AmbitoActual.Add(nombreVariable, tipo);
            }

            if (EsTipoActual("ASIGNACION"))
            {
                Avanzar();

                // Permitir cadena vacía para variables string
                if (tipo.ToLower() == "string" && EsTipoActual("CADENA"))
                {
                    Avanzar();
                }
                else
                {
                    AnalizarExpresion();
                }
            }

            if (!Consumir("PUNTO_COMA", "Se esperaba ';' al final de la declaración"))
                return;
        }

        private void AnalizarAsignacion()
            {
                string nombreVariable = tokens[posicionActual].Valor;
                Avanzar();

                // Buscar la variable en todos los ámbitos (del más interno al más externo)
                bool variableDeclarada = false;
                foreach (var ambito in ambitos)
                {
                    if (ambito.ContainsKey(nombreVariable))
                    {
                        variableDeclarada = true;
                        break;
                    }
                }

                if (!variableDeclarada)
                {
                    AgregarError("Semántico", $"La variable '{nombreVariable}' no ha sido declarada",
                                tokens[posicionActual - 1].Linea, tokens[posicionActual - 1].Columna);
                }

                if (!Consumir("ASIGNACION", "Se esperaba '=' en la asignación"))
                    return;

                AnalizarExpresion();

                if (!Consumir("PUNTO_COMA", "Se esperaba ';' al final de la asignación"))
                    return;
            }

        private void AnalizarPrint()
        {
            if (!Consumir("PRINT", "Se esperaba 'print'"))
                return;

            if (!Consumir("PARENTESIS_IZQ", "Se esperaba '(' después de 'print'"))
                return;

            // Aceptar cadena vacía o con contenido
            if (!EsTipoActual("CADENA"))
            {
                AgregarError("Sintáctico", "Se esperaba cadena de texto entre comillas",
                            tokens[posicionActual].Linea, tokens[posicionActual].Columna);
                return;
            }

            // Consumir la cadena (puede ser vacía "")
            Avanzar();

            if (!Consumir("PARENTESIS_DER", "Se esperaba ')' después del mensaje"))
                return;

            if (!Consumir("PUNTO_COMA", "Se esperaba ';' al final de la sentencia print"))
                return;
        }

        private void AnalizarExpresion()
            {
                if (EsTipoActual("IDENTIFICADOR") || EsTipoActual("ENTERO") || EsTipoActual("DECIMAL"))
                {
                    Avanzar();

                    if (EsTipoActual("SUMA") || EsTipoActual("RESTA") || EsTipoActual("MULTIPLICACION") || EsTipoActual("DIVISION"))
                    {
                        Avanzar();
                        AnalizarExpresion();
                    }
                }
                else
                {
                    AgregarError("Sintáctico", $"Expresión no válida: {tokens[posicionActual].Valor}",
                                tokens[posicionActual].Linea, tokens[posicionActual].Columna);
                    Avanzar();
                }
            }

            private bool Consumir(string tipo, string mensajeError)
            {
                if (posicionActual < tokens.Count && tokens[posicionActual].Tipo == tipo)
                {
                    Avanzar();
                    return true;
                }

                AgregarError("Sintáctico", mensajeError, tokens[posicionActual].Linea, tokens[posicionActual].Columna);
                return false;
            }

            private bool EsTipoActual(string tipo)
            {
                return posicionActual < tokens.Count && tokens[posicionActual].Tipo == tipo;
            }

            private void Avanzar()
            {
                posicionActual++;
            }

            private void AgregarError(string tipo, string descripcion, int linea, int columna)
            {
                errores.Add(new Error(tipo, descripcion, linea, columna));
            }
        }
    
}
