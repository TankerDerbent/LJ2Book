using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimplesNet
{
	namespace DPIAware
	{
		internal class ImageNameBuilder
		{
			DpiHelper dpiHelper;

			private ImageNameBuilder() { }

			public ImageNameBuilder(DpiHelper _helper)
			{
				this.dpiHelper = _helper;
			}

			public string this[string sPictureName]
			{
				get
				{
					return @"Images/" + sPictureName + "-" + dpiHelper.ScaleX.ToString() + ".png";
				}
			}
		}
	}
}
