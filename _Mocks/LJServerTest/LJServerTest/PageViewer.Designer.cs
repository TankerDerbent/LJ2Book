namespace LJServerTest
{
	partial class PageViewer
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
			this.webBrowser = new System.Windows.Forms.WebBrowser ();
			this.close = new System.Windows.Forms.Button ();
			this.SuspendLayout ();
			// 
			// webBrowser
			// 
			this.webBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.webBrowser.Location = new System.Drawing.Point (12, 12);
			this.webBrowser.MinimumSize = new System.Drawing.Size (20, 20);
			this.webBrowser.Name = "webBrowser";
			this.webBrowser.Size = new System.Drawing.Size (851, 522);
			this.webBrowser.TabIndex = 0;
			// 
			// close
			// 
			this.close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.close.Location = new System.Drawing.Point (788, 572);
			this.close.Name = "close";
			this.close.Size = new System.Drawing.Size (75, 23);
			this.close.TabIndex = 1;
			this.close.Text = "Закрыть";
			this.close.UseVisualStyleBackColor = true;
			// 
			// PageViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.close;
			this.ClientSize = new System.Drawing.Size (875, 607);
			this.Controls.Add (this.close);
			this.Controls.Add (this.webBrowser);
			this.Name = "PageViewer";
			this.ShowInTaskbar = false;
			this.Text = "Просмотр страницы";
			this.ResumeLayout (false);

		}

		#endregion

		private System.Windows.Forms.WebBrowser webBrowser;
		private System.Windows.Forms.Button close;
	}
}