using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using acad = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Networks
{
    public class Main : IExtensionApplication
    {
        [Conditional("DEBUG")]
        private void AddToPanel()
        {
            // переменные для кнопки(Bitmap для картинки)
            Bitmap bmp = new Bitmap(1, 1);
            bmp.SetPixel(0, 0, Color.Green);
            bmp = new Bitmap(bmp, 1024, 1024);
            IntPtr hBitmap = bmp.GetHbitmap();

            BitmapSource bs =
                System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            // создание кнопок
            RibbonButton buttonFittings = new RibbonButton
            {
                Id = "_button_networks_manager",
                IsToolTipEnabled = true,
                Size = RibbonItemSize.Large,
                ShowImage = true,
                LargeImage = bs,
                ShowText = true,
                Text = "Коридоры для сетей",
                CommandHandler = new CommandHandlerButtonNetworks()
            };

            // создаем контейнер для элементов
            RibbonPanelSource rbPanelSourceLevel = new RibbonPanelSource
            {
                Title = "Сети"
            };
            rbPanelSourceLevel.Items.Add(buttonFittings);
            RibbonPanel rbPanel = new RibbonPanel
            {
                Source = rbPanelSourceLevel
            };

            RibbonControl rbCtrl = ComponentManager.Ribbon;
            RibbonTab rbTab = null;
            foreach (var tab in rbCtrl.Tabs)
            {
                if (tab.Id == "MyPluginsRibbon")
                    rbTab = tab;
            }

            if (rbTab is null)
            {
                rbTab = new RibbonTab
                {
                    Title = "Мои Плагины",
                    Id = "MyPluginsRibbon"
                };
                rbCtrl.Tabs.Add(rbTab);
            }

            rbTab.Panels.Add(rbPanel);
        }
        

        public void Initialize()
        {
            AddToPanel();
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

        [Conditional("DEBUG")]
        [CommandMethod("CURVAS")]
        public void Curvas()
        {
            
        }
        
    }
    
    public class CommandHandlerButtonNetworks : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object param)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            MainForm mainForm = new MainForm();
            acad.ShowModelessDialog(mainForm);
        }
    }
}
