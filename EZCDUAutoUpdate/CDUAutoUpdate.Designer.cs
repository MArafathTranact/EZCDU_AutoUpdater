namespace EZCDUAutoUpdate
{
    partial class CDUAutoUpdate
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CDUAutoUpdate));
            lblVersion = new Label();
            lblService = new Label();
            lblStatus = new Label();
            txtServiceDownloadURL = new TextBox();
            txtServiceInstallPath = new TextBox();
            btnSaveConfiguration = new Button();
            btnCancel = new Button();
            btnFolderSelect = new Button();
            txtUserName = new TextBox();
            lblUsername = new Label();
            txtPassword = new TextBox();
            lblPassword = new Label();
            btnShowPassword = new Button();
            toolTipShowPassword = new ToolTip(components);
            toolTipInstallPath = new ToolTip(components);
            toolTipSaveconfiguration = new ToolTip(components);
            toolTipCancelApplication = new ToolTip(components);
            SuspendLayout();
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.BackColor = Color.Transparent;
            lblVersion.Font = new Font("Verdana", 9F, FontStyle.Bold);
            lblVersion.ForeColor = SystemColors.HighlightText;
            lblVersion.Location = new Point(12, 68);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(158, 14);
            lblVersion.TabIndex = 0;
            lblVersion.Text = "Service Download URL";
            lblVersion.Visible = false;
            // 
            // lblService
            // 
            lblService.AutoSize = true;
            lblService.BackColor = Color.Transparent;
            lblService.Font = new Font("Verdana", 9F, FontStyle.Bold);
            lblService.ForeColor = SystemColors.HighlightText;
            lblService.Location = new Point(14, 193);
            lblService.Name = "lblService";
            lblService.Size = new Size(137, 14);
            lblService.TabIndex = 1;
            lblService.Text = "Service Install Path";
            lblService.Visible = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Verdana", 9F, FontStyle.Bold);
            lblStatus.ForeColor = SystemColors.HighlightText;
            lblStatus.Location = new Point(28, 90);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 14);
            lblStatus.TabIndex = 2;
            // 
            // txtServiceDownloadURL
            // 
            txtServiceDownloadURL.Font = new Font("Verdana", 9F);
            txtServiceDownloadURL.Location = new Point(179, 66);
            txtServiceDownloadURL.Name = "txtServiceDownloadURL";
            txtServiceDownloadURL.Size = new Size(152, 22);
            txtServiceDownloadURL.TabIndex = 3;
            txtServiceDownloadURL.Visible = false;
            // 
            // txtServiceInstallPath
            // 
            txtServiceInstallPath.Font = new Font("Verdana", 9F);
            txtServiceInstallPath.Location = new Point(179, 190);
            txtServiceInstallPath.Name = "txtServiceInstallPath";
            txtServiceInstallPath.Size = new Size(152, 22);
            txtServiceInstallPath.TabIndex = 4;
            txtServiceInstallPath.Visible = false;
            // 
            // btnSaveConfiguration
            // 
            btnSaveConfiguration.Cursor = Cursors.Hand;
            btnSaveConfiguration.Font = new Font("Verdana", 9F);
            btnSaveConfiguration.Location = new Point(100, 240);
            btnSaveConfiguration.Name = "btnSaveConfiguration";
            btnSaveConfiguration.Size = new Size(76, 30);
            btnSaveConfiguration.TabIndex = 5;
            btnSaveConfiguration.Text = "Save";
            toolTipSaveconfiguration.SetToolTip(btnSaveConfiguration, "Save Configuration");
            btnSaveConfiguration.UseVisualStyleBackColor = true;
            btnSaveConfiguration.Visible = false;
            btnSaveConfiguration.Click += btnSaveConfiguration_Click;
            // 
            // btnCancel
            // 
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Font = new Font("Verdana", 9F);
            btnCancel.Location = new Point(190, 240);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(76, 30);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            toolTipCancelApplication.SetToolTip(btnCancel, "Close Application");
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Visible = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnFolderSelect
            // 
            btnFolderSelect.BackgroundImage = (Image)resources.GetObject("btnFolderSelect.BackgroundImage");
            btnFolderSelect.BackgroundImageLayout = ImageLayout.Zoom;
            btnFolderSelect.Cursor = Cursors.Hand;
            btnFolderSelect.Font = new Font("Verdana", 9F);
            btnFolderSelect.Location = new Point(335, 191);
            btnFolderSelect.Name = "btnFolderSelect";
            btnFolderSelect.Size = new Size(26, 23);
            btnFolderSelect.TabIndex = 7;
            toolTipInstallPath.SetToolTip(btnFolderSelect, "Select Service install path folder");
            btnFolderSelect.UseVisualStyleBackColor = true;
            btnFolderSelect.Visible = false;
            btnFolderSelect.Click += btnFolderSelect_Click;
            // 
            // txtUserName
            // 
            txtUserName.Font = new Font("Verdana", 9F);
            txtUserName.Location = new Point(179, 105);
            txtUserName.Name = "txtUserName";
            txtUserName.Size = new Size(152, 22);
            txtUserName.TabIndex = 9;
            txtUserName.Visible = false;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.BackColor = Color.Transparent;
            lblUsername.Font = new Font("Verdana", 9F, FontStyle.Bold);
            lblUsername.ForeColor = SystemColors.HighlightText;
            lblUsername.Location = new Point(14, 109);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(80, 14);
            lblUsername.TabIndex = 8;
            lblUsername.Text = "User Name";
            lblUsername.Visible = false;
            // 
            // txtPassword
            // 
            txtPassword.Font = new Font("Verdana", 9F);
            txtPassword.Location = new Point(179, 148);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(152, 22);
            txtPassword.TabIndex = 11;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Visible = false;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.BackColor = Color.Transparent;
            lblPassword.Font = new Font("Verdana", 9F, FontStyle.Bold);
            lblPassword.ForeColor = SystemColors.HighlightText;
            lblPassword.Location = new Point(14, 151);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(72, 14);
            lblPassword.TabIndex = 10;
            lblPassword.Text = "Password";
            lblPassword.Visible = false;
            // 
            // btnShowPassword
            // 
            btnShowPassword.BackgroundImage = (Image)resources.GetObject("btnShowPassword.BackgroundImage");
            btnShowPassword.BackgroundImageLayout = ImageLayout.Zoom;
            btnShowPassword.Cursor = Cursors.Hand;
            btnShowPassword.Font = new Font("Verdana", 9F);
            btnShowPassword.Location = new Point(335, 148);
            btnShowPassword.Name = "btnShowPassword";
            btnShowPassword.Size = new Size(26, 23);
            btnShowPassword.TabIndex = 12;
            toolTipShowPassword.SetToolTip(btnShowPassword, "Show Password");
            btnShowPassword.UseVisualStyleBackColor = true;
            btnShowPassword.Visible = false;
            btnShowPassword.Click += btnShowPassword_Click;
            // 
            // CDUAutoUpdate
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.Transact_BG;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(374, 281);
            Controls.Add(btnShowPassword);
            Controls.Add(txtPassword);
            Controls.Add(lblPassword);
            Controls.Add(txtUserName);
            Controls.Add(lblUsername);
            Controls.Add(btnFolderSelect);
            Controls.Add(btnCancel);
            Controls.Add(btnSaveConfiguration);
            Controls.Add(txtServiceInstallPath);
            Controls.Add(txtServiceDownloadURL);
            Controls.Add(lblStatus);
            Controls.Add(lblService);
            Controls.Add(lblVersion);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "CDUAutoUpdate";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CDU Auto Update";
            Load += CDUAutoUpdate_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblVersion;
        private Label lblService;
        private Label lblStatus;
        private TextBox txtServiceDownloadURL;
        private TextBox txtServiceInstallPath;
        private Button btnSaveConfiguration;
        private Button btnCancel;
        private Button btnFolderSelect;
        private TextBox txtUserName;
        private Label lblUsername;
        private TextBox txtPassword;
        private Label lblPassword;
        private Button btnShowPassword;
        private ToolTip toolTipShowPassword;
        private ToolTip toolTipInstallPath;
        private ToolTip toolTipSaveconfiguration;
        private ToolTip toolTipCancelApplication;
    }
}