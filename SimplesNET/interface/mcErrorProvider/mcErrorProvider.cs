using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SimplesNet.Helper;

namespace SimplesNet
{
	public class mcErrorProvider
	{
		Dictionary<IWin32Window,ToolTip> toolTips = new Dictionary<IWin32Window,ToolTip>();

		public mcErrorProvider()
		{
			MouseHook.MouseAction += Event;
			KeyBoardHook.KeyBoardAction += Event;
		}

		public void Clear()
		{
			Event(null, null);
		}
		
		public void SetError(IWin32Window control, string text, string title = "Error")
		{
			if(control != null)
			{
				ToolTip toolTipShow = null;
				if (string.IsNullOrEmpty(text))
				{
					if (toolTips.ContainsKey(control))
					{
						toolTips[control].Dispose();
						toolTips.Remove(control);
					}
				}
				else if (toolTips.ContainsKey(control))
				{
					toolTipShow = toolTips[control];
				}
				else
				{
					if (toolTips.Count == 0)
					{
						MouseHook.Start();
						KeyBoardHook.Start();
					}
					toolTipShow = new ToolTip() { IsBalloon = true, ToolTipIcon = ToolTipIcon.Warning, ToolTipTitle = title, ShowAlways = true };
					toolTips.Add(control, toolTipShow);
				}
				if (toolTipShow != null)
				{
					var width = 0;
					var height = 0;
					var controlUI = Control.FromHandle(control.Handle);
					if (controlUI != null)
					{
						if (!string.IsNullOrEmpty(controlUI.Text))
						{
							var textWidth = TextRenderer.MeasureText(controlUI.Text, controlUI.Font).Width;
							width = textWidth;
						}
						if (width > controlUI.Width)
							width = controlUI.Width;
						height = controlUI.Height / 2;
					}
					toolTipShow.Show(text, control, width, height, 5000);
					toolTipShow.Show(text, control, width, height, 5000);
				}
			}
			if (toolTips.Count == 0)
			{
				MouseHook.stop();
				KeyBoardHook.stop();
			}
		}

		private void Event(object sender, EventArgs e) 
		{
			foreach (var tt in toolTips)
			{
				if (tt.Value.Active)
					tt.Value.Hide(tt.Key);
				tt.Value.Dispose();
			}
			toolTips.Clear();
			MouseHook.stop();
			KeyBoardHook.stop();	
		}
	}
}
