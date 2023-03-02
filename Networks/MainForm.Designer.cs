namespace Networks
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DrawByLineButton = new System.Windows.Forms.Button();
            this.WaterPipesCheckBox = new System.Windows.Forms.CheckBox();
            this.SewersCheckBox = new System.Windows.Forms.CheckBox();
            this.HeatingNetworksCheckBox = new System.Windows.Forms.CheckBox();
            this.CommunicationLinesCheckBox = new System.Windows.Forms.CheckBox();
            this.PowerCablesCheckBox = new System.Windows.Forms.CheckBox();
            this.SewersSizeTextBox = new System.Windows.Forms.TextBox();
            this.WaterPipesSizeTextBox = new System.Windows.Forms.TextBox();
            this.HeatingNetworksSizeTextBox = new System.Windows.Forms.TextBox();
            this.WaterPipeTypeComboBox = new System.Windows.Forms.ComboBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.ConfigureLayersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CommunictionLabel = new System.Windows.Forms.Label();
            this.SizeLabel = new System.Windows.Forms.Label();
            this.TypeLabel = new System.Windows.Forms.Label();
            this.DrawByAreaButton = new System.Windows.Forms.Button();
            this.AllowIntersectionCheckBox = new System.Windows.Forms.CheckBox();
            this.GasPipeCheckBox = new System.Windows.Forms.CheckBox();
            this.GasPipePressureTextBox = new System.Windows.Forms.TextBox();
            this.PressureLabel = new System.Windows.Forms.Label();
            this.DrawButton = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // DrawByLineButton
            // 
            this.DrawByLineButton.Location = new System.Drawing.Point(31, 246);
            this.DrawByLineButton.Name = "DrawByLineButton";
            this.DrawByLineButton.Size = new System.Drawing.Size(102, 28);
            this.DrawByLineButton.TabIndex = 0;
            this.DrawByLineButton.Text = "Вдоль линии";
            this.DrawByLineButton.UseVisualStyleBackColor = true;
            this.DrawByLineButton.Click += new System.EventHandler(this.DrawByLineButton_Click);
            // 
            // WaterPipesCheckBox
            // 
            this.WaterPipesCheckBox.AutoSize = true;
            this.WaterPipesCheckBox.Location = new System.Drawing.Point(12, 65);
            this.WaterPipesCheckBox.Name = "WaterPipesCheckBox";
            this.WaterPipesCheckBox.Size = new System.Drawing.Size(120, 21);
            this.WaterPipesCheckBox.TabIndex = 1;
            this.WaterPipesCheckBox.Tag = "Водопровод";
            this.WaterPipesCheckBox.Text = "Водопроводы";
            this.WaterPipesCheckBox.UseVisualStyleBackColor = true;
            this.WaterPipesCheckBox.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // SewersCheckBox
            // 
            this.SewersCheckBox.AutoSize = true;
            this.SewersCheckBox.Location = new System.Drawing.Point(12, 92);
            this.SewersCheckBox.Name = "SewersCheckBox";
            this.SewersCheckBox.Size = new System.Drawing.Size(118, 21);
            this.SewersCheckBox.TabIndex = 2;
            this.SewersCheckBox.Tag = "Канализация";
            this.SewersCheckBox.Text = "Канализации";
            this.SewersCheckBox.UseVisualStyleBackColor = true;
            this.SewersCheckBox.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // HeatingNetworksCheckBox
            // 
            this.HeatingNetworksCheckBox.AutoSize = true;
            this.HeatingNetworksCheckBox.Location = new System.Drawing.Point(12, 118);
            this.HeatingNetworksCheckBox.Name = "HeatingNetworksCheckBox";
            this.HeatingNetworksCheckBox.Size = new System.Drawing.Size(101, 21);
            this.HeatingNetworksCheckBox.TabIndex = 3;
            this.HeatingNetworksCheckBox.Tag = "Теплосеть";
            this.HeatingNetworksCheckBox.Text = "Теплосети";
            this.HeatingNetworksCheckBox.UseVisualStyleBackColor = true;
            this.HeatingNetworksCheckBox.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // CommunicationLinesCheckBox
            // 
            this.CommunicationLinesCheckBox.AutoSize = true;
            this.CommunicationLinesCheckBox.Location = new System.Drawing.Point(12, 144);
            this.CommunicationLinesCheckBox.Name = "CommunicationLinesCheckBox";
            this.CommunicationLinesCheckBox.Size = new System.Drawing.Size(113, 21);
            this.CommunicationLinesCheckBox.TabIndex = 4;
            this.CommunicationLinesCheckBox.Tag = "Линия связи";
            this.CommunicationLinesCheckBox.Text = "Линии связи";
            this.CommunicationLinesCheckBox.UseVisualStyleBackColor = true;
            this.CommunicationLinesCheckBox.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // PowerCablesCheckBox
            // 
            this.PowerCablesCheckBox.AutoSize = true;
            this.PowerCablesCheckBox.Location = new System.Drawing.Point(12, 170);
            this.PowerCablesCheckBox.Name = "PowerCablesCheckBox";
            this.PowerCablesCheckBox.Size = new System.Drawing.Size(139, 21);
            this.PowerCablesCheckBox.TabIndex = 5;
            this.PowerCablesCheckBox.Tag = "Силовой кабель";
            this.PowerCablesCheckBox.Text = "Силовые кабели";
            this.PowerCablesCheckBox.UseVisualStyleBackColor = true;
            this.PowerCablesCheckBox.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // SewersSizeTextBox
            // 
            this.SewersSizeTextBox.Location = new System.Drawing.Point(160, 90);
            this.SewersSizeTextBox.Name = "SewersSizeTextBox";
            this.SewersSizeTextBox.Size = new System.Drawing.Size(100, 22);
            this.SewersSizeTextBox.TabIndex = 6;
            this.SewersSizeTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            // 
            // WaterPipesSizeTextBox
            // 
            this.WaterPipesSizeTextBox.Location = new System.Drawing.Point(160, 63);
            this.WaterPipesSizeTextBox.Name = "WaterPipesSizeTextBox";
            this.WaterPipesSizeTextBox.Size = new System.Drawing.Size(100, 22);
            this.WaterPipesSizeTextBox.TabIndex = 7;
            this.WaterPipesSizeTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            // 
            // HeatingNetworksSizeTextBox
            // 
            this.HeatingNetworksSizeTextBox.Location = new System.Drawing.Point(160, 116);
            this.HeatingNetworksSizeTextBox.Name = "HeatingNetworksSizeTextBox";
            this.HeatingNetworksSizeTextBox.Size = new System.Drawing.Size(100, 22);
            this.HeatingNetworksSizeTextBox.TabIndex = 8;
            this.HeatingNetworksSizeTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            // 
            // WaterPipeTypeComboBox
            // 
            this.WaterPipeTypeComboBox.FormattingEnabled = true;
            this.WaterPipeTypeComboBox.Items.AddRange(new object[] { "Железобетонные и асбестоцементные трубы", "Чугунные трубы", "Пластмассовые трубы" });
            this.WaterPipeTypeComboBox.Location = new System.Drawing.Point(266, 63);
            this.WaterPipeTypeComboBox.Name = "WaterPipeTypeComboBox";
            this.WaterPipeTypeComboBox.Size = new System.Drawing.Size(121, 24);
            this.WaterPipeTypeComboBox.TabIndex = 9;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.ConfigureLayersToolStripMenuItem });
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(397, 28);
            this.menuStrip1.TabIndex = 10;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // ConfigureLayersToolStripMenuItem
            // 
            this.ConfigureLayersToolStripMenuItem.Name = "ConfigureLayersToolStripMenuItem";
            this.ConfigureLayersToolStripMenuItem.Size = new System.Drawing.Size(131, 24);
            this.ConfigureLayersToolStripMenuItem.Text = "Настроить слои";
            this.ConfigureLayersToolStripMenuItem.Click += new System.EventHandler(this.ConfigureLayersToolStripMenuItem_Click);
            // 
            // CommunictionLabel
            // 
            this.CommunictionLabel.Location = new System.Drawing.Point(13, 37);
            this.CommunictionLabel.Name = "CommunictionLabel";
            this.CommunictionLabel.Size = new System.Drawing.Size(152, 23);
            this.CommunictionLabel.TabIndex = 11;
            this.CommunictionLabel.Text = "Коммуникации";
            // 
            // SizeLabel
            // 
            this.SizeLabel.Location = new System.Drawing.Point(160, 37);
            this.SizeLabel.Name = "SizeLabel";
            this.SizeLabel.Size = new System.Drawing.Size(100, 23);
            this.SizeLabel.TabIndex = 12;
            this.SizeLabel.Text = "Размер (мм)";
            // 
            // TypeLabel
            // 
            this.TypeLabel.Location = new System.Drawing.Point(266, 37);
            this.TypeLabel.Name = "TypeLabel";
            this.TypeLabel.Size = new System.Drawing.Size(100, 23);
            this.TypeLabel.TabIndex = 13;
            this.TypeLabel.Text = "Тип";
            // 
            // DrawByAreaButton
            // 
            this.DrawByAreaButton.Location = new System.Drawing.Point(139, 246);
            this.DrawByAreaButton.Name = "DrawByAreaButton";
            this.DrawByAreaButton.Size = new System.Drawing.Size(102, 28);
            this.DrawByAreaButton.TabIndex = 14;
            this.DrawByAreaButton.Text = "В области";
            this.DrawByAreaButton.UseVisualStyleBackColor = true;
            this.DrawByAreaButton.Click += new System.EventHandler(this.DrawByAreaButton_Click);
            // 
            // AllowIntersectionCheckBox
            // 
            this.AllowIntersectionCheckBox.Enabled = false;
            this.AllowIntersectionCheckBox.Location = new System.Drawing.Point(17, 280);
            this.AllowIntersectionCheckBox.Name = "AllowIntersectionCheckBox";
            this.AllowIntersectionCheckBox.Size = new System.Drawing.Size(224, 24);
            this.AllowIntersectionCheckBox.TabIndex = 17;
            this.AllowIntersectionCheckBox.Text = "Разрешить пересечения";
            this.AllowIntersectionCheckBox.UseVisualStyleBackColor = true;
            this.AllowIntersectionCheckBox.CheckedChanged += new System.EventHandler(this.AllowIntersectionCheckBox_CheckedChanged);
            // 
            // GasPipeCheckBox
            // 
            this.GasPipeCheckBox.Location = new System.Drawing.Point(12, 197);
            this.GasPipeCheckBox.Name = "GasPipeCheckBox";
            this.GasPipeCheckBox.Size = new System.Drawing.Size(139, 24);
            this.GasPipeCheckBox.TabIndex = 18;
            this.GasPipeCheckBox.Tag = "Газопровод";
            this.GasPipeCheckBox.Text = "Газопровод";
            this.GasPipeCheckBox.UseVisualStyleBackColor = true;
            this.GasPipeCheckBox.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // GasPipePressureTextBox
            // 
            this.GasPipePressureTextBox.Location = new System.Drawing.Point(160, 197);
            this.GasPipePressureTextBox.Name = "GasPipePressureTextBox";
            this.GasPipePressureTextBox.Size = new System.Drawing.Size(100, 22);
            this.GasPipePressureTextBox.TabIndex = 19;
            this.GasPipePressureTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_KeyPress);
            // 
            // PressureLabel
            // 
            this.PressureLabel.Location = new System.Drawing.Point(160, 171);
            this.PressureLabel.Name = "PressureLabel";
            this.PressureLabel.Size = new System.Drawing.Size(100, 23);
            this.PressureLabel.TabIndex = 20;
            this.PressureLabel.Text = "Давление";
            // 
            // DrawButton
            // 
            this.DrawButton.Location = new System.Drawing.Point(247, 246);
            this.DrawButton.Name = "DrawButton";
            this.DrawButton.Size = new System.Drawing.Size(102, 28);
            this.DrawButton.TabIndex = 21;
            this.DrawButton.Text = "Нарисовать";
            this.DrawButton.UseVisualStyleBackColor = true;
            this.DrawButton.Click += new System.EventHandler(this.DrawButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 323);
            this.Controls.Add(this.DrawButton);
            this.Controls.Add(this.PressureLabel);
            this.Controls.Add(this.GasPipePressureTextBox);
            this.Controls.Add(this.GasPipeCheckBox);
            this.Controls.Add(this.AllowIntersectionCheckBox);
            this.Controls.Add(this.DrawByAreaButton);
            this.Controls.Add(this.TypeLabel);
            this.Controls.Add(this.SizeLabel);
            this.Controls.Add(this.CommunictionLabel);
            this.Controls.Add(this.WaterPipeTypeComboBox);
            this.Controls.Add(this.HeatingNetworksSizeTextBox);
            this.Controls.Add(this.WaterPipesSizeTextBox);
            this.Controls.Add(this.SewersSizeTextBox);
            this.Controls.Add(this.PowerCablesCheckBox);
            this.Controls.Add(this.CommunicationLinesCheckBox);
            this.Controls.Add(this.HeatingNetworksCheckBox);
            this.Controls.Add(this.SewersCheckBox);
            this.Controls.Add(this.WaterPipesCheckBox);
            this.Controls.Add(this.DrawByLineButton);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MaximumSize = new System.Drawing.Size(415, 370);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(415, 370);
            this.Name = "MainForm";
            this.Text = "Коридоры для сетей";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button DrawButton;

        private System.Windows.Forms.Label PressureLabel;

        private System.Windows.Forms.TextBox GasPipePressureTextBox;

        private System.Windows.Forms.CheckBox GasPipeCheckBox;

        private System.Windows.Forms.CheckBox AllowIntersectionCheckBox;

        private System.Windows.Forms.Button DrawByAreaButton;

        private System.Windows.Forms.Label CommunictionLabel;
        private System.Windows.Forms.Label SizeLabel;
        private System.Windows.Forms.Label TypeLabel;

        private System.Windows.Forms.ToolStripMenuItem ConfigureLayersToolStripMenuItem;

        private System.Windows.Forms.MenuStrip menuStrip1;

        #endregion

        private System.Windows.Forms.Button DrawByLineButton;
        private System.Windows.Forms.CheckBox WaterPipesCheckBox;
        private System.Windows.Forms.CheckBox SewersCheckBox;
        private System.Windows.Forms.CheckBox HeatingNetworksCheckBox;
        private System.Windows.Forms.CheckBox CommunicationLinesCheckBox;
        private System.Windows.Forms.CheckBox PowerCablesCheckBox;
        private System.Windows.Forms.TextBox SewersSizeTextBox;
        private System.Windows.Forms.TextBox WaterPipesSizeTextBox;
        private System.Windows.Forms.TextBox HeatingNetworksSizeTextBox;
        private System.Windows.Forms.ComboBox WaterPipeTypeComboBox;
    }
}

