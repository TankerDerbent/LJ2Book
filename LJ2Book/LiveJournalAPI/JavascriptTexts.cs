using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LJ2Book.LiveJournalAPI
{
	public static class JavascriptTexts
	{
		public const string ExtractArticleTitle = @"
(function() {{
var tittles = document.getElementsByClassName('aentry-post__title-text');
if (tittles.length < 1)
	tittles = document.getElementsByClassName('entry-title');
if (tittles.length > 0) 
	return tittles[0].innerHTML;
else
	return ''
 }} )();
";
		public const string ExtractArticleBody = @"
(function() {{
var articles = document.getElementsByClassName('aentry-post__text');
if (articles.length < 1)
	articles = document.getElementsByClassName('b-singlepost-body');
if (articles.length < 1)
	return ''
var theResultText = '<div class=""result-article"">' + articles[0].innerHTML; + '</div>';
document.body.innerHTML = theResultText;
var ljsales = document.getElementsByClassName('ljsale');
for (var i = 0; i < ljsales.length; i++)
	ljsales[i].parentNode.removeChild(ljsales[i]);
var ljlikes = document.getElementsByClassName('lj-like');
for (var j = 0; j < ljlikes.length; j++)
	ljlikes[j].parentNode.removeChild(ljlikes[j]);
return document.getElementsByClassName('result-article')[0].innerHTML;
 }} )();
";
		public const string ExtractImageList = @"
(function() {{
var imgs = document.getElementsByTagName('img');
var sImgs = ' ';
for (var i = 0; i < imgs.length; i++)
	sImgs += (imgs[i].src + ' ');
return sImgs;}} )();
";
		public const string ScrollNextA = @"
function ScrollNextA() {
	var nPageYOffset = window.pageYOffset;
	var labels = document.getElementsByTagName('a');
	var len = labels.length;
	for(var i = 1; i < len; i++)
	{
		var labelOffset = labels[i].offsetTop;
		if (labelOffset > nPageYOffset)
		{
			window.scrollTo(0, labelOffset);
			break;
		}
	}
}
";
		public const string ScrollPrevA = @"
function ScrollPrevA() {
	var nPageYOffset = window.pageYOffset;
	var labels = document.getElementsByTagName('a');
	var len = labels.length;
	for(var i = len - 1; i >= 0; i--)
	{
		var labelOffset = labels[i].offsetTop;
		if (labelOffset < nPageYOffset)
		{
			window.scrollTo(0, labelOffset);
			break;
		}
	}
}
";

	}
}
