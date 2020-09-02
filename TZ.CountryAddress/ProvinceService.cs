using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TZ.CountryAddress
{
    public class ProvinceService
    {

        /// <summary>
        /// 国际州信息缓存字典
        /// </summary>
        private static readonly ConcurrentDictionary<string, List<ProvinceModel>> CountryProvinceInfoDic = new ConcurrentDictionary<string, List<ProvinceModel>>();

        public ProvinceService()
        {
            CountryProvinceInfoDic.TryAdd("US", InitProvinceInfosByCountry("US"));
        }

        public List<ProvinceModel> GetProvinceInfosByCountry(string countryCode)
        {
            if (CountryProvinceInfoDic.TryGetValue(countryCode, out var countryProvinceInfoList))
            {
                return countryProvinceInfoList;
            }
            return null;
        }


        public (bool success, string msg) CheckProvince(string countryCode, string province)
        {
            (bool success, string msg) result = new ValueTuple<bool, string>();
            var getResult = GetProvinceInfo(countryCode, province);
            if (!getResult.success)
            {
                result.success = false;
                result.msg = getResult.msg;
                return result;
            }
            result.msg = "州正确";
            result.success = true;
            return result;
        }


        public (bool success, string msg, ProvinceModel provinceInfo) GetProvinceInfo(string countryCode, string province)
        {
            (bool success, string msg, ProvinceModel provinceInfo) result = new ValueTuple<bool, string,ProvinceModel>();
            var countryProvinceInfoList = GetProvinceInfosByCountry(countryCode);
            if (countryProvinceInfoList == null || countryProvinceInfoList.Count == 0)
            {
                result.success = false;
                result.msg = "没有找到该国家代码的州数据";
                return result;
            }

            var provinceInfo =
                countryProvinceInfoList.FirstOrDefault(x =>
                    CheckSameProvinceStr(province, x.ProvinceCode) ||
                    CheckSameProvinceStr(province, x.ProvinceEnName) ||
                    CheckSameProvinceStr(province, x.ProvinceCnName)
                );

            if (provinceInfo == null)
            {
                result.success = false;
                result.msg = $"没有找到该国家的[{province}]州信息";
                return result;
            }
            result.success = true;
            result.provinceInfo = provinceInfo;
            return result;
        }

        private bool CheckSameProvinceStr(string str1,string str2)
        {
            if (string.IsNullOrEmpty(str1)&&string.IsNullOrEmpty(str2))
                return true;

            if (string.IsNullOrEmpty(str1)&&!string.IsNullOrEmpty(str2))
                return false;

            if (!string.IsNullOrEmpty(str1)&&string.IsNullOrEmpty(str2))
                return false;

            if (str1 == str2)
                return true;
            str1 = str1.Replace("  ", " ").Replace(" ", "").ToUpper();
            str2 = str2.Replace("  ", " ").Replace(" ", "").ToUpper();

            return str1 == str2;
        }

        #region private method

        public List<ProvinceModel> InitProvinceInfosByCountry(string countryCode)
        {
            var countryProvinceInfoList = new List<ProvinceModel>();
            #region 美国
            if (countryCode == "US")
            {
                //根据美国邮政USPS官网：https://tools.usps.com/zip-code-lookup.htm?byaddress
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AL",
                        ProvinceEnName = "Alabama",
                        ProvinceCnName = "阿拉巴马州",
                        ProvinceLocalName = "Alabama",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AK",
                        ProvinceEnName = "Alaska",
                        ProvinceCnName = "阿拉斯加州",
                        ProvinceLocalName = "Alaska",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AS",
                        ProvinceEnName = "American Samoa",
                        ProvinceCnName = "美属萨摩亚",
                        ProvinceLocalName = "American Samoa",
                        ProvinceAliasList = new List<string> { "东萨摩亚", "美洲萨摩亚群岛" }
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AZ",
                        ProvinceEnName = "Arizona",
                        ProvinceCnName = "亚利桑那州",
                        ProvinceLocalName = "Arizona",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AR",
                        ProvinceEnName = "Arkansas",
                        ProvinceCnName = "阿肯色州",
                        ProvinceLocalName = "Arkansas",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "CA",
                        ProvinceEnName = "California",
                        ProvinceCnName = "加利福尼亚州",
                        ProvinceLocalName = "California",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "CO",
                        ProvinceEnName = "Colorado",
                        ProvinceCnName = "科罗拉多州",
                        ProvinceLocalName = "Colorado",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "CT",
                        ProvinceEnName = "Connecticut",
                        ProvinceCnName = "康涅狄格州",
                        ProvinceLocalName = "Connecticut",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "DE",
                        ProvinceEnName = "Delaware",
                        ProvinceCnName = "特拉华州",
                        ProvinceLocalName = "Delaware",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "DC",
                        ProvinceEnName = "District of Columbia",
                        ProvinceCnName = "哥伦比亚特区",
                        ProvinceLocalName = "District of Columbia",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "FM",
                        ProvinceEnName = "Federated Stated of Micronesia",
                        ProvinceCnName = "密克罗尼西亚联邦",
                        ProvinceLocalName = "Federated Stated of Micronesia",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "FL",
                        ProvinceEnName = "Florida",
                        ProvinceCnName = "弗罗里达州",
                        ProvinceLocalName = "Florida",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "GA",
                        ProvinceEnName = "Georgia",
                        ProvinceCnName = "乔治亚州",
                        ProvinceLocalName = "Georgia",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "GU",
                        ProvinceEnName = "Guam",
                        ProvinceCnName = "关岛",
                        ProvinceLocalName = "Guam",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "HI",
                        ProvinceEnName = "Hawaii",
                        ProvinceCnName = "夏威夷",
                        ProvinceLocalName = "Hawaii",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "ID",
                        ProvinceEnName = "Idaho",
                        ProvinceCnName = "爱达荷州",
                        ProvinceLocalName = "Idaho",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "IL",
                        ProvinceEnName = "Illinois",
                        ProvinceCnName = "伊利诺伊州",
                        ProvinceLocalName = "Illinois",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "IN",
                        ProvinceEnName = "Indiana",
                        ProvinceCnName = "印第安纳州",
                        ProvinceLocalName = "Indiana",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "IA",
                        ProvinceEnName = "Iowa",
                        ProvinceCnName = "艾奥瓦州",
                        ProvinceLocalName = "Iowa",
                        ProvinceAliasList = new List<string> { "爱荷华州", "爱阿华州", "爱我华州", "衣阿华州" }
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "KS",
                        ProvinceEnName = "Kansas",
                        ProvinceCnName = "堪萨斯州",
                        ProvinceLocalName = "Kansas",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "KY",
                        ProvinceEnName = "Kentucky",
                        ProvinceCnName = "肯塔基州",
                        ProvinceLocalName = "Kentucky",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "LA",
                        ProvinceEnName = "Louisiana",
                        ProvinceCnName = "路易斯安那州",
                        ProvinceLocalName = "Louisiana",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "ME",
                        ProvinceEnName = "Maine",
                        ProvinceCnName = "缅因州",
                        ProvinceLocalName = "Maine",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MH",
                        ProvinceEnName = "Marshall Islands",
                        ProvinceCnName = "马绍尔群岛",
                        ProvinceLocalName = "Marshall Islands",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MD",
                        ProvinceEnName = "Maryland",
                        ProvinceCnName = "马里兰州",
                        ProvinceLocalName = "Maryland",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MA",
                        ProvinceEnName = "Massachusetts",
                        ProvinceCnName = "马赛诸塞州",
                        ProvinceLocalName = "Massachusetts",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MI",
                        ProvinceEnName = "Michigan",
                        ProvinceCnName = "密歇根州",
                        ProvinceLocalName = "Michigan",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MN",
                        ProvinceEnName = "Minnesota",
                        ProvinceCnName = "明尼苏达州",
                        ProvinceLocalName = "Minnesota",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MS",
                        ProvinceEnName = "Mississippi",
                        ProvinceCnName = "密西西比州",
                        ProvinceLocalName = "Mississippi",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MO",
                        ProvinceEnName = "Missouri",
                        ProvinceCnName = "密苏里州",
                        ProvinceLocalName = "Missouri",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MT",
                        ProvinceEnName = "Montana",
                        ProvinceCnName = "蒙大拿州",
                        ProvinceLocalName = "Montana",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "NE",
                        ProvinceEnName = "Nebraska",
                        ProvinceCnName = "内布拉斯加州",
                        ProvinceLocalName = "Nebraska",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "NV",
                        ProvinceEnName = "Nevada",
                        ProvinceCnName = "内华达州",
                        ProvinceLocalName = "Nevada",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "NH",
                        ProvinceEnName = "New Hampshire",
                        ProvinceCnName = "新罕布什尔州",
                        ProvinceLocalName = "New Hampshire",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "NJ",
                        ProvinceEnName = "New Jersey",
                        ProvinceCnName = "新泽西州",
                        ProvinceLocalName = "New Jersey",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "NM",
                        ProvinceEnName = "New Mexico",
                        ProvinceCnName = "新墨西哥州",
                        ProvinceLocalName = "New Mexico",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "NY",
                        ProvinceEnName = "New York",
                        ProvinceCnName = "纽约州",
                        ProvinceLocalName = "New York",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "NC",
                        ProvinceEnName = "North Carolina",
                        ProvinceCnName = "北卡罗莱纳州",
                        ProvinceLocalName = "North Carolina",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "ND",
                        ProvinceEnName = "North Dakota",
                        ProvinceCnName = "北达科塔州",
                        ProvinceLocalName = "North Dakota",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "MP",
                        ProvinceEnName = "Northern Mariana Islands",
                        ProvinceCnName = "北马里亚纳群岛邦",
                        ProvinceLocalName = "Northern Mariana Islands",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "OH",
                        ProvinceEnName = "Ohio",
                        ProvinceCnName = "俄亥俄州",
                        ProvinceLocalName = "Ohio",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "OK",
                        ProvinceEnName = "Oklahoma",
                        ProvinceCnName = "俄克拉荷马州",
                        ProvinceLocalName = "Oklahoma",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "OR",
                        ProvinceEnName = "Oregon",
                        ProvinceCnName = "俄勒冈州",
                        ProvinceLocalName = "Oregon",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "PW",
                        ProvinceEnName = "Palau",
                        ProvinceCnName = "帕劳",
                        ProvinceLocalName = "Palau",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "PA",
                        ProvinceEnName = "Pennsylvania",
                        ProvinceCnName = "宾夕法尼亚州",
                        ProvinceLocalName = "Pennsylvania",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "PR",
                        ProvinceEnName = "Puerto Rico",
                        ProvinceCnName = "波多黎各",
                        ProvinceLocalName = "Puerto Rico",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "RI",
                        ProvinceEnName = "Rhode Island",
                        ProvinceCnName = "罗德岛州",
                        ProvinceLocalName = "Rhode Island",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "SC",
                        ProvinceEnName = "South Carolina",
                        ProvinceCnName = "南卡罗莱纳州",
                        ProvinceLocalName = "South Carolina",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "SD",
                        ProvinceEnName = "South Dakota",
                        ProvinceCnName = "南达科塔州",
                        ProvinceLocalName = "South Dakota",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "TN",
                        ProvinceEnName = "Tennessee",
                        ProvinceCnName = "田纳西州",
                        ProvinceLocalName = "Tennessee",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "TX",
                        ProvinceEnName = "Texas",
                        ProvinceCnName = "德克萨斯州",
                        ProvinceLocalName = "Texas",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "UT",
                        ProvinceEnName = "Utah",
                        ProvinceCnName = "犹他州",
                        ProvinceLocalName = "Utah",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "VT",
                        ProvinceEnName = "Vermont",
                        ProvinceCnName = "佛蒙特州",
                        ProvinceLocalName = "Vermont",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "VI",
                        ProvinceEnName = "Virgin Islands",
                        ProvinceCnName = "美属维尔京群岛",
                        ProvinceLocalName = "Virgin Islands",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "VA",
                        ProvinceEnName = "Virginia",
                        ProvinceCnName = "弗吉尼亚州",
                        ProvinceLocalName = "Virginia",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "WA",
                        ProvinceEnName = "Washington",
                        ProvinceCnName = "华盛顿州",
                        ProvinceLocalName = "Washington",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "WV",
                        ProvinceEnName = "West Virginia",
                        ProvinceCnName = "西弗吉尼亚州",
                        ProvinceLocalName = "West Virginia",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "WI",
                        ProvinceEnName = "Wisconsin",
                        ProvinceCnName = "威斯康星州",
                        ProvinceLocalName = "Wisconsin",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "WY",
                        ProvinceEnName = "Wyoming",
                        ProvinceCnName = "怀俄明州",
                        ProvinceLocalName = "Wyoming",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AA",
                        ProvinceEnName = "Armed Forces Americas",
                        ProvinceCnName = "美洲美军基地",
                        ProvinceLocalName = "Armed Forces Americas",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AE",
                        ProvinceEnName = "Armed Forces Africa",
                        ProvinceCnName = "非洲美军基地",
                        ProvinceLocalName = "Armed Forces Africa",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AE",
                        ProvinceEnName = "Armed Forces Canada",
                        ProvinceCnName = "加拿大美军基地",
                        ProvinceLocalName = "Armed Forces Canada",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AE",
                        ProvinceEnName = "Armed Forces Europe",
                        ProvinceCnName = "欧洲美军基地",
                        ProvinceLocalName = "Armed Forces Europe",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AE",
                        ProvinceEnName = "Armed Forces Middle East",
                        ProvinceCnName = "中东美军基地",
                        ProvinceLocalName = "Armed Forces Middle East",
                    }
                );
                countryProvinceInfoList.Add(
                    new ProvinceModel
                    {
                        ProvinceCode = "AP",
                        ProvinceEnName = "Armed Forces Pacific",
                        ProvinceCnName = "太平洋美军基地",
                        ProvinceLocalName = "Armed Forces Pacific",
                    }
                );

            }
            #endregion

            return countryProvinceInfoList;
        }
        #endregion
    }
}