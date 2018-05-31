using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace LJ2Book.DataBase
{
	public class Param
	{
		[Key]
		public string ParamName { get; set; }
		public string ParamValue { get; set; }
		public static string GetParam(string _Key, SiteContext _context)
		{
			var paramRemember = from p in _context.Params where p.ParamName == _Key select p.ParamValue;
			string sValue = paramRemember.FirstOrDefault();
			return sValue;
		}
		public static bool GetParamBool(string _Key, SiteContext _context)
		{
			string sValue = GetParam(_Key, _context);
			return sValue.ToLower() == "true";
		}
		public static int GetParamInt(string _Key, SiteContext _context)
		{
			string sValue = GetParam(_Key, _context);
			return Convert.ToInt32(sValue);
		}
		public static void SetParam(string _Key, string _Value, SiteContext _context)
		{
			Param param = _context.Params.Find(_Key);
			if (param == null)
			{
				param = new Param { ParamName = _Key, ParamValue = _Value };
				_context.Params.Add(param);
				_context.SaveChanges();
			}
			else
			{
				param.ParamValue = _Value;
				_context.Entry(param).State = System.Data.Entity.EntityState.Modified;
				_context.SaveChanges();
			}
		}
		public static void SetParam(string _Key, int _Value, SiteContext _context)
		{
			SetParam(_Key, _Value.ToString(), _context);
		}
		public static void SetParam(string _Key, bool _Value, SiteContext _context)
		{
			SetParam(_Key, _Value ? "true" : "false", _context);
		}
	}
}
