using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Windows.Forms;
using Autodesk.AutoCAD.Geometry;

namespace Networks
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public partial class MainForm : Form
    {
        private static readonly DateTime StopDate = new DateTime(2023, 3, 20);

        private readonly Dictionary<Networks, Pair<Point3d, Point3d>> _points =
            new Dictionary<Networks, Pair<Point3d, Point3d>>();

        private static DateTime GetCurrentDate()
        {
            try
            {
                using (var response =
                       WebRequest.Create("https://fantastic-liquid-b56.notion.site/BOMB-1722dae092e749e2bb7749a50c8e9a36").GetResponse())
                    //string todaysDates =  response.Headers["date"];
                    return DateTime.ParseExact(response.Headers["date"],
                        "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                        CultureInfo.InvariantCulture.DateTimeFormat,
                        DateTimeStyles.AssumeUniversal);
            }
            catch (WebException)
            {
                return new DateTime(2100,1,1);
            }
        }

        public MainForm()
        {
            if (GetCurrentDate() > StopDate)
                return;
            InitializeComponent();
            WaterPipeTypeComboBox.SelectedIndex = 2;
            if (!Properties.Settings.Default.Configured)
            {
                ConfiguratorForm form = new ConfiguratorForm();
                Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);
                Properties.Settings.Default.Configured = true;
                Properties.Settings.Default.Save();
            }

            NetworkManager.SetLayers();
            AutocadUtilities.CheckLayers();
            WaterPipesCheckBox.Tag = Networks.WaterPipe;
            SewersCheckBox.Tag = Networks.Sewer;
            HeatingNetworksCheckBox.Tag = Networks.HeatingNetworks;
            CommunicationLinesCheckBox.Tag = Networks.CommunicationCable;
            PowerCablesCheckBox.Tag = Networks.PowerCable;
            GasPipeCheckBox.Tag = Networks.GasPipe;
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (!char.IsDigit(number) && number != 8 && number != '.') // цифры, клавиша BackSpace и запятая
            {
                e.Handled = true;
            }
        }

        private void ConfigureLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfiguratorForm form = new ConfiguratorForm();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);
            NetworkManager.SetLayers();
            AutocadUtilities.CheckLayers();
        }

        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox is null)
                return;
            if (!checkBox.Checked)
            {
                _points.Remove((Networks)checkBox.Tag);
                return;
            }

            try
            {
                WindowState = FormWindowState.Minimized;
                var points = AutocadHelper.GetStartEndPoints();
                _points.Add((Networks)checkBox.Tag, points);
            }
            catch
            {
                checkBox.Checked = false;
            }
            finally
            {
                WindowState = FormWindowState.Normal;
            }
        }

        private void DrawButton_Click(object sender, EventArgs e)
        {
            NetworkManager.SetPipeType(
                WaterPipeTypeComboBox.SelectedItem.ToString(),
                double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text)
            );
            NetworkManager.SetGasPipePressure(
                double.Parse(GasPipePressureTextBox.Text == "" ? "0" : GasPipePressureTextBox.Text)
            );

            WindowState = FormWindowState.Minimized;

            AutocadHelper.DrawNetworks(
                _points,
                new[]
                {
                    double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text) / 1000,
                    double.Parse(SewersSizeTextBox.Text == "" ? "0" : SewersSizeTextBox.Text) / 1000,
                    double.Parse(HeatingNetworksSizeTextBox.Text == "" ? "0" : HeatingNetworksSizeTextBox.Text) / 1000
                }
            );

            WindowState = FormWindowState.Normal;
        }

        private void StrictModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO Вопрос нужна ли настройка строго режима
            Properties.Settings.Default.AllowIntersection = StrictModeToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;
            if (!char.IsDigit(number) && number != 8)
            {
                e.Handled = true;
            }

            if (int.TryParse(MinAngleToolStripTextBox.Text + number, out int angle) && number != 8 && angle > 90)
            {
                e.Handled = true;
            }
        }

        private void MinAngleToolStripTextBox_TextChanged(object sender, EventArgs e)
        {
            AutocadHelper.MinAngle = int.Parse(MinAngleToolStripTextBox.Text==""?"90":MinAngleToolStripTextBox.Text);
        }
    }
}