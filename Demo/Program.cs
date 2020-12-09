using System;
using TZ.CountryAddress;

namespace Demo
{
    class Program
    {
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
            Console.ReadKey();
        }
    }
}
