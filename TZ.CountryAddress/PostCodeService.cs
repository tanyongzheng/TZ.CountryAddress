using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TZ.CountryAddress
{
    public class PostCodeService
    {
        /// <summary>
        /// 获取邮编正则表达式
        /// </summary>
        /// <param name="countryCode">国家代码</param>
        /// <returns></returns>
        public (bool success, string msg,string regexStr) GetPostCodeRegex(string countryCode)
        {
            (bool success, string msg,string regexStr) result = new ValueTuple<bool, string,string>();
            if (string.IsNullOrEmpty(countryCode))
            {
                result.msg = "国家代码不能为空！";
                return result;
            }
            var rule= GetPostCodeRule(countryCode.ToUpper());
            if (rule == null)
            {
                result.msg = $"没找到国家{countryCode}的邮编正则表达式！";
                return result;
            }
            if (string.IsNullOrEmpty(rule.RegexStr))
            {
                result.msg = $"国家{countryCode}" + (string.IsNullOrEmpty(rule.Description) ? "没有邮编" : rule.Description);
                return result;
            }
            result.regexStr = rule.RegexStr;
            result.msg = rule.Description;
            result.success = true;
            return result;
        }

        /// <summary>
        /// 检测邮编格式
        /// </summary>
        /// <param name="countryCode">国家代码</param>
        /// <param name="postCode">邮编</param>
        /// <returns></returns>
        public (bool success, string msg) CheckPostCodeFormat(string countryCode,string postCode)
        {
            (bool success, string msg) result = new ValueTuple<bool, string>();
            var rule= GetPostCodeRule(countryCode);
            if (rule==null)
            {
                result.msg = "没匹配到对应国家的邮编规则！";
                return result;
            }
            result = CheckPostCodeFormatByRule(rule, postCode);
            return result;
        }

        /// <summary>
        /// 检测邮编是否在范围内
        /// </summary>
        /// <param name="countryCode">国家代码</param>
        /// <param name="startPostCode">起始邮编</param>
        /// <param name="endPostCode">结束邮编</param>
        /// <param name="postCode">邮编</param>
        /// <param name="isFixFormat">是否修复邮编格式</param>
        /// <returns></returns>
        public (bool success,string msg) CheckPostCodeInRange(string countryCode,string startPostCode,string endPostCode,string postCode,bool isFixFormat=true)
        {
            (bool success, string msg) result=new ValueTuple<bool,string>();
            var rule= GetPostCodeRule(countryCode);
            if (rule == null)
            {
                result.msg = "没有找到该国家的邮编正则规则";
                return result;
            }
            // 检测范围是否正确
           var rangeCheckResult=  CheckPostCodeRangeByRule(rule, startPostCode, endPostCode, isFixFormat);
           if (!rangeCheckResult.success)
           {
               result.msg = rangeCheckResult.msg;
               return result;
           }

            //format postal code
            if (rule.RangeIsNumber&&
                !string.IsNullOrEmpty(rule.LeftPaddingChar)&&
                rule.LeftPaddingChar.Length==1)
            {
                if (postCode.Length < rule.MinLenght)
                {
                    postCode = postCode.PadLeft(rule.MinLenght.Value,rule.LeftPaddingChar[0]);
                }
            }

            if (isFixFormat&& rule.FixFormatFunc != null)
            {
                var fixResult1 = rule.FixFormatFunc(postCode);
                if (fixResult1.success)
                {
                    postCode = fixResult1.formatPostCode;
                }
            }

            var postCodeMatch = Regex.Match(postCode, rule.RangeRegexStr);
            if (!postCodeMatch.Success)
            {
                result.msg = "检测邮编正则规则不匹配，" + rule.Description;
                return result;
            }

            if (rule.RangeIsNumber)
            {
                if (!int.TryParse(startPostCode, out var startNumber))
                {
                    result.msg = "起始邮编范围转换为数字失败！";
                    return result;
                }
                if (!int.TryParse(endPostCode, out var endNumber))
                {
                    result.msg = "结束邮编范围转换为数字失败！";
                    return result;
                }

                if (!int.TryParse(postCodeMatch.Value, out var postCodeNumber))
                {
                    result.msg = "验证邮编转换为数字失败！";
                    return result;
                }

                if (postCodeNumber < startNumber)
                {
                    result.msg = "小于起始邮编！";
                    return result;
                }
                if (postCodeNumber > endNumber)
                {
                    result.msg = "大于结束邮编！";
                    return result;
                }

                result.success = true;
                result.msg = "在范围内！";
                return result;
            }
            /*
            if (startPostCodeMatch.Value.Equals(postCode,StringComparison.OrdinalIgnoreCase))
            {
                result.success = true;
                result.msg = "在范围内！";
                return result;
            }
            if (endPostCodeMatch.Value.Equals(postCode,StringComparison.OrdinalIgnoreCase))
            {
                result.success = true;
                result.msg = "在范围内！";
                return result;
            }
            
            if (String.Compare(postCode,startPostCode,StringComparison.OrdinalIgnoreCase)==1&&
                String.Compare(endPostCode,postCode,StringComparison.OrdinalIgnoreCase)==1)
            {
                result.success = true;
                result.msg = "在范围内！";
                return result;
            }
            */

            if (string.Compare(postCode,startPostCode,StringComparison.OrdinalIgnoreCase)==-1||
                string.Compare(endPostCode,postCode,StringComparison.OrdinalIgnoreCase)==-1)
            {
                result.msg = "不在范围内！";
                return result;
            }

            result.success = true;
            result.msg = "在范围内";
            return result;
        }

        
        public (bool success, string msg) CheckRangeOverlap(string countryCode,List<PostCodeRangeModel> postCodeRangeList)
        {
            (bool success, string msg) result = new ValueTuple<bool, string>();
            var rule = GetPostCodeRule(countryCode);
            if (rule == null)
            {
                result.msg = "没匹配到对应国家的邮编规则！";
                return result;
            }
            
            for (var i=0;i< postCodeRangeList.Count;i++)
            {
                var rangeItem = postCodeRangeList[i];
                var currentResult = CheckRangeOverlap(rule, rangeItem, postCodeRangeList, new[] {i});
                if (!currentResult.success)
                {
                    result.msg = currentResult.msg;
                    return result;
                }
            }

            result.success = true;
            result.msg = "范围列表没有重叠的邮编范围";
            return result;
        }

        #region private method
        /// <summary>
        /// 获取国家的邮编规则
        /// </summary>
        /// <param name="countryCode">国家代码，两位字母全大写</param>
        /// <returns></returns>
        private PostCodeValidationRuleModel GetPostCodeRule(string countryCode)
        {
            var list = new List<PostCodeValidationRuleModel>();
            #region 部分国家邮编规则验证

            #region 北美洲
            if (countryCode == "US")//美国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "US";
                model.CountryCnName = "美国";
                model.RegexStr = "(^[0-9]{5}$)|(^[0-9]{5}-[0-9]{4}$)";
                model.Description = "5数字，如：12345；或5数字+短横线+4数字，如：12345-1234";
                model.Format = "(NNNNN)|(NNNNN-NNNNN)";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CA")//加拿大
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CA";
                model.CountryCnName = "加拿大";
                //model.RegexStr = "^[a-zA-Z]{1}[0-9]{1}[a-zA-Z]{1} [0-9]{1}[a-zA-Z]{1}[0-9]{1}$";
                model.RegexStr = "^(?=[^DdFfIiOoQqUu\\d\\s])[A-Za-z]\\d(?=[^DdFfIiOoQqUu\\d\\s])[A-Za-z]\\s{0,1}\\d(?=[^DdFfIiOoQqUu\\d\\s])[A-Za-z]\\d$";
                model.Description = "字母+数字+字母+空格+数字+字母+数字，不包含字母 D, F, I, O, Q, U，如：a1B 2C3";
                model.Format = "ANA NAN";
                model.RangeIsNumber = false;
                //model.RangeRegexStr = "^[a-zA-Z]{1}[0-9]{1}[a-zA-Z]{1} [0-9]{1}[a-zA-Z]{1}[0-9]{1}$";
                model.RangeRegexStr = "^(?=[^DdFfIiOoQqUu\\d\\s])[A-Za-z]\\d(?=[^DdFfIiOoQqUu\\d\\s])[A-Za-z]\\s{0,1}\\d(?=[^DdFfIiOoQqUu\\d\\s])[A-Za-z]\\d$";
                model.MinLenght = 6;
                model.LeftPaddingChar = null;
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MX")//
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MX";
                model.CountryCnName = "墨西哥";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "GT")//危地马拉
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GT";
                model.CountryCnName = "危地马拉";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "BZ")//伯利兹
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BZ";
                model.CountryCnName = "伯利兹";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "SV")//萨尔瓦多
            {
                //DHL邮编库没有邮编
                //订单有3-5位数字的邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SV";
                model.CountryCnName = "萨尔瓦多";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "HN")//洪都拉斯
            {
                //DHL邮编库没有邮编
                //订单有3-5位数字的邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "HN";
                model.CountryCnName = "洪都拉斯";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PA")//巴拿马
            {
                //DHL邮编库没有邮编
                //订单有3-4位数字的邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PA";
                model.CountryCnName = "巴拿马";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BS")//巴哈马
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BS";
                model.CountryCnName = "巴哈马";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "CU")//古巴
            {

                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CU";
                model.CountryCnName = "古巴";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "JM")//牙买加
            {
                //DHL邮编库没有邮编
                //订单有3-4位数字及多个字母的邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "JM";
                model.CountryCnName = "牙买加";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "HT")//海地
            {
                //DHL邮编库没有邮编
                //订单有4位数字的邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "HT";
                model.CountryCnName = "海地";
                model.RegexStr = "^(HT){0,2}[0-9]{4}$";
                model.Description = "HT+4位数字 如HT1234,或4位数字，如：1234";
                model.Format = "(HT)NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "DO")//多米尼加共和国
            {
                //DHL邮编库没有邮编
                //订单有5-6位数字的邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "DO";
                model.CountryCnName = "多米尼加共和国";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CR")//哥斯达黎加
            {
                //DHL邮编库没有邮编
                //订单有5位数字的邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CR";
                model.CountryCnName = "哥斯达黎加";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "KN")//圣基茨和尼维斯
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KN";
                model.CountryCnName = "圣基茨和尼维斯";
                model.RegexStr = "^KN[0-9]{4}$";
                model.Description = "KN+4位数字 如KN1234";
                model.Format = "HTNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AG")//安提瓜和巴布达
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AG";
                model.CountryCnName = "安提瓜和巴布达";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "DM")//多米尼克
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "DM";
                model.CountryCnName = "多米尼克";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "LC")//圣卢西亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LC";
                model.CountryCnName = "圣卢西亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "VC")//圣文森特和格林纳丁斯 ,简称圣文森
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "VC";
                model.CountryCnName = "圣文森特和格林纳丁斯";
                model.RegexStr = "^VC[0-9]{4}$";
                model.Description = "VC+4位数字 如VC1234";
                model.Format = "VCNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BB")//巴巴多斯
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BB";
                model.CountryCnName = "巴巴多斯";
                model.RegexStr = "^BB[0-9]{5}$";
                model.Description = "BB+4位数字 如BB12345";
                model.Format = "BBNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "GD")//格林纳达
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GD";
                model.CountryCnName = "格林纳达";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "TT")//特立尼达和多巴哥
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TT";
                model.CountryCnName = "特立尼达和多巴哥";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "NI")//尼加拉瓜
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NI";
                model.CountryCnName = "尼加拉瓜";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 南美洲
            else if (countryCode == "EC")//厄瓜多尔
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "EC";
                model.CountryCnName = "厄瓜多尔";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CO")//哥伦比亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CO";
                model.CountryCnName = "哥伦比亚";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "VE")//委内瑞拉
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "VE";
                model.CountryCnName = "委内瑞拉";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PE")//秘鲁
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PE";
                model.CountryCnName = "秘鲁";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BR")//巴西
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BR";
                model.CountryCnName = "巴西";
                model.RegexStr = "^[0-9]{5}-[0-9]{3}$";
                model.Description = "5位数字+短横线+3位数字，如：12345-123";
                model.Format = "";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CL")//智利
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CL";
                model.CountryCnName = "智利";
                model.RegexStr = "^[0-9]{7}$";
                model.Description = "7位数字，如：1234567";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{7}$";
                model.MinLenght = 7;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "UY")//乌拉圭
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "UY";
                model.CountryCnName = "乌拉圭";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PY")//巴拉圭
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PY";
                model.CountryCnName = "巴拉圭";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AR")//阿根廷
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AR";
                model.CountryCnName = "阿根廷";
                model.RegexStr = "^[A-Za-z]{1}[0-9]{4}[A-Za-z]{3}$";
                model.Description = "1位字母+4位数字+3位字母，如：B1636FDA";
                model.Format = "ANNNNAAA";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^[A-Za-z]{1}[0-9]{4}[A-Za-z]{3}$";
                model.MinLenght = 8;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
                /*
                Regex regex1 = new Regex("^[0-9]{4}$");//5位数字，如：1234
                var match1 = regex1.Match(postCode);
                if (!match1.Success)
                {
                    result = false;
                    message += "[" + countryCode + "邮编规则不对！4位数字，如：1234]";
                }
                */
            }
            else if (countryCode == "BO")//玻利维亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BO";
                model.CountryCnName = "玻利维亚";
                model.RegexStr = "";
                model.Description = "没有邮编，遇到必填的可以填4个0";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "GY")//圭亚那
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GY";
                model.CountryCnName = "圭亚那";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "SR")//苏里南
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SR";
                model.CountryCnName = "苏里南";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "GF")//法属圭亚那
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GF";
                model.CountryCnName = "法属圭亚那";
                model.RegexStr = "^973[0-9]{2}$";
                model.Description = "5位数字,973开头，如：97300";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^973[0-9]{2}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "FK")//福克兰群岛
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "FK";
                model.CountryCnName = "福克兰群岛";
                model.RegexStr = "^[Ff][Ii][Qq]{2}[ ]{0,1}1[Zz]{2}$";
                model.Description = "固定一个邮编：FIQQ 1ZZ";
                model.Format = "FIQQ 1ZZ";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^[Ff][Ii][Qq]{2}[ ]{0,1}1[Zz]{2}$";
                model.MinLenght = 7;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            /*
            else if (countryCode == "GS")//南乔治亚岛和南桑威奇群岛
            {
                //DHL邮编库没有邮编
            }
            */
            #endregion

            #region 欧洲

            #region 北欧
            else if (countryCode == "SE")//瑞典
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SE";
                model.CountryCnName = "瑞典";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "NO")//挪威
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NO";
                model.CountryCnName = "挪威";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "FI")//芬兰
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "FI";
                model.CountryCnName = "芬兰";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "DK")//丹麦
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "DK";
                model.CountryCnName = "丹麦";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "IS")//冰岛
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "";
                model.CountryCnName = "";
                model.RegexStr = "^[0-9]{3}$";
                model.Description = "3位数字，如：123";
                model.Format = "NNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{3}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 南欧
            else if (countryCode == "IT")//意大利
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "IT";
                model.CountryCnName = "意大利";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "ES")//西班牙
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ES";
                model.CountryCnName = "西班牙";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PT")//葡萄牙
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PT";
                model.CountryCnName = "葡萄牙";
                model.RegexStr = "^[0-9]{4}(-[0-9]{3}|[0-9]{3})?$";
                model.Description = "4数字或7位数字或4数字+短横线+3位数字，如：1234，1234567，1234-123";
                model.Format = "NNNN-NNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AD")//安道尔
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AD";
                model.CountryCnName = "安道尔";
                model.RegexStr = "^[aA][Dd][0-9]{3}$";
                model.Description = "AD+3位数字，如：AD600";
                model.Format = "ADNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "[0-9]{3}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "VA")//梵蒂冈
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "VA";
                model.CountryCnName = "梵蒂冈";
                model.RegexStr = "^00120$";
                model.Description = "固定为：00120";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^00120$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SM")//圣马力诺
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SM";
                model.CountryCnName = "圣马力诺";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MT")//马耳他
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "";
                model.CountryCnName = "";
                model.RegexStr = "^[A-Za-z]{3} [0-9]{4}$";
                model.Description = "3位字母+空格+4位数字，如：MLH 1234";
                model.Format = "AAA NNNN";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^[A-Za-z]{3} [0-9]{4}$";
                model.MinLenght = 8;
                model.LeftPaddingChar = "";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SI")//斯洛文尼亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SI";
                model.CountryCnName = "斯洛文尼亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "HR")//克罗地亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "HR";
                model.CountryCnName = "克罗地亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BA")//波黑
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BA";
                model.CountryCnName = "波黑";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "RS")//塞尔维亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "RS";
                model.CountryCnName = "塞尔维亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "ME")//黑山共和国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ME";
                model.CountryCnName = "黑山共和国";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AL")//阿尔巴尼亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AL";
                model.CountryCnName = "阿尔巴尼亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MK")//马其顿
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MK";
                model.CountryCnName = "马其顿";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BG")//保加利亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BG";
                model.CountryCnName = "保加利亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "GR")//希腊
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GR";
                model.CountryCnName = "希腊";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "RO")//罗马尼亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "RO";
                model.CountryCnName = "罗马尼亚";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 西欧
            else if (countryCode == "GB")//英国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GB";
                model.CountryCnName = "英国";
                model.RegexStr = "^[a-zA-Z0-9]{2,4} [a-zA-Z0-9]{3}$";
                model.Description = "2-4位数字或字母+空格+3位数字或字母，如：1a2c 33d";
                model.Format = "[AN]{2,4} [AN]{3}";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^[a-zA-Z0-9]{2,4} [a-zA-Z0-9]{3}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "";
                model.NoPostCode = false;
                model.FixFormatFunc = (postCode) =>
                {
                    (bool success, string formatPostCode, string msg) result = new ValueTuple<bool, string, string>();
                    if (string.IsNullOrEmpty(postCode))
                    {
                        result.msg = "邮编不能为空！";
                        return result;
                    }
                    if (postCode.Length<5)
                    {
                        result.msg = model.CountryCode+"邮编不能小于5位！";
                        return result;
                    }
                    postCode = postCode.Replace("  ", " ").Replace(" ", "");
                    result.formatPostCode = postCode.Substring(0, postCode.Length - 3) + " " + postCode.Substring(postCode.Length - 3, 3);
                    result.success = true;
                    return result;
                };
                list.Add(model);
            }
            else if (countryCode == "IE")//爱尔兰
            {
                //目前DHL邮编库中爱尔兰邮编为空
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "IE";
                model.CountryCnName = "爱尔兰";
                model.RegexStr = "(?:^[AC-FHKNPRTV-Y][0-9]{2}|D6W)[ -]?[0-9AC-FHKNPRTV-Y]{4}$";
                model.Description = "1个字母+2个数字（D6W除外）+ 4个字母或数字，如：A65 F4E2";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 8;
                model.LeftPaddingChar = "";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "NL")//荷兰
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NL";
                model.CountryCnName = "荷兰";
                model.RegexStr = "^[0-9]{4}[ ]{0,1}[a-zA-Z]{2}$";
                model.Description = "4数字+2位字母，如：12345AB";
                model.Format = "NNNNAA";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            } //4位数字
            else if (countryCode == "BE")//比利时
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BE";
                model.CountryCnName = "比利时";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            } //4位数字
            else if (countryCode == "LU")//卢森堡
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LU";
                model.CountryCnName = "卢森堡";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "FR")//法国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "FR";
                model.CountryCnName = "法国";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MC")//摩纳哥
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MC";
                model.CountryCnName = "摩纳哥";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 中欧
            else if (countryCode == "DE")//德国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "DE";
                model.CountryCnName = "德国";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PL")//波兰
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PL";
                model.CountryCnName = "波兰";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CZ")//捷克
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "";
                model.CountryCnName = "";
                model.RegexStr = "^[1234567][0-9]{4}$";
                model.Description = "第一个字符为1至7的数字，后4位为数字，如：11234";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[1234567][0-9]{4}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SK")//斯洛伐克
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "";
                model.CountryCnName = "";
                model.RegexStr = "^[089]{1}[0-9]{4}$";
                model.Description = "第一位数字位0或8或9+4数字如：01234";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[089]{1}[0-9]{4}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "HU")//匈牙利
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "HU";
                model.CountryCnName = "匈牙利";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AT")//奥地利
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AT";
                model.CountryCnName = "奥地利";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LI")//列支敦士登
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LI";
                model.CountryCnName = "列支敦士登";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CH")//瑞士
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CH";
                model.CountryCnName = "瑞士";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 东欧
            else if (countryCode == "EE")//爱沙尼亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "EE";
                model.CountryCnName = "爱沙尼亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LV")//拉脱维亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LV";
                model.CountryCnName = "拉脱维亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LT")//立陶宛
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LT";
                model.CountryCnName = "立陶宛";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BY")//白俄罗斯
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BY";
                model.CountryCnName = "白俄罗斯";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "RU")//俄罗斯
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "RU";
                model.CountryCnName = "俄罗斯";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "UA")//乌克兰
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "UA";
                model.CountryCnName = "乌克兰";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion
            #endregion

            #region 大洋洲
            else if (countryCode == "AU")//澳大利亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AU";
                model.CountryCnName = "澳大利亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);

            }
            else if (countryCode == "PW")//帕劳
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PW";
                model.CountryCnName = "帕劳";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "FM")//密克罗尼西亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "FM";
                model.CountryCnName = "密克罗尼西亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MH")//马绍尔群岛
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MH";
                model.CountryCnName = "马绍尔群岛";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "NR")//瑙鲁
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NR";
                model.CountryCnName = "瑙鲁";
                model.RegexStr = "^[Nn]{1}[Rr]{1}[Uu]{1}68$";
                model.Description = "固定邮编NRU68";
                model.Format = "NRU68";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^[Nn]{1}[Rr]{1}[Uu]{1}68$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "PG")//巴布亚新几内亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PG";
                model.CountryCnName = "巴布亚新几内亚";
                model.RegexStr = "^[0-9]{3}$";
                model.Description = "3位数字，如：123";
                model.Format = "NNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{3}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SB")//所罗门群岛
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SB";
                model.CountryCnName = "所罗门群岛";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "VU")//瓦努阿图
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "VU";
                model.CountryCnName = "瓦努阿图";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "TV")//图瓦卢
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TV";
                model.CountryCnName = "图瓦卢";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "FJ")//斐济
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "FJ";
                model.CountryCnName = "斐济";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "WS")//萨摩亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "WS";
                model.CountryCnName = "萨摩亚";
                model.RegexStr = "^[wW]{1}[sS]{1}[0-9]{4}$";
                model.Description = "WS+4位数字，如：WS1251";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "KI")//基里巴斯
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KI";
                model.CountryCnName = "基里巴斯";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "TO")//汤加
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TO";
                model.CountryCnName = "汤加";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "NZ")//新西兰
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NZ";
                model.CountryCnName = "新西兰";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 亚洲

            #region 东亚
            else if (countryCode == "CN")//中国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CN";
                model.CountryCnName = "中国";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "JP")//日本
            {
                //E邮宝规则：三位数字+破折号+四位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "JP";
                model.CountryCnName = "日本";
                model.RegexStr = "^[0-9]{7}$";
                model.Description = "7位数字，如：1234567";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{7}$";
                model.MinLenght = 7;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "KR")//韩国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KR";
                model.CountryCnName = "韩国";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "KP")//朝鲜
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KP";
                model.CountryCnName = "朝鲜";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "MN")//蒙古
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MN";
                model.CountryCnName = "蒙古";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 东南亚
            else if (countryCode == "VN")//越南
            {
                //越南信息传媒部于2017年3月29日颁布决定：国家邮政编码共有5位数
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "VN";
                model.CountryCnName = "越南";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LA")//老挝
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LA";
                model.CountryCnName = "老挝";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "KH")//柬埔寨
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KH";
                model.CountryCnName = "柬埔寨";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "TH")//泰国
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TH";
                model.CountryCnName = "泰国";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MM")//缅甸
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MM";
                model.CountryCnName = "缅甸";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MY")//马来西亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MY";
                model.CountryCnName = "马来西亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SG")//新加坡
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SG";
                model.CountryCnName = "新加坡";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "ID")//印度尼西亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ID";
                model.CountryCnName = "印度尼西亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BN")//文莱
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BN";
                model.CountryCnName = "文莱";
                model.RegexStr = "^[A-Za-z]{2}[0-9]{4}$";
                model.Description = "2位字母+4位数字，如：BS8811";
                model.Format = "AANNNN";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^[A-Za-z]{2}[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PH")//菲律宾
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PH";
                model.CountryCnName = "菲律宾";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "TL")//东帝汶
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TL";
                model.CountryCnName = "东帝汶";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            #endregion

            #region 南亚
            else if (countryCode == "NP")//尼泊尔
            {
                //DHL邮编库没有邮编，根据E邮宝规则来
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NP";
                model.CountryCnName = "尼泊尔";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BT")//不丹
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BT";
                model.CountryCnName = "不丹";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "IN")//印度
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "IN";
                model.CountryCnName = "印度";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PK")//巴基斯坦
            {
                //DHL邮编库没有邮编，根据E邮宝规则来
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PK";
                model.CountryCnName = "巴基斯坦";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BD")//孟加拉
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BD";
                model.CountryCnName = "孟加拉";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LK")//斯里兰卡
            {
                //DHL邮编库没有邮编，根据E邮宝规则来
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LK";
                model.CountryCnName = "斯里兰卡";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MV")//马尔代夫
            {
                //DHL邮编库没有邮编，根据订单数据推测
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MV";
                model.CountryCnName = "马尔代夫";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 西亚
            else if (countryCode == "IR")//伊朗 
            {
                //DHL邮编库没有邮编
                //Regex regex1 = new Regex("^[0-9]{5}-[0-9]{5}$");//5位数字+5位数字12345-12345 E邮宝规则
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "IR";
                model.CountryCnName = "伊朗";
                model.RegexStr = "^[0-9]{10}$";
                model.Description = "10位数字，如：1234567890";
                model.Format = "NNNNNNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{10}$";
                model.MinLenght = 10;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "IQ")//伊拉克
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "IQ";
                model.CountryCnName = "伊拉克";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AZ")//阿塞拜疆
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AZ";
                model.CountryCnName = "阿塞拜疆";
                model.RegexStr = "^(AZ)?[0-9]{4}$";
                model.Description = "4位数字或AZ+4位数字，如：1234，AZ1234";
                model.Format = "AZNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "GE")//格鲁吉亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GE";
                model.CountryCnName = "格鲁吉亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AM")//亚美尼亚
            {
                //新邮编为4位数字，旧邮编为6位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AM";
                model.CountryCnName = "亚美尼亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "TR")//土耳其
            {
                //DHL邮编库没有邮编，根据订单数据推测
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TR";
                model.CountryCnName = "土耳其";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SY")//叙利亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SY";
                model.CountryCnName = "叙利亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "JO")//约旦
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "JO";
                model.CountryCnName = "约旦";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "IL")//以色列 
            {
                //5数字，如：12345；
                //7数字，如：1234567；
                //5位数字+4位数字12345-1234
                //以色列邮政编码在2013年之前由5位数字组成，之后，以色列的邮编位数由5位改为7位
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "IL";
                model.CountryCnName = "以色列";
                model.RegexStr = "^[0-9]{7}$";
                model.Description = "7位数字，如：1234567";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{7}$";
                model.MinLenght = 7;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "PS")//巴勒斯坦
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "PS";
                model.CountryCnName = "巴勒斯坦";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "SA")//沙特阿拉伯
            {
                //
                //DHL邮编库没有邮编
                //E邮宝规则五位数字，或七位数字，或五位数字+短横线+四位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SA";
                model.CountryCnName = "沙特阿拉伯";
                model.RegexStr = "^[0-9]{5}(-[0-9]{4})?$";
                model.Description = "5位数字或5位数字+短横线+4位数字，如：12345，或12345-1234";
                model.Format = "NNNNN(-NNNN)";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "BH")//巴林
            {
                //DHL邮编库没有邮编
                //E邮宝规则3至4位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BH";
                model.CountryCnName = "巴林";
                model.RegexStr = "^[0-9]{3,4}$";
                model.Description = "3到4位数字，如：123或1234";
                model.Format = "NNN(N)";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{3,4}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "QA")//卡塔尔
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "QA";
                model.CountryCnName = "卡塔尔";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "YE")//也门
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "YE";
                model.CountryCnName = "也门";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "OM")//阿曼
            {
                //DHL邮编库没有邮编
                //E邮宝规则3位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "OM";
                model.CountryCnName = "阿曼";
                model.RegexStr = "^[0-9]{3}$";
                model.Description = "3位数字，如：123";
                model.Format = "NNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{3}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AE")//阿拉伯联合酋长国 阿联酋
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AE";
                model.CountryCnName = "阿联酋";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "KW")//科威特
            {
                //DHL邮编库没有邮编
                //E邮宝规则5位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KW";
                model.CountryCnName = "科威特";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LB")//黎巴嫩
            {
                //DHL邮编库没有邮编
                //E邮宝规则四位数字+空格+四位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LB";
                model.CountryCnName = "黎巴嫩";
                model.RegexStr = "^[0-9]{4}( [0-9]{4})?$";
                model.Description = "4位数字或4位数字+空格+4位数字，如：1234或1234 1234";
                model.Format = "NNNN( NNNN)";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CY")//塞浦路斯
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CY";
                model.CountryCnName = "塞浦路斯";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #region 中亚
            else if (countryCode == "TM")//土库曼斯坦
            {
                //DHL邮编库没有邮编
                //网上资料为6位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TM";
                model.CountryCnName = "土库曼斯坦";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "KG")//吉尔吉斯斯坦
            {
                //DHL邮编库没有邮编
                //网上资料为6位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KG";
                model.CountryCnName = "吉尔吉斯斯坦";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "UZ")//乌兹别克斯坦
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "UZ";
                model.CountryCnName = "乌兹别克斯坦";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "TJ")//塔吉克斯坦
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TJ";
                model.CountryCnName = "塔吉克斯坦";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "KZ")//哈萨克斯坦
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KZ";
                model.CountryCnName = "哈萨克斯坦";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "AF")//阿富汗
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AF";
                model.CountryCnName = "阿富汗";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }

            #endregion
            #endregion

            #region 非洲

            #region 北非
            else if (countryCode == "EG")//埃及
            {
                //DHL邮编库没有邮编
                //E邮宝规则为5位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "EG";
                model.CountryCnName = "埃及";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LY")//利比亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LY";
                model.CountryCnName = "利比亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "NT")//突尼斯
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NT";
                model.CountryCnName = "突尼斯";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "DZ")//阿尔及利亚
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "DZ";
                model.CountryCnName = "阿尔及利亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MA")//摩洛哥
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MA";
                model.CountryCnName = "摩洛哥";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SD")//苏丹
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SD";
                model.CountryCnName = "苏丹";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }

            #endregion

            #region 东非
            else if (countryCode == "ET")//埃塞俄比亚
            {
                //DHL邮编库没有邮编
                //E邮宝规则为4位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ET";
                model.CountryCnName = "埃塞俄比亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SS")//南苏丹
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SS";
                model.CountryCnName = "南苏丹";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "ER")//厄立特里亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ER";
                model.CountryCnName = "厄立特里亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "DJ")//吉布提
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "DJ";
                model.CountryCnName = "吉布提";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "SO")//索马里
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SO";
                model.CountryCnName = "索马里";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "KE")//肯尼亚
            {
                //DHL邮编库没有邮编
                //E邮宝规则为5位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KE";
                model.CountryCnName = "肯尼亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "UG")//乌干达
            {
                //DHL邮编库没有邮编
                //E邮宝规则为5位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "UG";
                model.CountryCnName = "乌干达";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "RW")//卢旺达
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "RW";
                model.CountryCnName = "卢旺达";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "BI")//布隆迪
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BI";
                model.CountryCnName = "布隆迪";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "TZ")//坦桑尼亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TZ";
                model.CountryCnName = "坦桑尼亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "SC")//塞舌尔
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SC";
                model.CountryCnName = "塞舌尔";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "MU")//毛里求斯
            {
                //DHL邮编库没有邮编
                //E邮宝规则为三位数字+两位字母+三位数字
                //youbianku资料：毛里求斯邮政编码由5位字符组成，有两个岛屿的邮编由字母+4位数字组成
                //英文维基百科位5位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MU";
                model.CountryCnName = "毛里求斯";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }

            #endregion

            #region 西非
            else if (countryCode == "EH")//西撒哈拉
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "EH";
                model.CountryCnName = "西撒哈拉";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "MR")//毛里塔尼亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MR";
                model.CountryCnName = "毛里塔尼亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "SN")//塞内加尔
            {
                //DHL邮编库没有邮编
                //E邮宝规则为5位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SN";
                model.CountryCnName = "塞内加尔";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "GM")//冈比亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GM";
                model.CountryCnName = "冈比亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "ML")//马里
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ML";
                model.CountryCnName = "马里";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "BF")//布基纳法索
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BF";
                model.CountryCnName = "布基纳法索";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "GN")//几内亚
            {
                //DHL邮编库没有邮编
                //E邮宝规则为三位数字+两位字母+三位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GN";
                model.CountryCnName = "几内亚";
                model.RegexStr = "^[0-9]{3}[ a-zA-Z0-9]{2}[0-9]{3}$";
                model.Description = "3位数字+2位字母+3位数字，如：123AB123";
                model.Format = "NNNAANNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{3}";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "GW")//几内亚比绍
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GW";
                model.CountryCnName = "几内亚比绍";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CV")//佛得角
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CV";
                model.CountryCnName = "佛得角";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SL")//塞拉利昂
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SL";
                model.CountryCnName = "塞拉利昂";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "LR")//利比里亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LR";
                model.CountryCnName = "利比里亚";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "CI")//科特迪瓦
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CI";
                model.CountryCnName = "科特迪瓦";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "TG")//多哥
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TG";
                model.CountryCnName = "多哥";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "BJ")//贝宁
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BJ";
                model.CountryCnName = "贝宁";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "NE")//尼日尔
            {
                //DHL邮编库没有邮编
                //E邮宝规则为4位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NE";
                model.CountryCnName = "尼日尔";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "NG")//尼日利亚
            {
                //DHL邮编库没有邮编
                //E邮宝规则为6位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NG";
                model.CountryCnName = "尼日利亚";
                model.RegexStr = "^[0-9]{6}$";
                model.Description = "6位数字，如：123456";
                model.Format = "NNNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{6}$";
                model.MinLenght = 6;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }

            #endregion

            #region 中非
            else if (countryCode == "CF")//中非共和国
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CF";
                model.CountryCnName = "中非共和国";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "TD")//乍得
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "TD";
                model.CountryCnName = "乍得";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "CM")//喀麦隆
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CM";
                model.CountryCnName = "喀麦隆";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "GQ")//赤道几内亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GQ";
                model.CountryCnName = "赤道几内亚";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "GA")//加蓬
            {
                //DHL邮编库没有邮编
                //E邮宝规则为二位数字+短横线+三位数字
                //英文维基百科没有邮编
                //youbianku写法：2个数字+城市+2个数字，参考：:https://www.youbianku.com/%E5%8A%A0%E8%93%AC
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GA";
                model.CountryCnName = "加蓬";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "CD")//刚果民主共和国/刚果(金)
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CD";
                model.CountryCnName = "刚果民主共和国";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "CG")//刚果（布）
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "CG";
                model.CountryCnName = "刚果（布）";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "ST")//圣多美和普林西比
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ST";
                model.CountryCnName = "圣多美和普林西比";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }

            #endregion

            #region 南非
            else if (countryCode == "AO")//安哥拉
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "AO";
                model.CountryCnName = "安哥拉";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "ZM")//赞比亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ZM";
                model.CountryCnName = "赞比亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MW")//马拉维
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MW";
                model.CountryCnName = "马拉维";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "MZ")//莫桑比克
            {
                //DHL邮编库没有邮编
                //E邮宝规则为4位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MZ";
                model.CountryCnName = "莫桑比克";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "ZW")//津巴布韦
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ZW";
                model.CountryCnName = "津巴布韦";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "BW")//博茨瓦纳
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "BW";
                model.CountryCnName = "博茨瓦纳";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            else if (countryCode == "NA")//纳米比亚
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "NA";
                model.CountryCnName = "纳米比亚";
                model.RegexStr = "^[0-9]{5}$";
                model.Description = "5位数字，如：12345";
                model.Format = "NNNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{5}$";
                model.MinLenght = 5;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "ZA")//南非
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "ZA";
                model.CountryCnName = "南非";
                model.RegexStr = "^[0-9]{4}$";
                model.Description = "4位数字，如：1234";
                model.Format = "NNNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{4}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "SZ")//斯威士兰
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "SZ";
                model.CountryCnName = "斯威士兰";
                model.RegexStr = "^[ a-zA-Z]{1}[0-9]{3}$";
                model.Description = "1位字母+3位数字，如：H123";
                model.Format = "ANNN";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^[ a-zA-Z]{1}[0-9]{3}$";
                model.MinLenght = 4;
                model.LeftPaddingChar = "";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "LS")//莱索托
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "LS";
                model.CountryCnName = "莱索托";
                model.RegexStr = "^[0-9]{3}$";
                model.Description = "3位数字，如：123";
                model.Format = "NNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{3}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "MG")//马达加斯加
            {
                //DHL邮编库没有邮编
                //E邮宝规则为3位数字
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "MG";
                model.CountryCnName = "马达加斯加";
                model.RegexStr = "^[0-9]{3}$";
                model.Description = "3位数字，如：123";
                model.Format = "NNN";
                model.RangeIsNumber = true;
                model.RangeRegexStr = "^[0-9]{3}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "0";
                model.NoPostCode = false;
                list.Add(model);
            }
            /*
            else if (countryCode == "MU")//毛里求斯
            {
                //DHL邮编库没有邮编
                //E邮宝规则为三位数字+两位字母+三位数字，如：123AB123
                //^[0-9]{3}[ a-zA-Z0-9]{2}[0-9]{3}$
            }
            */
            else if (countryCode == "KM")//科摩罗
            {
                //DHL邮编库没有邮编
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "KM";
                model.CountryCnName = "科摩罗";
                model.RegexStr = "";
                model.Description = "没有邮编";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "";
                model.MinLenght = 0;
                model.LeftPaddingChar = "";
                model.NoPostCode = true;
                list.Add(model);
            }
            #endregion

            #endregion

            #region DHL其他国家有字母的邮编（除欧洲及E邮宝已知的邮编规则）

            else if (countryCode == "GG")//根西岛，英国属地
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "GG";
                model.CountryCnName = "根西岛";
                model.RegexStr = "^GY[0-9]{1}[ a-zA-Z0-9]{0,4}$";
                model.Description = "GY+1位数字+0-4位数字或字母，如：GY12AU";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^GY[0-9]{1}[ a-zA-Z0-9]{0,4}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "";
                model.NoPostCode = false;
                list.Add(model);
            }
            else if (countryCode == "JE")//泽西岛，英国属地
            {
                var model = new PostCodeValidationRuleModel();
                model.CountryCode = "JE";
                model.CountryCnName = "泽西岛";
                model.RegexStr = "^JE[0-9]{1}[ a-zA-Z0-9]{0,4}$";
                model.Description = "JE+1位数字+0-4位数字或字母，如：JE3 7AE";
                model.Format = "";
                model.RangeIsNumber = false;
                model.RangeRegexStr = "^JE[0-9]{1}[ a-zA-Z0-9]{0,4}$";
                model.MinLenght = 3;
                model.LeftPaddingChar = "";
                model.NoPostCode = false;
                list.Add(model);
            }
            #endregion

            #endregion

            if (list.Count == 1)
            {
                return list[0];
            }
            return null;
        }


        /// <summary>
        /// 检测邮编格式
        /// </summary>
        /// <paramref name="rule">邮编规则</paramref>
        /// <param name="rule"></param>
        /// <param name="postCode">邮编</param>
        /// <returns></returns>
        private (bool success, string msg) CheckPostCodeFormatByRule(PostCodeValidationRuleModel rule, string postCode)
        {
            (bool success, string msg) result = new ValueTuple<bool, string>();
            if (rule==null)
            {
                result.msg = "邮编规则不能为空！";
                return result;
            }
            var regexStr = rule.RegexStr;
            if (string.IsNullOrEmpty(regexStr))
            {
                result.msg = "邮编规则正则表达式不能为空！";
                return result;
            }

            if (!Regex.IsMatch(postCode, regexStr))
            {
                result.msg = "不符合邮编规则：" + rule.Description;
                return result;
            }

            result.success = true;
            result.msg = "符合邮编规则";
            return result;
        }



        /// <summary>
        /// 检测邮编是否在范围内
        /// </summary>
        /// <param name="rule">邮编规则</param>
        /// <param name="startPostCode">起始邮编</param>
        /// <param name="endPostCode">结束邮编</param>
        /// <param name="isFixFormat">是否修复邮编格式</param>
        /// <returns></returns>
        private (bool success, string msg) CheckPostCodeRangeByRule(PostCodeValidationRuleModel rule, string startPostCode, string endPostCode, bool isFixFormat = true)
        {
            (bool success, string msg) result = new ValueTuple<bool, string>();
            if (rule == null)
            {
                result.msg = "邮编规则不能为空！";
                return result;
            }

            // format postal code
            if (rule.RangeIsNumber &&
                !string.IsNullOrEmpty(rule.LeftPaddingChar) &&
                rule.LeftPaddingChar.Length == 1)
            {
                if (startPostCode.Length < rule.MinLenght)
                {
                    startPostCode = startPostCode.PadLeft(rule.MinLenght.Value, rule.LeftPaddingChar[0]);
                }
                if (endPostCode.Length < rule.MinLenght)
                {
                    endPostCode = endPostCode.PadLeft(rule.MinLenght.Value, rule.LeftPaddingChar[0]);
                }
            }

            if (isFixFormat && rule.FixFormatFunc != null)
            {
                var fixResult2 = rule.FixFormatFunc(startPostCode);
                if (fixResult2.success)
                {
                    startPostCode = fixResult2.formatPostCode;
                }
                var fixResult3 = rule.FixFormatFunc(endPostCode);
                if (fixResult3.success)
                {
                    endPostCode = fixResult3.formatPostCode;
                }
            }
            var startPostCodeMatch = Regex.Match(startPostCode, rule.RangeRegexStr);
            if (!startPostCodeMatch.Success)
            {
                result.msg = "起始邮编范围正则规则不匹配，" + rule.Description;
                return result;
            }
            var endPostCodeMatch = Regex.Match(endPostCode, rule.RangeRegexStr);
            if (!endPostCodeMatch.Success)
            {
                result.msg = "结束邮编范围正则规则不匹配，" + rule.Description;
                return result;
            }

            // 邮编范围是数字
            if (rule.RangeIsNumber)
            {
                if (!int.TryParse(startPostCodeMatch.Value, out var startNumber))
                {
                    result.msg = "起始邮编范围转换为数字失败！";
                    return result;
                }
                if (!int.TryParse(endPostCodeMatch.Value, out var endNumber))
                {
                    result.msg = "结束邮编范围转换为数字失败！";
                    return result;
                }

                if (startNumber > endNumber)
                {
                    result.msg = "起始邮编不能大于结束邮编！";
                    return result;
                }

                result.success = true;
                result.msg = "邮编范围正确！";
                return result;
            }

            result.success = true;
            result.msg = "邮编范围正确！";
            return result;
        }


        private (bool success, string msg) CheckRangeOverlap(
            PostCodeValidationRuleModel rule,
            PostCodeRangeModel postCodeRange, 
            List<PostCodeRangeModel> postCodeRangeList,
            int[] excludeRangeIndexs=null
            )
        {
            (bool success, string msg) result = new ValueTuple<bool, string>();
            if (rule == null)
            {
                result.msg = "邮编规则不能为空！";
                return result;
            }
            {
                var checkRangeResult = CheckPostCodeRangeByRule(rule, postCodeRange.StartPostCode, postCodeRange.EndPostCode);
                if (!checkRangeResult.success)
                {
                    result.msg = $"邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]{checkRangeResult.msg}";
                    return result;
                }
            }
            for (var i = 0; i < postCodeRangeList.Count; i++)
            {
                if (excludeRangeIndexs != null && excludeRangeIndexs.Length > 0)
                {
                    if(excludeRangeIndexs.Contains(i)) continue;
                }
                var rangeItem = postCodeRangeList[i];
                var checkRangeResult = CheckPostCodeRangeByRule(rule, rangeItem.StartPostCode, rangeItem.EndPostCode);
                if (!checkRangeResult.success)
                {
                    result.msg = $"邮编范围[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]{checkRangeResult.msg}";
                    return result;
                }

                #region 数字范围的邮编
                if (rule.RangeIsNumber)
                {

                    #region 匹配范围正则表达式
                    var startPostCodeMatch = Regex.Match(postCodeRange.StartPostCode, rule.RangeRegexStr);
                    if (!startPostCodeMatch.Success)
                    {
                        result.msg = $"当前被验证邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]起始邮编范围正则规则不匹配，" + rule.Description;
                        return result;
                    }
                    var endPostCodeMatch = Regex.Match(postCodeRange.EndPostCode, rule.RangeRegexStr);
                    if (!endPostCodeMatch.Success)
                    {
                        result.msg = $"当前被验证邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]结束邮编范围正则规则不匹配，" + rule.Description;
                        return result;
                    }

                    if (!int.TryParse(startPostCodeMatch.Value, out var rangeStartNumber))
                    {
                        result.msg = $"当前被验证起始邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]转换为数字失败！";
                        return result;
                    }
                    if (!int.TryParse(endPostCodeMatch.Value, out var rangeEndNumber))
                    {
                        result.msg = $"当前被验证结束邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]转换为数字失败！";
                        return result;
                    }
                    #endregion

                    #region 数字范围的邮编
                    var minPostCodeMatch = Regex.Match(rangeItem.StartPostCode, rule.RangeRegexStr);
                    if (!minPostCodeMatch.Success)
                    {
                        result.msg = $"当前验证重叠邮编范围[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]开始邮编范围正则规则不匹配，" + rule.Description;
                        return result;
                    }

                    var maxPostCodeMatch = Regex.Match(rangeItem.EndPostCode, rule.RangeRegexStr);
                    if (!maxPostCodeMatch.Success)
                    {
                        result.msg = $"当前验证重叠邮编范围[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]结束邮编范围正则规则不匹配，" + rule.Description;
                        return result;
                    }

                    if (!int.TryParse(minPostCodeMatch.Value, out var minNumber))
                    {
                        result.msg = $"当前验证重叠邮编范围[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]开始邮编转换为数字失败！";
                        return result;
                    }
                    if (!int.TryParse(maxPostCodeMatch.Value, out var maxNumber))
                    {
                        result.msg = $"当前验证重叠邮编范围[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]结束邮编转换为数字失败！";
                        return result;
                    } 
                    #endregion

                    if (rangeStartNumber >= minNumber&&rangeStartNumber<=maxNumber)
                    {
                        result.msg = $"邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]与[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]有重叠！";
                        return result;
                    }

                    if (rangeEndNumber >= minNumber&&rangeEndNumber<=maxNumber)
                    {
                        result.msg = $"邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]与[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]有重叠！";
                        return result;
                    }
                }
                #endregion

                #region 非数字范围邮编
                else
                {
                    if (postCodeRange.StartPostCode==rangeItem.StartPostCode||
                        postCodeRange.StartPostCode==rangeItem.EndPostCode||
                        postCodeRange.EndPostCode==rangeItem.StartPostCode||
                        postCodeRange.EndPostCode==rangeItem.EndPostCode
                    )
                    {
                        result.msg = $"邮编范围[{postCodeRange.StartPostCode}-{postCodeRange.EndPostCode}]与[{rangeItem.StartPostCode}-{rangeItem.EndPostCode}]有重叠！";
                        return result;
                    }
                }
                #endregion
            }

            result.success = true;
            result.msg = "没有重叠的邮编范围";
            return result;
        }
        #endregion

    }
}
