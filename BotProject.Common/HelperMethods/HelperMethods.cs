using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BotProject.Common
{
    public static class HelperMethods
    {
        public static string ToUnsignString(string input)
        {
            input = input.Trim();
            for (int i = 0x20; i < 0x30; i++)
            {
                input = input.Replace(((char)i).ToString(), " ");
            }
            input = input.Replace(".", "-");
            input = input.Replace(" ", "-");
            input = input.Replace(",", "-");
            input = input.Replace(";", "-");
            input = input.Replace(":", "-");
            input = input.Replace("  ", "-");
            Regex regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
            string str = input.Normalize(NormalizationForm.FormD);
            string str2 = regex.Replace(str, string.Empty).Replace('đ', 'd').Replace('Đ', 'D');
            while (str2.IndexOf("?") >= 0)
            {
                str2 = str2.Remove(str2.IndexOf("?"), 1);
            }
            while (str2.Contains("--"))
            {
                str2 = str2.Replace("--", "-").ToLower();
            }
            return str2;
        }

        public static string HighLightWord(string strString, string strKeyWord, bool bNosign)
        {
            if (string.IsNullOrEmpty(strString) || string.IsNullOrEmpty(strKeyWord))
                return strString;

            string strStringLower = strString.ToLower();
            string strWord = strKeyWord;
            if (!string.IsNullOrEmpty(strWord))
            {
                int iStartAt = 0, iFound = -1;
                ArrayList arrOpenTag = new ArrayList();
                while (iStartAt < strStringLower.Length)
                {
                    if ((iFound = strStringLower.IndexOf(strWord, iStartAt)) < 0)
                        break;
                    arrOpenTag.Add(iFound);
                    iStartAt = iFound + 1;
                }

                for (int i = arrOpenTag.Count - 1; i >= 0; i--)
                {
                    int iOpenTag = (int)arrOpenTag[i];
                    if (iOpenTag > 0)
                    {
                        char chPre = strStringLower[iOpenTag - 1];
                        if (chPre != ' ' && chPre != ',' && chPre != '.' && chPre != ';' && chPre != ':' && chPre != '!' && chPre != '?' && chPre != '\'' && chPre != '"' && chPre != '(' && chPre != ')' && chPre != '|' && chPre != '\r' && chPre != '\n')
                            continue;
                    }
                    int iCloseTag = iOpenTag + strWord.Length;
                    if (iCloseTag < strStringLower.Length)
                    {
                        char chNext = strStringLower[iCloseTag];
                        if (chNext != ' ' && chNext != ',' && chNext != '.' && chNext != ';' && chNext != ':' && chNext != '!' && chNext != '?' && chNext != '\'' && chNext != '"' && chNext != '(' && chNext != ')' && chNext != '|' && chNext != '\r' && chNext != '\n')
                            continue;
                    }

                    strStringLower = strStringLower.Insert(iCloseTag, "</b>");
                    strStringLower = strStringLower.Insert(iOpenTag, "<b>");

                    strString = strString.Insert(iCloseTag, "</b>");
                    strString = strString.Insert(iOpenTag, "<b>");
                }
            }
            return strString;
        }

        /// <summary>
        /// Validate string containt SQL-Injection
        /// </summary>
        /// <param name="strIn"></param>
        /// <returns></returns>
        static string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.@-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters,
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Replace HTML template with values
        /// </summary>
        /// <param name="template">Template content HTML</param>
        /// <param name="replacements">Dictionary with key/value</param>
        /// <returns></returns>
        public static string Parse(this string template, Dictionary<string, string> replacements)
        {
            if (replacements.Count > 0)
            {
                template = replacements.Keys
                            .Aggregate(template, (current, key) => current.Replace(key, replacements[key]));
            }
            return template;
        }

        private static int[] Map_VNOrigin = {194,226,258,259,202,234,212,244,431,432,416,417,272,
                                273,7840,7841,7842,7843,7844,7845,7846,7847,7848,7849,7850,7851,
                                7852,7853,7854,7855,7856,7857,7858,7859,7860,7861,7862,7863,7864,
                                7865,7866,7867,7868,7869,7870,7871,7872,7873,7874,7875,7876,7877,
                                7878,7879,7880,7881,7882,7883,7884,7885,7886,7887,7888,7889,7890,
                                7891,7892,7893,7894,7895,7896,7897,7898,7899,7900,7901,7902,7903,
                                7904,7905,7906,7907,7908,7909,7910,7911,7912,7913,7914,7915,7916,
                                7917,7918,7919,7920,7921,7922,7923,7924,7925,7926,7927,7928,7929,
                                192,193,195,200,201,204,205,210,211,213,217,218,221,224,225,227,
                                232,233,236,237,242,243,245,249,250,253,360,361,296,297};

        private static int[] Map_VN1258 = {194,226,258,259,202,234,212,244,431,432,416,417,272,273,
                                65,803,97,803,65,777,97,777,194,769,226,769,194,768,226,768,194,777,
                                226,777,194,771,226,771,194,803,226,803,258,769,259,769,258,768,259,
                                768,258,777,259,777,258,771,259,771,258,803,259,803,69,803,101,803,
                                69,777,101,777,69,771,101,771,202,769,234,769,202,768,234,768,202,
                                777,234,777,202,771,234,771,202,803,234,803,73,777,105,777,73,803,
                                105,803,79,803,111,803,79,777,111,777,212,769,244,769,212,768,244,
                                768,212,777,244,777,212,771,244,771,212,803,244,803,416,769,417,769,
                                416,768,417,768,416,777,417,777,416,771,417,771,416,803,417,803,85,
                                803,117,803,85,777,117,777,431,769,432,769,431,768,432,768,431,777,
                                432,777,431,771,432,771,431,803,432,803,89,768,121,768,89,803,121,
                                803,89,777,121,777,89,771,121,771,65,768,65,769,65,771,69,768,69,
                                769,73,768,73,769,79,768,79,769,79,771,85,768,85,769,89,769,97,768,
                                97,769,97,771,101,768,101,769,105,768,105,769,111,768,111,769,111,
                                771,117,768,117,769,121,769,85,771,117,771,73,771,105,771};
        /// <summary>
        /// chuyen doi chuoi Unicode to hop sang chuoi Unicode dung san
        /// </summary>
        /// <param name="strUnicode">chuoi Unicode to hop</param>
        /// <returns>chuoi Unicode dung san</returns>
        //public static string UnicodeVN1258ToUnicodeOrigin(string strUnicode)
        public static string UnicodeVN1258ToUnicodeOrigin(object stringUnicode)
        {
            //if (strUnicode == null)
            if (stringUnicode == null)
                return null;
            StringBuilder strOriginDest = new StringBuilder();
            int i = 0;
            //int iLenOrigin = 134;
            int iLen1258 = 254;

            //string stTest0_14 = tu 0 den 14 cua Map_VN1258;
            //string stMapVN1258 = tu 14 den het cua Map_VN1258;
            string stMapVN1258 = "";
            for (i = 0; i < iLen1258; i++)
                stMapVN1258 += (char)Map_VN1258[i];
            string stMapVN1258_a = stMapVN1258.Substring(0, 14);

            string strUnicode = (string)stringUnicode;
            i = 0;
            while (i < strUnicode.Length)
            {
                if (strUnicode[i] == 9)
                {
                    strOriginDest.Append("\t");
                    i++;
                    continue;
                }
                if (strUnicode[i] < 'A')
                {
                    strOriginDest.Append(strUnicode[i]);
                    i++;
                    continue;
                }
                if (strUnicode[i] > 'Z' && strUnicode[i] < 'a')
                {
                    strOriginDest.Append(strUnicode[i]);
                    i++;
                    continue;
                }
                if (strUnicode[i] >= 'A' && strUnicode[i] <= 'Z' && strUnicode[i] != 'A' && strUnicode[i] != 'E' && strUnicode[i] != 'I' && strUnicode[i] != 'O' && strUnicode[i] != 'U' && strUnicode[i] != 'Y')
                {
                    strOriginDest.Append(strUnicode[i]);
                    i++;
                    continue;
                }
                if (strUnicode[i] >= 'a' && strUnicode[i] <= 'z' && strUnicode[i] != 'a' && strUnicode[i] != 'e' && strUnicode[i] != 'i' && strUnicode[i] != 'o' && strUnicode[i] != 'u' && strUnicode[i] != 'y')
                {
                    strOriginDest.Append(strUnicode[i]);
                    i++;
                    continue;
                }
                if (i + 1 < strUnicode.Length)
                {
                    string stFind = strUnicode[i].ToString() + strUnicode[i + 1];
                    int k = stMapVN1258.IndexOf(stFind, 14);
                    if (k != -1)
                    {
                        strOriginDest.Append((char)Map_VNOrigin[14 + (k - 14) / 2]);
                        i += 2;
                    }
                    else
                    {
                        stFind = strUnicode[i].ToString();
                        k = stMapVN1258_a.IndexOf(stFind);
                        if (k != -1)
                        {
                            strOriginDest.Append((char)Map_VNOrigin[k]);
                        }
                        else strOriginDest.Append(strUnicode[i]);
                        i++;
                    }
                }
                else
                {
                    string stFind = strUnicode[i].ToString();
                    int k = stMapVN1258_a.IndexOf(stFind);
                    if (k != -1)
                    {
                        strOriginDest.Append((char)Map_VNOrigin[k]);
                    }
                    else strOriginDest.Append(strUnicode[i]);
                    i++;
                }
            }
            return strOriginDest.ToString();
        }


        /// <summary>
        /// Check extension of file image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string ImageType(this Image image)
		{
			if (image.RawFormat.Equals(ImageFormat.Bmp))
			{
				return "Bmp";
			}
			else if (image.RawFormat.Equals(ImageFormat.MemoryBmp))
			{
				return "BMP";
			}
			else if (image.RawFormat.Equals(ImageFormat.Wmf))
			{
				return "Emf";
			}
			else if (image.RawFormat.Equals(ImageFormat.Wmf))
			{
				return "Wmf";
			}
			else if (image.RawFormat.Equals(ImageFormat.Gif))
			{
				return ".gif";
			}
			else if (image.RawFormat.Equals(ImageFormat.Jpeg))
			{
				return ".jpg";
			}
			else if (image.RawFormat.Equals(ImageFormat.Png))
			{
				return ".png";
			}
			else if (image.RawFormat.Equals(ImageFormat.Tiff))
			{
				return "Tiff";
			}
			else if (image.RawFormat.Equals(ImageFormat.Exif))
			{
				return "Exif";
			}
			else if (image.RawFormat.Equals(ImageFormat.Icon))
			{
				return ".ico";
			}

			return ".jpg";
		}

	    public static void SaveImageByFormat(Image image,string filePath)
        {
            if (image.RawFormat.Equals(ImageFormat.Png))
            {
                image.Save(filePath, ImageFormat.Png);
            }
            else if (image.RawFormat.Equals(ImageFormat.Jpeg))
            {
                image.Save(filePath, ImageFormat.Jpeg);
            }
            else if (image.RawFormat.Equals(ImageFormat.Gif))
            {
                image.Save(filePath, ImageFormat.Gif);
            }
            else
            {
                image.Save(filePath, ImageFormat.Jpeg);
            }
        }

        public static string EscapeXml(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                return text.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;");
            }
            return text;
        }

        public static string EscapeXmlOutTag(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                return text.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;");
            }
            return text;
        }

        public const int OnWeekDay = 1;
        public const int OffWeekDay = 0;
        /// <summary>
        /// Thời gian làm việc trả lời khách hàng
        /// T2-CN : 8h00 - 12h00, 13h00 - 17h30
        /// </summary>
        /// <returns></returns>
        public static bool IsTimeInWorks()
        {
            bool rs = false;
            int T7CN = Int32.Parse(ConfigHelper.ReadString("T7CN"));
            DateTime timeCurrent = DateTime.Now;
            if(T7CN == OnWeekDay)
            {
                if ((timeCurrent.DayOfWeek == DayOfWeek.Saturday) || (timeCurrent.DayOfWeek == DayOfWeek.Sunday))
                {
                    rs = false;
                    return rs;
                }
            }
            if ((timeCurrent.Hour >= 8 && timeCurrent.Hour < 12))
            {
                rs = true;
            }
            else if (timeCurrent.Hour >= 13 && (timeCurrent.TimeOfDay < System.TimeSpan.Parse("17:30:00")))
            {
                rs = true;
            }
            else
            {
                rs = false;
            }

            return rs;
        }
	}
}
