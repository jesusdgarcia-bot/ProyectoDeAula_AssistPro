using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsForms_Presentacion
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool modoDemostracion = args.Length == 1 && args[0] == "--demo";
            Application.Run(modoDemostracion ? DatosDemostracion.CrearFormulario() : new Form1());
        }
    }
}
