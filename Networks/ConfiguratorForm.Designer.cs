using System.ComponentModel;

namespace Networks
{
    partial class ConfiguratorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.WaterPipeLabel = new System.Windows.Forms.Label();
            this.WaterPipeComboBox = new System.Windows.Forms.ComboBox();
            this.SewerLabel = new System.Windows.Forms.Label();
            this.HeatingNetworksLabel = new System.Windows.Forms.Label();
            this.CommunicationCableLabel = new System.Windows.Forms.Label();
            this.PowerCableLabel = new System.Windows.Forms.Label();
            this.SewerComboBox = new System.Windows.Forms.ComboBox();
            this.HeatingNetworksComboBox = new System.Windows.Forms.ComboBox();
            this.CommunicationCableComboBox = new System.Windows.Forms.ComboBox();
            this.PowerCableComboBox = new System.Windows.Forms.ComboBox();
            this.SaveButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // WaterPipeLabel
            // 
            this.WaterPipeLabel.Location = new System.Drawing.Point(12, 9);
            this.WaterPipeLabel.Name = "WaterPipeLabel";
            this.WaterPipeLabel.Size = new System.Drawing.Size(100, 23);
            this.WaterPipeLabel.TabIndex = 0;
            this.WaterPipeLabel.Text = "Водопроводы";
            // 
            // WaterPipeComboBox
            // 
            this.WaterPipeComboBox.FormattingEnabled = true;
            this.WaterPipeComboBox.Location = new System.Drawing.Point(163, 6);
            this.WaterPipeComboBox.Name = "WaterPipeComboBox";
            this.WaterPipeComboBox.Size = new System.Drawing.Size(121, 24);
            this.WaterPipeComboBox.TabIndex = 1;
            // 
            // SewerLabel
            // 
            this.SewerLabel.Location = new System.Drawing.Point(12, 39);
            this.SewerLabel.Name = "SewerLabel";
            this.SewerLabel.Size = new System.Drawing.Size(100, 23);
            this.SewerLabel.TabIndex = 2;
            this.SewerLabel.Text = "Канализации";
            // 
            // HeatingNetworksLabel
            // 
            this.HeatingNetworksLabel.Location = new System.Drawing.Point(12, 69);
            this.HeatingNetworksLabel.Name = "HeatingNetworksLabel";
            this.HeatingNetworksLabel.Size = new System.Drawing.Size(100, 23);
            this.HeatingNetworksLabel.TabIndex = 3;
            this.HeatingNetworksLabel.Text = "Теплосети";
            // 
            // CommunicationCableLabel
            // 
            this.CommunicationCableLabel.Location = new System.Drawing.Point(12, 99);
            this.CommunicationCableLabel.Name = "CommunicationCableLabel";
            this.CommunicationCableLabel.Size = new System.Drawing.Size(100, 23);
            this.CommunicationCableLabel.TabIndex = 4;
            this.CommunicationCableLabel.Text = "Линии связи";
            // 
            // PowerCableLabel
            // 
            this.PowerCableLabel.Location = new System.Drawing.Point(12, 129);
            this.PowerCableLabel.Name = "PowerCableLabel";
            this.PowerCableLabel.Size = new System.Drawing.Size(145, 23);
            this.PowerCableLabel.TabIndex = 5;
            this.PowerCableLabel.Text = "Силовые кабели";
            // 
            // SewerComboBox
            // 
            this.SewerComboBox.FormattingEnabled = true;
            this.SewerComboBox.Location = new System.Drawing.Point(163, 36);
            this.SewerComboBox.Name = "SewerComboBox";
            this.SewerComboBox.Size = new System.Drawing.Size(121, 24);
            this.SewerComboBox.TabIndex = 6;
            // 
            // HeatingNetworksComboBox
            // 
            this.HeatingNetworksComboBox.FormattingEnabled = true;
            this.HeatingNetworksComboBox.Location = new System.Drawing.Point(163, 66);
            this.HeatingNetworksComboBox.Name = "HeatingNetworksComboBox";
            this.HeatingNetworksComboBox.Size = new System.Drawing.Size(121, 24);
            this.HeatingNetworksComboBox.TabIndex = 7;
            // 
            // CommunicationCableComboBox
            // 
            this.CommunicationCableComboBox.FormattingEnabled = true;
            this.CommunicationCableComboBox.Location = new System.Drawing.Point(163, 96);
            this.CommunicationCableComboBox.Name = "CommunicationCableComboBox";
            this.CommunicationCableComboBox.Size = new System.Drawing.Size(121, 24);
            this.CommunicationCableComboBox.TabIndex = 8;
            // 
            // PowerCableComboBox
            // 
            this.PowerCableComboBox.FormattingEnabled = true;
            this.PowerCableComboBox.Location = new System.Drawing.Point(163, 126);
            this.PowerCableComboBox.Name = "PowerCableComboBox";
            this.PowerCableComboBox.Size = new System.Drawing.Size(121, 24);
            this.PowerCableComboBox.TabIndex = 9;
            // 
            // SaveButton
            // 
            this.SaveButton.Location = new System.Drawing.Point(102, 156);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(93, 23);
            this.SaveButton.TabIndex = 10;
            this.SaveButton.Text = "Сохранить";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // ConfiguratorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 193);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.PowerCableComboBox);
            this.Controls.Add(this.CommunicationCableComboBox);
            this.Controls.Add(this.HeatingNetworksComboBox);
            this.Controls.Add(this.SewerComboBox);
            this.Controls.Add(this.PowerCableLabel);
            this.Controls.Add(this.CommunicationCableLabel);
            this.Controls.Add(this.HeatingNetworksLabel);
            this.Controls.Add(this.SewerLabel);
            this.Controls.Add(this.WaterPipeComboBox);
            this.Controls.Add(this.WaterPipeLabel);
            this.MaximumSize = new System.Drawing.Size(330, 240);
            this.MinimumSize = new System.Drawing.Size(330, 240);
            this.Name = "ConfiguratorForm";
            this.Text = "Настройка слоев";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label PowerCableLabel;

        private System.Windows.Forms.Button SaveButton;

        private System.Windows.Forms.Label WaterPipeLabel;
        private System.Windows.Forms.ComboBox WaterPipeComboBox;
        private System.Windows.Forms.Label SewerLabel;
        private System.Windows.Forms.Label HeatingNetworksLabel;
        private System.Windows.Forms.Label CommunicationCableLabel;
        private System.Windows.Forms.ComboBox SewerComboBox;
        private System.Windows.Forms.ComboBox HeatingNetworksComboBox;
        private System.Windows.Forms.ComboBox CommunicationCableComboBox;
        private System.Windows.Forms.ComboBox PowerCableComboBox;

        #endregion
    }
}