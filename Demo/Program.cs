using System;
using System.Collections.Generic;
using TZ.CountryAddress;

namespace Demo
{
    class Program
    {
        private static List<string> postRangeList = new List<string>()
        {
            "90000-90899",
            "91000-92899",
            "93000-93599",
            "86400-86499",
            "88900-89199",
            "89300-89399",
            "93600-93999",
            "95000-95199",
            "82900-83299",
            "83400-83499",
            "84000-84799",
            "85000-85399",
            "85500-85799",
            "85900-86099",
            "86300-86399",
            "89400-89599",
            "89700-89899",
            "94000-94999",
            "95200-96199",
            "59000-59999",
            "67900-67999",
            "73900-73999",
            "79000-79499",
            "79700-81699",
            "82000-82899",
            "83300-83399",
            "83500-83899",
            "86500-86599",
            "87000-87199",
            "87300-88599",
            "97000-98699",
            "98800-99499",
            "51000-51399",
            "51500-51699",
            "57000-57799",
            "58500-58899",
            "64000-64199",
            "64400-64799",
            "64900-64999",
            "66000-66299",
            "66400-67899",
            "68000-68199",
            "68300-69399",
            "72600-72799",
            "72900-73199",
            "73300-73899",
            "74000-74199",
            "74300-75499",
            "75700-75899",
            "76000-77099",
            "77200-78999",
            "79500-79699",
            "35000-35299",
            "35400-35999",
            "36200-36299",
            "36500-36699",
            "36900-37299",
            "37500-37599",
            "38000-39799",
            "42000-42499",
            "46000-46499",
            "46900-46999",
            "47200-47299",
            "47400-47999",
            "49800-50999",
            "51400-51499",
            "52000-52899",
            "53000-53299",
            "53400-53599",
            "53700-55199",
            "55300-56799",
            "58000-58499",
            "60000-62099",
            "62200-63199",
            "63300-63999",
            "64800-64899",
            "65000-65899",
            "70000-70199",
            "70300-70899",
            "71000-71499",
            "71600-72599",
            "72800-72899",
            "75500-75699",
            "75900-75999",
            "00500-00599",
            "01000-08999",
            "09900-21299",
            "21400-26899",
            "27000-33999",
            "34100-34299",
            "34400-34499",
            "34600-34799",
            "34900-34999",
            "36000-36199",
            "36300-36499",
            "36700-36899",
            "37300-37499",
            "37600-37999",
            "39800-41899",
            "42500-42799",
            "43000-45999",
            "46500-46899",
            "47000-47199",
            "47300-47399",
            "48000-49799",
            "00600-00999",
            "96700-96999",
            "99500-99999",
        };

        static void Main(string[] args)
        {
            var service=new PostCodeService();
            var result1 = service.CheckPostCodeFormat("US", "12345-1234");
            var result2 = service.CheckPostCodeInRange("US", "1000","2000","1234");
            var result3 = service.CheckPostCodeInRange("GB", "S109EE", "S129EE", "S119EE");
            var result4 = service.GetPostCodeRegex("US");
            var result5 = service.CheckPostCodeInRange("US", "50000", "99999", "12345");

            //Console.WriteLine(result4.msg);
            var provinceService=new ProvinceService();
            var list1 = provinceService.GetProvinceInfosByCountry("US");
            var list2 = provinceService.GetProvinceInfosByCountry("US");
            List<PostCodeRangeModel> rangeList=new List<PostCodeRangeModel>();
            for (var i = 0; i < 10; i++)
            {
                var model=new PostCodeRangeModel();
                model.StartPostCode = $"{i}0000";
                model.EndPostCode = $"{i}9999";
                rangeList.Add(model);
            }
            rangeList.Add(new PostCodeRangeModel()
            {
                StartPostCode = "12345",
                EndPostCode = "12346"
            });


            var rangeList2 = new List<PostCodeRangeModel>();
            foreach (var item in postRangeList)
            {
                var arr = item.Split('-');

                var model = new PostCodeRangeModel();
                model.StartPostCode =arr[0];
                model.EndPostCode =arr[1];
                rangeList2.Add(model);
            }
            rangeList2.Add(new PostCodeRangeModel()
            {
                StartPostCode = "12345",
                EndPostCode = "12346"
            });
            var result6 = service.CheckRangeOverlap("US", rangeList2);
            Console.WriteLine(result6.msg);
            Console.ReadKey();
        }
    }
}
