namespace IpcCircularScan
{
    partial class ShadingCorrectionDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.label_dialogdescription = new System.Windows.Forms.Label();
            this.buttonContinue = new System.Windows.Forms.Button();
            this.buttonCancelStatic = new System.Windows.Forms.Button();
            this.labelAction = new System.Windows.Forms.Label();
            this.buttonAcquire = new System.Windows.Forms.Button();
            this.buttonFinish = new System.Windows.Forms.Button();
            this.backgroundWorker_ShadingCorrection = new System.ComponentModel.BackgroundWorker();
            this.numericUpDown_NoImages = new System.Windows.Forms.NumericUpDown();
            this.buttonCancelInProcess = new System.Windows.Forms.Button();
            this.backgroundWorker_SwitchXrayOn = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker_ReturnManipulator = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_NoImages)).BeginInit();
            this.SuspendLayout();
            // 
            // label_dialogdescription
            // 
            this.label_dialogdescription.AutoSize = true;
            this.label_dialogdescription.Location = new System.Drawing.Point(128, 9);
            this.label_dialogdescription.Name = "label_dialogdescription";
            this.label_dialogdescription.Size = new System.Drawing.Size(272, 13);
            this.label_dialogdescription.TabIndex = 1;
            this.label_dialogdescription.Text = "This dialog will help acquire a manual shading correction";
            this.label_dialogdescription.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonContinue
            // 
            this.buttonContinue.Location = new System.Drawing.Point(415, 39);
            this.buttonContinue.Name = "buttonContinue";
            this.buttonContinue.Size = new System.Drawing.Size(96, 43);
            this.buttonContinue.TabIndex = 2;
            this.buttonContinue.Text = "Continue...";
            this.buttonContinue.UseVisualStyleBackColor = true;
            this.buttonContinue.Click += new System.EventHandler(this.buttonContinue_Click);
            // 
            // buttonCancelStatic
            // 
            this.buttonCancelStatic.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancelStatic.Location = new System.Drawing.Point(415, 99);
            this.buttonCancelStatic.Name = "buttonCancelStatic";
            this.buttonCancelStatic.Size = new System.Drawing.Size(96, 43);
            this.buttonCancelStatic.TabIndex = 3;
            this.buttonCancelStatic.Text = "Cancel";
            this.buttonCancelStatic.UseVisualStyleBackColor = true;
            this.buttonCancelStatic.Click += new System.EventHandler(this.buttonCancelStatic_Click);
            // 
            // labelAction
            // 
            this.labelAction.AutoSize = true;
            this.labelAction.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAction.Location = new System.Drawing.Point(80, 64);
            this.labelAction.Name = "labelAction";
            this.labelAction.Size = new System.Drawing.Size(249, 18);
            this.labelAction.TabIndex = 4;
            this.labelAction.Text = "Please move the sample out of view.";
            this.labelAction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonAcquire
            // 
            this.buttonAcquire.Enabled = false;
            this.buttonAcquire.Location = new System.Drawing.Point(415, 39);
            this.buttonAcquire.Name = "buttonAcquire";
            this.buttonAcquire.Size = new System.Drawing.Size(96, 43);
            this.buttonAcquire.TabIndex = 5;
            this.buttonAcquire.Text = "Acquire";
            this.buttonAcquire.UseVisualStyleBackColor = true;
            this.buttonAcquire.Visible = false;
            this.buttonAcquire.Click += new System.EventHandler(this.buttonAcquire_Click);
            // 
            // buttonFinish
            // 
            this.buttonFinish.Enabled = false;
            this.buttonFinish.Location = new System.Drawing.Point(415, 39);
            this.buttonFinish.Name = "buttonFinish";
            this.buttonFinish.Size = new System.Drawing.Size(96, 103);
            this.buttonFinish.TabIndex = 6;
            this.buttonFinish.Text = "Finish";
            this.buttonFinish.UseVisualStyleBackColor = true;
            this.buttonFinish.Visible = false;
            this.buttonFinish.Click += new System.EventHandler(this.buttonFinish_Click);
            // 
            // backgroundWorker_ShadingCorrection
            // 
            this.backgroundWorker_ShadingCorrection.WorkerReportsProgress = true;
            this.backgroundWorker_ShadingCorrection.WorkerSupportsCancellation = true;
            this.backgroundWorker_ShadingCorrection.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_ShadingCorrection_DoWork);
            this.backgroundWorker_ShadingCorrection.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_ShadingCorrection_RunWorkerCompleted);
            // 
            // numericUpDown_NoImages
            // 
            this.numericUpDown_NoImages.Enabled = false;
            this.numericUpDown_NoImages.Location = new System.Drawing.Point(150, 99);
            this.numericUpDown_NoImages.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.numericUpDown_NoImages.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_NoImages.Name = "numericUpDown_NoImages";
            this.numericUpDown_NoImages.Size = new System.Drawing.Size(120, 20);
            this.numericUpDown_NoImages.TabIndex = 7;
            this.numericUpDown_NoImages.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_NoImages.Visible = false;
            this.numericUpDown_NoImages.ValueChanged += new System.EventHandler(this.numericUpDown_NoImages_ValueChanged);
            // 
            // buttonCancelInProcess
            // 
            this.buttonCancelInProcess.Enabled = false;
            this.buttonCancelInProcess.Location = new System.Drawing.Point(415, 99);
            this.buttonCancelInProcess.Name = "buttonCancelInProcess";
            this.buttonCancelInProcess.Size = new System.Drawing.Size(96, 43);
            this.buttonCancelInProcess.TabIndex = 8;
            this.buttonCancelInProcess.Text = "Cancel";
            this.buttonCancelInProcess.UseVisualStyleBackColor = true;
            this.buttonCancelInProcess.Visible = false;
            this.buttonCancelInProcess.Click += new System.EventHandler(this.buttonCancelInProcess_Click);
            // 
            // backgroundWorker_SwitchXrayOn
            // 
            this.backgroundWorker_SwitchXrayOn.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_SwitchXrayOn_DoWork);
            // 
            // backgroundWorker_ReturnManipulator
            // 
            this.backgroundWorker_ReturnManipulator.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_ReturnManipulator_DoWork);
            this.backgroundWorker_ReturnManipulator.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_ReturnManipulator_RunWorkerCompleted);
            // 
            // ShadingCorrectionDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(538, 171);
            this.ControlBox = false;
            this.Controls.Add(this.buttonCancelInProcess);
            this.Controls.Add(this.numericUpDown_NoImages);
            this.Controls.Add(this.labelAction);
            this.Controls.Add(this.buttonCancelStatic);
            this.Controls.Add(this.buttonContinue);
            this.Controls.Add(this.label_dialogdescription);
            this.Controls.Add(this.buttonFinish);
            this.Controls.Add(this.buttonAcquire);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShadingCorrectionDialog";
            this.ShowIcon = false;
            this.Text = "Acquire a shading correction";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ShadingCorrectionDialog_FormClosing);
            this.Load += new System.EventHandler(this.ShadingCorrectionDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_NoImages)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_dialogdescription;
        private System.Windows.Forms.Button buttonContinue;
        private System.Windows.Forms.Button buttonCancelStatic;
        private System.Windows.Forms.Label labelAction;
        private System.Windows.Forms.Button buttonAcquire;
        private System.Windows.Forms.Button buttonFinish;
        private System.ComponentModel.BackgroundWorker backgroundWorker_ShadingCorrection;
        private System.Windows.Forms.NumericUpDown numericUpDown_NoImages;
        private System.Windows.Forms.Button buttonCancelInProcess;
        private System.ComponentModel.BackgroundWorker backgroundWorker_SwitchXrayOn;
        private System.ComponentModel.BackgroundWorker backgroundWorker_ReturnManipulator;
    }
}