using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CompiladorFinal;

namespace CompiladorFinal
{
    public partial class Form1 : Form
    {
        private AnalizadorLexico analizadorLexico;
        private AnalizadorSintactico analizadorSintactico;
        private AnalizadorSemantico analizadorSemantico;

        public Form1()
        {
            InitializeComponent();
            analizadorLexico = new AnalizadorLexico();
            analizadorSintactico = new AnalizadorSintactico();
            analizadorSemantico = new AnalizadorSemantico();
        }


        private void btnAnalizar_Click(object sender, EventArgs e)
        {
            lstTokens.Items.Clear();
            txtErrores.Clear(); // Limpiar el TextBox de errores

            string codigo = txtCodigo.Text;
            List<Error> errores = new List<Error>();

            // An�lisis l�xico
            List<Token> tokens = analizadorLexico.Analizar(codigo, errores);

            // Mostrar tokens
            foreach (Token token in tokens)
            {
                lstTokens.Items.Add(token.ToString());
            }

            // Mostrar errores l�xicos
            if (errores.Count > 0)
            {
                txtErrores.AppendText("=== ERRORES L�XICOS ===\r\n");
                foreach (Error error in errores)
                {
                    txtErrores.AppendText(error.ToString() + "\r\n"); // Reemplazo aqu�
                }
                txtErrores.AppendText("\r\n");
            }

            // An�lisis sint�ctico
            List<Error> erroresSintacticos = analizadorSintactico.Analizar(tokens);

            // Mostrar errores sint�cticos
            if (erroresSintacticos.Count > 0)
            {
                txtErrores.AppendText("=== ERRORES SINT�CTICOS ===\r\n");
                foreach (Error error in erroresSintacticos)
                {
                    txtErrores.AppendText(error.ToString() + "\r\n"); // Reemplazo aqu�
                }
                txtErrores.AppendText("\r\n");
            }

            // An�lisis sem�ntico
            List<Error> erroresSemanticos = analizadorSemantico.Analizar(tokens);

            // Mostrar errores sem�nticos
            if (erroresSemanticos.Count > 0)
            {
                txtErrores.AppendText("=== ERRORES SEM�NTICOS ===\r\n");
                foreach (Error error in erroresSemanticos)
                {
                    txtErrores.AppendText(error.ToString() + "\r\n"); // Reemplazo aqu�
                }
                txtErrores.AppendText("\r\n");
            }

            if (errores.Count == 0 && erroresSintacticos.Count == 0 && erroresSemanticos.Count == 0)
            {
                txtErrores.AppendText("COMPILACI�N EXITOSA - SIN ERRORES\r\n");
            }
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtCodigo.Clear();
            lstTokens.Items.Clear();
            txtErrores.Clear(); // Cambio aqu�
        }
    }
}

