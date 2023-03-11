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
            var layers = AutocadUtilities.GetAllLayers();
            WaterPipeComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            SewerComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            HeatingNetworksComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            CommunicationCableComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            PowerCableComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            GasPipeComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            BuildingsFoundationComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            StreetSideStoneComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            ExternalEdgeComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            HvlSupportsFoundation1ComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            HvlSupportsFoundation35ComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            HvlSupportsFoundationOverComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            RedLineComboBox.Items.AddRange(layers.Cast<object>().ToArray());
            
            WaterPipeComboBox.Text = Properties.Settings.Default.WaterPipeLayerName;
            SewerComboBox.Text = Properties.Settings.Default.SewerLayerName;
            HeatingNetworksComboBox.Text = Properties.Settings.Default.HeatingNetworkLayerName;
            CommunicationCableComboBox.Text = Properties.Settings.Default.CommunicationCableLayerName;
            PowerCableComboBox.Text = Properties.Settings.Default.PowerCableLayerName;
            GasPipeComboBox.Text = Properties.Settings.Default.GasPipeLayerName;
            BuildingsFoundationComboBox.Text = Properties.Settings.Default.BuildingsFoundationLayerName;
            StreetSideStoneComboBox.Text = Properties.Settings.Default.StreetSideStoneLayerName;
            ExternalEdgeComboBox.Text = Properties.Settings.Default.ExternalEdgeLayerName;
            HvlSupportsFoundation1ComboBox.Text = Properties.Settings.Default.HvlSupportsFoundation1LayerName;
            HvlSupportsFoundation35ComboBox.Text = Properties.Settings.Default.HvlSupportsFoundation35LayerName;
            HvlSupportsFoundationOverComboBox.Text = Properties.Settings.Default.HvlSupportsFoundationOverLayerName;
            RedLineComboBox.Text = Properties.Settings.Default.RedLineLayerName;
        }

        private bool CheckUnique()
        {
            var layers = new[]
            {
                WaterPipeComboBox.Text,
                SewerComboBox.Text,
                HeatingNetworksComboBox.Text,
                CommunicationCableComboBox.Text,
                PowerCableComboBox.Text,
                GasPipeComboBox.Text,
                BuildingsFoundationComboBox.Text,
                StreetSideStoneComboBox.Text,
                ExternalEdgeComboBox.Text,
                HvlSupportsFoundation1ComboBox.Text,
                HvlSupportsFoundation35ComboBox.Text,
                HvlSupportsFoundationOverComboBox.Text,
                RedLineComboBox.Text,
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
            Properties.Settings.Default.GasPipeLayerName = GasPipeComboBox.Text;
            Properties.Settings.Default.BuildingsFoundationLayerName = BuildingsFoundationComboBox.Text;
            Properties.Settings.Default.StreetSideStoneLayerName = StreetSideStoneComboBox.Text;
            Properties.Settings.Default.ExternalEdgeLayerName = ExternalEdgeComboBox.Text;
            Properties.Settings.Default.HvlSupportsFoundation1LayerName = HvlSupportsFoundation1ComboBox.Text;
            Properties.Settings.Default.HvlSupportsFoundation35LayerName = HvlSupportsFoundation35ComboBox.Text;
            Properties.Settings.Default.HvlSupportsFoundationOverLayerName = HvlSupportsFoundationOverComboBox.Text;
            Properties.Settings.Default.RedLineLayerName =RedLineComboBox.Text;
            Properties.Settings.Default.Save();
            Close();
        }
    }
}