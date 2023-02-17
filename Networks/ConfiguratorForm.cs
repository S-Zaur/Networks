using System;
using System.Linq;
using System.Windows.Forms;

namespace Networks
{
    public partial class ConfiguratorForm : Form
    {
        public ConfiguratorForm()
        {
            InitializeComponent();
            var layers = AutocadHelper.GetAllLayers();
            WaterPipeComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            SewerComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            HeatingNetworksComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            CommunicationCableComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            PowerCableComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            
            WaterPipeComboBox.Text = Properties.Settings.Default.WaterPipeLayerName;
            SewerComboBox.Text = Properties.Settings.Default.SewerLayerName;
            HeatingNetworksComboBox.Text = Properties.Settings.Default.HeatingNetworkLayerName;
            CommunicationCableComboBox.Text = Properties.Settings.Default.CommunicationCableLayerName;
            PowerCableComboBox.Text = Properties.Settings.Default.PowerCableLayerName;
        }

        private bool CheckUnique()
        {
            var layers = new[]
            {
                WaterPipeComboBox.Text,
                SewerComboBox.Text,
                HeatingNetworksComboBox.Text,
                CommunicationCableComboBox.Text,
                PowerCableComboBox.Text
            };
            return layers.Distinct().Count() == layers.Length;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!CheckUnique())
            {
                // ReSharper disable once LocalizableElement
                MessageBox.Show("Слои должны быть уникальными");
                return;
            }
            Properties.Settings.Default.WaterPipeLayerName = WaterPipeComboBox.Text;            
            Properties.Settings.Default.SewerLayerName = SewerComboBox.Text;            
            Properties.Settings.Default.HeatingNetworkLayerName = HeatingNetworksComboBox.Text;            
            Properties.Settings.Default.CommunicationCableLayerName = CommunicationCableComboBox.Text;            
            Properties.Settings.Default.PowerCableLayerName = PowerCableComboBox.Text;
            Properties.Settings.Default.Save();
            Close();
        }
    }
}