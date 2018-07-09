using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LJ2Book.DataBase
{
    public class Picture
    {
		[Key]
		public string Url { get; set; }
		public byte[] Data { get; set; }
    }
}
