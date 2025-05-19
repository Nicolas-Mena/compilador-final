using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompiladorFinal
{
    public class AnalizadorLexico
    {
        private readonly Dictionary<string, string> PalabrasReservadas = new Dictionary<string, string>
        {
            {"class", "CLASS"},
            {"if", "IF"},
            {"else", "ELSE"},
            {"for", "FOR"},
            {"while", "WHILE"},
            {"int", "INT"},
            {"string", "STRING"},
            {"char", "CHAR"},
            {"float", "FLOAT"},
            {"double", "DOUBLE"},
            {"print", "PRINT"},
            {"and", "AND"},
            {"or", "OR"}
        };

        private readonly Dictionary<string, string> Operadores = new Dictionary<string, string>
        {
            {"+", "SUMA"},
            {"-", "RESTA"},
            {"*", "MULTIPLICACION"},
            {"/", "DIVISION"},
            {"=", "ASIGNACION"},
            {"&&", "AND_LOGICO"},
            {"||", "OR_LOGICO"},
            {"<", "MENOR"},
            {">", "MAYOR"},
            {"<=", "MENOR_IGUAL"},
            {">=", "MAYOR_IGUAL"},
            {"==", "IGUALDAD"},
            {"!=", "DIFERENTE"}
        };

        private readonly Dictionary<string, string> Delimitadores = new Dictionary<string, string>
        {
            {"(", "PARENTESIS_IZQ"},
            {")", "PARENTESIS_DER"},
            {"{", "LLAVE_IZQ"},
            {"}", "LLAVE_DER"},
            {";", "PUNTO_COMA"},
            {"\"", "COMILLA"}
        };

        public List<Token> Analizar(string codigo, List<Error> errores)
        {
            List<Token> tokens = new List<Token>();
            int linea = 1;
            int columna = 1;
            int posicion = 0;

            while (posicion < codigo.Length)
            {
                char caracterActual = codigo[posicion];

                // Ignorar espacios en blanco y saltos de línea
                if (char.IsWhiteSpace(caracterActual))
                {
                    if (caracterActual == '\n')
                    {
                        linea++;
                        columna = 1;
                    }
                    else
                    {
                        columna++;
                    }
                    posicion++;
                    continue;
                }

                // Comentarios de una línea (simplificado)
                if (caracterActual == '/' && posicion + 1 < codigo.Length && codigo[posicion + 1] == '/')
                {
                    while (posicion < codigo.Length && codigo[posicion] != '\n')
                    {
                        posicion++;
                    }
                    linea++;
                    columna = 1;
                    continue;
                }

                // Identificadores y palabras reservadas
                if (char.IsLetter(caracterActual) || caracterActual == '_')
                {
                    int inicio = posicion;
                    while (posicion < codigo.Length && (char.IsLetterOrDigit(codigo[posicion]) || codigo[posicion] == '_'))
                    {
                        posicion++;
                    }
                    string lexema = codigo.Substring(inicio, posicion - inicio);
                    columna += lexema.Length;

                    if (PalabrasReservadas.ContainsKey(lexema))
                    {
                        tokens.Add(new Token(PalabrasReservadas[lexema], lexema, linea, columna - lexema.Length));
                    }
                    else
                    {
                        tokens.Add(new Token("IDENTIFICADOR", lexema, linea, columna - lexema.Length));
                    }
                    continue;
                }

                // Números (enteros y decimales)
                if (char.IsDigit(caracterActual))
                {
                    int inicio = posicion;
                    bool tienePunto = false;
                    while (posicion < codigo.Length && (char.IsDigit(codigo[posicion]) || codigo[posicion] == '.'))
                    {
                        if (codigo[posicion] == '.')
                        {
                            if (tienePunto)
                            {
                                errores.Add(new Error("Léxico", "Número con múltiples puntos decimales", linea, columna));
                                break;
                            }
                            tienePunto = true;
                        }
                        posicion++;
                    }
                    string lexema = codigo.Substring(inicio, posicion - inicio);
                    columna += lexema.Length;
                    tokens.Add(new Token(tienePunto ? "DECIMAL" : "ENTERO", lexema, linea, columna - lexema.Length));
                    continue;
                }

                // Cadenas de texto
                if (caracterActual == '"')
                {
                    int inicio = posicion;
                    posicion++;
                    columna++;
                    bool cerrada = false;

                    while (posicion < codigo.Length)
                    {
                        if (codigo[posicion] == '"')
                        {
                            cerrada = true;
                            posicion++;
                            columna++;
                            break;
                        }
                        if (codigo[posicion] == '\n')
                        {
                            linea++;
                            columna = 1;
                        }
                        posicion++;
                        columna++;
                    }

                    if (!cerrada)
                    {
                        errores.Add(new Error("Léxico", "Cadena de texto no cerrada", linea, columna));
                    }

                    string lexema = codigo.Substring(inicio, posicion - inicio);
                    tokens.Add(new Token("CADENA", lexema, linea, columna - lexema.Length));
                    continue;
                }

                // Operadores y delimitadores
                bool encontrado = false;

                // Verificar operadores de dos caracteres primero
                if (posicion + 1 < codigo.Length)
                {
                    string dosCaracteres = codigo.Substring(posicion, 2);
                    if (Operadores.ContainsKey(dosCaracteres))
                    {
                        tokens.Add(new Token(Operadores[dosCaracteres], dosCaracteres, linea, columna));
                        posicion += 2;
                        columna += 2;
                        encontrado = true;
                    }
                    else if (Delimitadores.ContainsKey(dosCaracteres))
                    {
                        tokens.Add(new Token(Delimitadores[dosCaracteres], dosCaracteres, linea, columna));
                        posicion += 2;
                        columna += 2;
                        encontrado = true;
                    }
                }

                if (!encontrado)
                {
                    string unCaracter = codigo[posicion].ToString();
                    if (Operadores.ContainsKey(unCaracter))
                    {
                        tokens.Add(new Token(Operadores[unCaracter], unCaracter, linea, columna));
                        posicion++;
                        columna++;
                        encontrado = true;
                    }
                    else if (Delimitadores.ContainsKey(unCaracter))
                    {
                        tokens.Add(new Token(Delimitadores[unCaracter], unCaracter, linea, columna));
                        posicion++;
                        columna++;
                        encontrado = true;
                    }
                }

                if (!encontrado)
                {
                    errores.Add(new Error("Léxico", $"Carácter no reconocido: '{caracterActual}'", linea, columna));
                    posicion++;
                    columna++;
                }
            }

            return tokens;
        }
    }
}
