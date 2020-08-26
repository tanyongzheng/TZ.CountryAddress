using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TZ.CountryAddress
{
    /// <summary>
    /// 参考资料：
    /// https://www.youbianku.com/
    /// https://en.wikipedia.org/wiki/List_of_postal_codes
    /// https://gist.github.com/tanyongzheng/aaac332ecc857d0e1b1e3f664430f368
    /// https://gist.github.com/tanyongzheng/3706648f3cee30de274891abd416bf85
    /// DHL邮编库
    /// </summary>
    public class PostCodeValidationRuleModel
    {
        /// <summary>
        /// 国家代码
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// 国家中文名
        /// </summary>
        public string CountryCnName { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        public string RegexStr { get; set; }

        /// <summary>
        /// 规则描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 范围是否为数字
        /// </summary>
        public  bool RangeIsNumber { get; set; }

        /// <summary>
        /// 比较范围提取正则表达式
        /// </summary>
        public string RangeRegexStr { get; set; }

        /*
        /// <summary>
        /// 范围字符开始索引
        /// </summary>
        public  int RangeStrStartIndex { get; set; }

        /// <summary>
        /// 范围字符长度
        /// </summary>
        public  int RangeStrLenght { get; set; }
        */

        /// <summary>
        /// A:字母
        /// N:数字
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 最小长度
        /// </summary>
        public  int? MinLenght { get; set; }

        /// <summary>
        /// 当小于最小长度的时，左边补齐的字符，默认0
        /// </summary>
        public string LeftPaddingChar { get; set; } = "0";

        /// <summary>
        /// 是否可以忽略邮编
        /// 譬如一些国家只有国际上的国家邮编，没有国内邮编
        /// </summary>
        //public  bool IgnorePostCode { get; set; }

        /// <summary>
        /// 没有邮编
        /// 譬如伯利兹
        /// </summary>
        public  bool NoPostCode { get; set; }

        /// <summary>
        /// 修复格式方法
        /// </summary>
        public Func<string,(bool success,string formatPostCode,string msg)> FixFormatFunc { get; set; }

    }
}