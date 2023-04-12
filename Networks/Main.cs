using Autodesk.AutoCAD.Runtime;
using acad = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Networks
{
    public class Main : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("NETWORKCORRIDORS")]
        public void Open()
        {
            MainForm mainForm = new MainForm();
            acad.ShowModelessDialog(mainForm);
        }

        [CommandMethod("DRAWPIPE")]
        public void DrawPipe()
        {
            AutocadUtilities.DrawPipe();
        }
    }
}