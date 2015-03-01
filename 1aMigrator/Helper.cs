using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _1aMigrator
{
    public static class Helper
    {
        public static Dictionary<string, List<BusinessFoodSpecial>> InitializeDayFoodSpecials() 
        {
            var dayFoodSpecials = new Dictionary<string, List<BusinessFoodSpecial>>();
            dayFoodSpecials.Add("Monday", new List<BusinessFoodSpecial>());
            dayFoodSpecials.Add("Tuesday", new List<BusinessFoodSpecial>());
            dayFoodSpecials.Add("Wednesday", new List<BusinessFoodSpecial>());
            dayFoodSpecials.Add("Thursday", new List<BusinessFoodSpecial>());
            dayFoodSpecials.Add("Friday", new List<BusinessFoodSpecial>());
            dayFoodSpecials.Add("Saturday", new List<BusinessFoodSpecial>());
            dayFoodSpecials.Add("Sunday", new List<BusinessFoodSpecial>());

            return dayFoodSpecials;
        }

        public static Dictionary<string, List<BusinessDrinkSpecial>> InitializeDayDrinkSpecials()
        {
            var dayDrinkSpecials = new Dictionary<string, List<BusinessDrinkSpecial>>();
            dayDrinkSpecials.Add("Monday", new List<BusinessDrinkSpecial>());
            dayDrinkSpecials.Add("Tuesday", new List<BusinessDrinkSpecial>());
            dayDrinkSpecials.Add("Wednesday", new List<BusinessDrinkSpecial>());
            dayDrinkSpecials.Add("Thursday", new List<BusinessDrinkSpecial>());
            dayDrinkSpecials.Add("Friday", new List<BusinessDrinkSpecial>());
            dayDrinkSpecials.Add("Saturday", new List<BusinessDrinkSpecial>());
            dayDrinkSpecials.Add("Sunday", new List<BusinessDrinkSpecial>());

            return dayDrinkSpecials;
        }
    }
}
