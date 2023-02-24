using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace Networks
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public partial class MainForm : Form
    {
        public MainForm()
        {
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
            AutocadHelper.CheckLayers();
        }

        private void DrawButton_Click(object sender, EventArgs e)
        {
            NetworkManager.SetPipeType(
                WaterPipeTypeComboBox.SelectedItem.ToString(),
                double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text)
            );

            WindowState = FormWindowState.Minimized;

            AutocadHelper.DrawNetworksByLine(
                CreateNetworkArray(),
                new[]
                {
                    double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text) / 1000,
                    double.Parse(SewersSizeTextBox.Text == "" ? "0" : SewersSizeTextBox.Text) / 1000,
                    double.Parse(HeatingNetworksSizeTextBox.Text == "" ? "0" : HeatingNetworksSizeTextBox.Text) / 1000
                }
            );

            WindowState = FormWindowState.Normal;
        }

        private Networks[] CreateNetworkArray()
        {
            List<Networks> lst = new List<Networks>();
            if (WaterPipesCheckBox.Checked)
                lst.Add(Networks.WaterPipe);
            if (SewersCheckBox.Checked)
                lst.Add(Networks.HouseholdSewer);
            if (PowerCablesCheckBox.Checked)
                lst.Add(Networks.PowerCable);
            if (CommunicationLinesCheckBox.Checked)
                lst.Add(Networks.CommunicationCable);
            if (HeatingNetworksCheckBox.Checked)
                lst.Add(Networks.HeatingNetworks);
            return lst.ToArray();
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
            AutocadHelper.CheckLayers();
        }

        private void DrawByAreaButton_Click(object sender, EventArgs e)
        {
            NetworkManager.SetPipeType(
                WaterPipeTypeComboBox.SelectedItem.ToString(),
                double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text)
            );

            WindowState = FormWindowState.Minimized;

            AutocadHelper.DrawNetworksByArea(
                CreateNetworkArray(),
                new[]
                {
                    double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text) / 1000,
                    double.Parse(SewersSizeTextBox.Text == "" ? "0" : SewersSizeTextBox.Text) / 1000,
                    double.Parse(HeatingNetworksSizeTextBox.Text == "" ? "0" : HeatingNetworksSizeTextBox.Text) / 1000
                }
            );

            WindowState = FormWindowState.Normal;
        }

        private void DrawByPointsButton_Click(object sender, EventArgs e)
        {
            NetworkManager.SetPipeType(
                WaterPipeTypeComboBox.SelectedItem.ToString(),
                double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text)
            );
            var networks = CreateNetworkArray();
            if (networks.Length == 0)
                return;
            if (networks.Length > 1)
            {
                MessageBox.Show("В данном режиме можно выбрать только одну коммуникацию");
                return;
            }

            var size = 0.0;
            if (WaterPipesCheckBox.Checked)
                size = double.Parse(WaterPipesSizeTextBox.Text == "" ? "0" : WaterPipesSizeTextBox.Text) / 1000;
            if (SewersCheckBox.Checked)
                size = double.Parse(SewersSizeTextBox.Text == "" ? "0" : SewersSizeTextBox.Text) / 1000;
            if (HeatingNetworksCheckBox.Checked)
                size = double.Parse(HeatingNetworksSizeTextBox.Text == "" ? "0" : HeatingNetworksSizeTextBox.Text) /
                       1000;
            WindowState = FormWindowState.Minimized;

            AutocadHelper.DrawNetworksByPoints(networks.First(), size);

            WindowState = FormWindowState.Normal;
        }
    }
}