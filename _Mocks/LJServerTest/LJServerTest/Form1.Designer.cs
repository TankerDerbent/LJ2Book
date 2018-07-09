namespace LJServerTest
{
	partial class mainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
            this.useProxy = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.proxyUrlText = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.proxyPortText = new System.Windows.Forms.TextBox();
            this.loginLbl = new System.Windows.Forms.Label();
            this.loginText = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.passwordText = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.protocol = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.loginCookies = new System.Windows.Forms.Button();
            this.postChallengeBtn = new System.Windows.Forms.Button();
            this.loginChallenge = new System.Windows.Forms.Button();
            this.loginClearMD5 = new System.Windows.Forms.Button();
            this.loginClear = new System.Windows.Forms.Button();
            this.privateBtn = new System.Windows.Forms.Button();
            this.logText = new System.Windows.Forms.TextBox();
            this.clear = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.urlText = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // useProxy
            // 
            this.useProxy.AutoSize = true;
            this.useProxy.Location = new System.Drawing.Point(12, 12);
            this.useProxy.Name = "useProxy";
            this.useProxy.Size = new System.Drawing.Size(138, 17);
            this.useProxy.TabIndex = 0;
            this.useProxy.Text = "Использовать прокси";
            this.useProxy.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Адрес";
            // 
            // proxyUrlText
            // 
            this.proxyUrlText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.proxyUrlText.Location = new System.Drawing.Point(149, 35);
            this.proxyUrlText.Name = "proxyUrlText";
            this.proxyUrlText.Size = new System.Drawing.Size(427, 20);
            this.proxyUrlText.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(32, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Порт";
            // 
            // proxyPortText
            // 
            this.proxyPortText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.proxyPortText.Location = new System.Drawing.Point(334, 61);
            this.proxyPortText.Name = "proxyPortText";
            this.proxyPortText.Size = new System.Drawing.Size(242, 20);
            this.proxyPortText.TabIndex = 2;
            // 
            // loginLbl
            // 
            this.loginLbl.AutoSize = true;
            this.loginLbl.Location = new System.Drawing.Point(12, 100);
            this.loginLbl.Name = "loginLbl";
            this.loginLbl.Size = new System.Drawing.Size(38, 13);
            this.loginLbl.TabIndex = 1;
            this.loginLbl.Text = "Логин";
            // 
            // loginText
            // 
            this.loginText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.loginText.Location = new System.Drawing.Point(334, 97);
            this.loginText.Name = "loginText";
            this.loginText.Size = new System.Drawing.Size(242, 20);
            this.loginText.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 126);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Пароль";
            // 
            // passwordText
            // 
            this.passwordText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.passwordText.Location = new System.Drawing.Point(334, 123);
            this.passwordText.Name = "passwordText";
            this.passwordText.Size = new System.Drawing.Size(242, 20);
            this.passwordText.TabIndex = 4;
            this.passwordText.UseSystemPasswordChar = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 170);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Протокол";
            // 
            // protocol
            // 
            this.protocol.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.protocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.protocol.FormattingEnabled = true;
            this.protocol.Location = new System.Drawing.Point(334, 167);
            this.protocol.Name = "protocol";
            this.protocol.Size = new System.Drawing.Size(242, 21);
            this.protocol.TabIndex = 5;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.loginCookies);
            this.groupBox1.Controls.Add(this.postChallengeBtn);
            this.groupBox1.Controls.Add(this.loginChallenge);
            this.groupBox1.Controls.Add(this.loginClearMD5);
            this.groupBox1.Controls.Add(this.loginClear);
            this.groupBox1.Location = new System.Drawing.Point(12, 194);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(564, 91);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Действия";
            // 
            // loginCookies
            // 
            this.loginCookies.Location = new System.Drawing.Point(370, 19);
            this.loginCookies.Name = "loginCookies";
            this.loginCookies.Size = new System.Drawing.Size(185, 23);
            this.loginCookies.TabIndex = 8;
            this.loginCookies.Text = "Авторизоваться (Cookies)";
            this.loginCookies.UseVisualStyleBackColor = true;
            this.loginCookies.Click += new System.EventHandler(this.loginCookies_Click);
            // 
            // postChallengeBtn
            // 
            this.postChallengeBtn.Location = new System.Drawing.Point(176, 48);
            this.postChallengeBtn.Name = "postChallengeBtn";
            this.postChallengeBtn.Size = new System.Drawing.Size(188, 23);
            this.postChallengeBtn.TabIndex = 7;
            this.postChallengeBtn.Text = "Запостить (Challenge / Response)";
            this.postChallengeBtn.UseVisualStyleBackColor = true;
            this.postChallengeBtn.Click += new System.EventHandler(this.postBtn_Click);
            // 
            // loginChallenge
            // 
            this.loginChallenge.Location = new System.Drawing.Point(144, 19);
            this.loginChallenge.Name = "loginChallenge";
            this.loginChallenge.Size = new System.Drawing.Size(220, 23);
            this.loginChallenge.TabIndex = 6;
            this.loginChallenge.Text = "Авторизоваться (Challenge / Response)";
            this.loginChallenge.UseVisualStyleBackColor = true;
            this.loginChallenge.Click += new System.EventHandler(this.loginChallenge_Click);
            // 
            // loginClearMD5
            // 
            this.loginClearMD5.Location = new System.Drawing.Point(6, 48);
            this.loginClearMD5.Name = "loginClearMD5";
            this.loginClearMD5.Size = new System.Drawing.Size(164, 23);
            this.loginClearMD5.TabIndex = 6;
            this.loginClearMD5.Text = "Авторизоваться (Clear MD5)";
            this.loginClearMD5.UseVisualStyleBackColor = true;
            this.loginClearMD5.Click += new System.EventHandler(this.loginClearMD5_Click);
            // 
            // loginClear
            // 
            this.loginClear.Location = new System.Drawing.Point(6, 19);
            this.loginClear.Name = "loginClear";
            this.loginClear.Size = new System.Drawing.Size(132, 23);
            this.loginClear.TabIndex = 6;
            this.loginClear.Text = "Авторизоваться (Clear)";
            this.loginClear.UseVisualStyleBackColor = true;
            this.loginClear.Click += new System.EventHandler(this.login_Click);
            // 
            // privateBtn
            // 
            this.privateBtn.Location = new System.Drawing.Point(6, 52);
            this.privateBtn.Name = "privateBtn";
            this.privateBtn.Size = new System.Drawing.Size(185, 23);
            this.privateBtn.TabIndex = 9;
            this.privateBtn.Text = "Загрузить страницу";
            this.privateBtn.UseVisualStyleBackColor = true;
            this.privateBtn.Click += new System.EventHandler(this.privateBtn_Click);
            // 
            // logText
            // 
            this.logText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logText.Location = new System.Drawing.Point(12, 378);
            this.logText.Multiline = true;
            this.logText.Name = "logText";
            this.logText.ReadOnly = true;
            this.logText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.logText.Size = new System.Drawing.Size(564, 232);
            this.logText.TabIndex = 6;
            // 
            // clear
            // 
            this.clear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.clear.Location = new System.Drawing.Point(501, 620);
            this.clear.Name = "clear";
            this.clear.Size = new System.Drawing.Size(75, 23);
            this.clear.TabIndex = 7;
            this.clear.Text = "Очистить";
            this.clear.UseVisualStyleBackColor = true;
            this.clear.Click += new System.EventHandler(this.clear_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.urlText);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.privateBtn);
            this.groupBox2.Location = new System.Drawing.Point(12, 291);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(564, 81);
            this.groupBox2.TabIndex = 8;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Чтение подзамочных записей";
            // 
            // urlText
            // 
            this.urlText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.urlText.Location = new System.Drawing.Point(137, 24);
            this.urlText.Name = "urlText";
            this.urlText.Size = new System.Drawing.Size(410, 20);
            this.urlText.TabIndex = 11;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 27);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Адрес";
            // 
            // mainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(588, 655);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.clear);
            this.Controls.Add(this.logText);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.protocol);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.passwordText);
            this.Controls.Add(this.proxyPortText);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.loginText);
            this.Controls.Add(this.loginLbl);
            this.Controls.Add(this.proxyUrlText);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.useProxy);
            this.Name = "mainForm";
            this.Text = "LJServer";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox useProxy;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox proxyUrlText;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox proxyPortText;
		private System.Windows.Forms.Label loginLbl;
		private System.Windows.Forms.TextBox loginText;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox passwordText;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox protocol;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button loginClear;
		private System.Windows.Forms.Button loginChallenge;
		private System.Windows.Forms.Button postChallengeBtn;
		private System.Windows.Forms.Button loginCookies;
		private System.Windows.Forms.TextBox logText;
		private System.Windows.Forms.Button loginClearMD5;
		private System.Windows.Forms.Button clear;
		private System.Windows.Forms.Button privateBtn;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox urlText;
	}
}

