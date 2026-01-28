using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gprs
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());
        }
    }
}