using System;
using System.Text;
using System.Linq;
using System.Globalization;

namespace SimplesNet
{
	static public class StringHelpers
	{
		static public string GetStringFormatForValues(double dMinValue, double dMaxValue)
		{
			try
			{
				double dLog10 = Math.Log10(Math.Abs(dMaxValue - dMinValue));
				dLog10 = dLog10 < 0.0 ? -dLog10 : 0.0;
				StringBuilder sb = new StringBuilder();
				sb.Append('F');
				sb.Append(Convert.ToInt32(dLog10 + 0.5));
				return sb.ToString();
			}
			catch (Exception)
			{

				return "F";
			}
		}

		static public int GetCountFormatForValues(double dMinValue, double dMaxValue)
		{
			try
			{
				double dLog10 = Math.Log10(Math.Abs(dMaxValue - dMinValue));
				dLog10 = dLog10 < 0.0 ? -dLog10 : 0.0;
				return Convert.ToInt32(dLog10 + 0.5);
			}
			catch (Exception)
			{
				return 0;
			}
		}

		static public string GetStringFormatForValues(double[] values, int defCount = 2, int maxCount = 10, int minCount = 2, bool? isCurrency = true)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('F');
			if (values.Length > 0)
				sb.Append(values.Max(v => GetStringFormatDoubleCountForValue(v, defCount, maxCount, minCount, isCurrency)));
            return sb.ToString();
        }

		static public int GetOptimizeStringFormatDoubleCountForValues(double[] values, bool isCorrect = false, bool? isCurrency = true)
		{
			var count = values.Max(v => GetStringFormatDoubleCountForValue(v, 0, 10, 0, isCurrency));
			var countCorrect = 0;
			if (isCorrect)
			{
				if (count > 0)
				{
					bool isOneDisctinct = false;
					do
					{
						double d1 = 1D;
						double d2 = 1D;
						for (int i = 0; i < count; i++)
						{
							if (i < (count - countCorrect - 1))
								d1 *= 10D;
							else d2 *= 10D;
						}
						var tempValues = values.Select(v =>
						{
							double val1 = v * d1;
							val1 = val1 - Math.Floor(val1);
							val1 = val1 * d2;
							var result = Math.Round(val1);
							return result;
						}).ToArray();
						isOneDisctinct = tempValues.Distinct().Count() == 1;
						if (isOneDisctinct)
							countCorrect++;
					} while (isOneDisctinct && countCorrect < count);
				}
			}
			return count - countCorrect;
		}

        static public int GetStringFormatDoubleCountForValue(double value, int defCount = 2, int maxCount = 10, int minCount = 2, bool? isCurrency = true)
        {
            int countZnak = 0;
            try
            {

                bool isWrite = false;
                bool isNotZero = false;
                uint lV = 0;
                string valStr = value.ToString();
                if (!valStr.Contains("E"))
                {
                    foreach (var sV in valStr)
                    {
						if ((!isCurrency.HasValue || isCurrency.Value) && sV.ToString() == CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator || (!isCurrency.HasValue || !isCurrency.Value) && sV.ToString() == CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                        {
                            isWrite = true;
                            continue;
                        }
                        if (isWrite)
                        {
                            countZnak++;
                            var tV = uint.Parse(sV.ToString());
                            if (tV > 0)
                                isNotZero = true;
                            if (isNotZero)
                            {
                                if (lV == 9 && tV == lV ||
                                    lV == 0 && tV == 0)
                                    break;
                            }
                            lV = tV;
                        }
                    }
                    if (!isNotZero)
                        countZnak = 0;
                }
                else
                {

                    valStr = valStr.Substring(valStr.IndexOf('E') + 2);
                    int.TryParse(valStr, out countZnak);
                }
            }
            catch
            {
                countZnak = 0;
            }
            if (defCount < 0)
                return countZnak;
            else
                return countZnak > maxCount ? defCount : (countZnak >= minCount ? countZnak : minCount );
        }
	}
}