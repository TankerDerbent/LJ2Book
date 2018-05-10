
using System.Windows.Forms;


namespace SimplesNet
{
	namespace AdjustableHeightControls
	{
		public sealed class TextBoxAdjustableHeight : TextBox
		{
			public TextBoxAdjustableHeight() { this.AutoSize = false; }

			private void InitializeComponent()
			{
				this.SuspendLayout();
				this.ResumeLayout(false);
			}
		}

		public sealed class MaskedTextBoxAdjustableHeight : MaskedTextBox
		{
			public MaskedTextBoxAdjustableHeight() { this.AutoSize = false; }

			private void InitializeComponent()
			{
				this.SuspendLayout();
				this.ResumeLayout(false);
			}
		}
	}
}
