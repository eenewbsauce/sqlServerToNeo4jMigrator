using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;

namespace _1aMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var db = new onea { })
            {
                var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
                client.Connect();

                var businesses = GetBusinesses(db);
                //CreateBusinessNodes(db, client, businesses);
                //CreateDaysOfWeekNodes(client);
                RelateBusinessesWithDaysAndSepcials(db, client, businesses);
                //Console.ReadLine();
            }
        }

        private static List<UserProfile> GetBusinesses(onea db)
        {
            var confirmedBusinesses = new List<UserProfile>();
            var possibleBusinesses = db.UserProfiles.ToList();

            using (var conn = new SqlConnection("Server=RT-PC\\SQL2012;Database=PourtraitBeta;Trusted_Connection=True;"))
            {
                conn.Open();                    
                
                foreach (var business in possibleBusinesses)
                {
                    using (var cmd = new SqlCommand("IsUseraBusiness", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@userId", SqlDbType.Int).Value = business.UserId;
                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            var x = reader[0];

                            if (x != null)
                            {
                                confirmedBusinesses.Add(business);
                            }
                        }

                        reader.Close();
                    }
                }
            }

            return confirmedBusinesses;

        }

        private static void RelateBusinessesWithDaysAndSepcials(onea db, GraphClient client, List<UserProfile> businesses)
        {
            var foodSpecials = db.BusinessFoodSpecials.ToList();
            var dayFoodSpecials = new Dictionary<string, List<BusinessFoodSpecial>>();                
                dayFoodSpecials.Add("Monday", new List<BusinessFoodSpecial>());
                dayFoodSpecials.Add("Tuesday", new List<BusinessFoodSpecial>());
                dayFoodSpecials.Add("Wednesday", new List<BusinessFoodSpecial>());
                dayFoodSpecials.Add("Thursday", new List<BusinessFoodSpecial>());
                dayFoodSpecials.Add("Friday", new List<BusinessFoodSpecial>());
                dayFoodSpecials.Add("Saturday", new List<BusinessFoodSpecial>());
                dayFoodSpecials.Add("Sunday", new List<BusinessFoodSpecial>());

            foreach (var fs in foodSpecials)
            {
                dayFoodSpecials[fs.DayOfWeek].Add(fs);
            }

           

            //var business = tester.Results;
            //var first = business.First();
            //var name = first.name;

            foreach (var key in dayFoodSpecials.Keys)
            {
                foreach (var fs in dayFoodSpecials[key]) 
                {
                    var businessId = db.BusinessFoodItemCategories.Find(fs.BusinessFoodItem.BusinessFoodItemCategoryId).FoodMenu.UserId;
                    var businessName = db.UserProfiles.Find(businessId).Name;

                    var tester = client.Cypher
                       .Match(string.Concat("(n:business {name:\"", businessName.Replace("'", "\'"), "\"})"))
                       .Return(n => n.As<Business>());

                    Console.WriteLine(tester.Results.First().name);

                    //var test = client.Cypher
                    //    //.Match("(d:day {day:'" + key.ToLower() + "'})")
                    //    .Match("(p:business {name: '" + businessName + "'})")
                    //    .Return(x =>
                    //        Console.WriteLine(x.ToString());
                    //        return x.As<Business>();
                    //    });
                        //.Merge("(d)-[r:RESTAURANT_HAS_SPECIAL]->(p)")
                        //.Return(p => p.As<Business>());
                    //var another = test.Results.ToList();
                    //var first = another.First();
                    //Console.WriteLine(first.Name);
                }
            }
        }

        private static void CreateDaysOfWeekNodes(GraphClient client)
        {
            var days = new string[]{"monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"};
            foreach (var day in days) 
            {
                client.Cypher
                .Create("(n:day {day})")
                .WithParam("day", new { day = day })
                .ExecuteWithoutResults();
            }            
        }

        private static void CreateBusinessNodes(onea db, GraphClient client, List<UserProfile> businesses)
        {
            foreach (var business in businesses)
            {                    
                client.Cypher
                    .Create("(n:business {biz})")
                    .WithParam("biz", new { name = business.Name })
                    .ExecuteWithoutResults();                          
            }            
        }
    }
}
