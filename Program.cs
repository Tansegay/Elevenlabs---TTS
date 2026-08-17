using System;
using System.Windows.Forms;

namespace ElevenLabsTTS;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
