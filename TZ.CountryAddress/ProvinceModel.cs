using System.Collections.Generic;

namespace TZ.CountryAddress
{
    public class ProvinceModel
    {
        /// <summary>
        /// 省州代码
        /// </summary>
        public string ProvinceCode { get; set; }

        /// <summary>
        /// 英文名称
        /// </summary>
        public string ProvinceEnName { get; set; }

        /// <summary>
        /// 中文名称
        /// </summary>
        public string ProvinceCnName { get; set; }

        /// <summary>
        /// 当地语言的名称
        /// </summary>
        public string ProvinceLocalName { get; set; }

        /// <summary>
        /// 别名，包括缩写
        /// </summary>
        public List<string> ProvinceAliasList { get; set; }

    }
}