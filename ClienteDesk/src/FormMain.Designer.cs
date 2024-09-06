namespace ClienteDesk.src
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lbDescMeuID = new Label();
            lbMeuID = new Label();
            lbIdParceiro = new Label();
            tbxIdParceiro = new TextBox();
            btnConectar = new Button();
            SuspendLayout();
            // 
            // lbDescMeuID
            // 
            lbDescMeuID.AutoSize = true;
            lbDescMeuID.Location = new Point(27, 18);
            lbDescMeuID.Name = "lbDescMeuID";
            lbDescMeuID.Size = new Size(45, 15);
            lbDescMeuID.TabIndex = 0;
            lbDescMeuID.Text = "Meu ID";
            // 
            // lbMeuID
            // 
            lbMeuID.AutoSize = true;
            lbMeuID.Font = new Font("Segoe UI", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lbMeuID.Location = new Point(22, 34);
            lbMeuID.Name = "lbMeuID";
            lbMeuID.Size = new Size(221, 40);
            lbMeuID.TabIndex = 1;
            lbMeuID.Text = "0000.0000.0000";
            lbMeuID.Click += lbMeuID_Click;
            // 
            // lbIdParceiro
            // 
            lbIdParceiro.AutoSize = true;
            lbIdParceiro.Location = new Point(27, 94);
            lbIdParceiro.Name = "lbIdParceiro";
            lbIdParceiro.Size = new Size(81, 15);
            lbIdParceiro.TabIndex = 2;
            lbIdParceiro.Text = "ID do parceiro";
            // 
            // tbxIdParceiro
            // 
            tbxIdParceiro.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbxIdParceiro.Location = new Point(27, 115);
            tbxIdParceiro.MaxLength = 12;
            tbxIdParceiro.Name = "tbxIdParceiro";
            tbxIdParceiro.Size = new Size(200, 29);
            tbxIdParceiro.TabIndex = 3;
            // 
            // btnConectar
            // 
            btnConectar.Location = new Point(87, 163);
            btnConectar.Name = "btnConectar";
            btnConectar.Size = new Size(75, 23);
            btnConectar.TabIndex = 4;
            btnConectar.Text = "Conectar";
            btnConectar.UseVisualStyleBackColor = true;
            btnConectar.Click += btnConectar_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(503, 331);
            Controls.Add(btnConectar);
            Controls.Add(tbxIdParceiro);
            Controls.Add(lbIdParceiro);
            Controls.Add(lbMeuID);
            Controls.Add(lbDescMeuID);
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbDescMeuID;
        private Label lbMeuID;
        private Label lbIdParceiro;
        private TextBox tbxIdParceiro;
        private Button btnConectar;
    }
}
