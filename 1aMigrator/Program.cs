using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Neo4jClient;
using System.Drawing;
using System.Drawing.Imaging;

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
                CreateBusinessNodes(db, client, businesses);
                CreateDaysOfWeekNodes(client);
                RelateBusinessesWithDaysAndSepcials(db, client, businesses);
                CreateGenres(db, client, businesses);
            }
        }

        private static string S3Put(string uri, string name)
        {
            string localFilename = @"c:\temp\s3\temp.jpg";

            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(string.Format("http://www.pourtrait.com{0}", uri), localFilename);
                }

                AmazonS3Client client = new AmazonS3Client();

                var stream = new System.IO.MemoryStream();
                var image = Image.FromFile(localFilename);
                image.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;

                var s3Name = name.Replace(" ", "").ToLower();

                PutObjectRequest request = new PutObjectRequest()
                {
                    InputStream = stream,
                    BucketName = "gounce_import",
                    Key = s3Name,
                    CannedACL = S3CannedACL.PublicRead
                };

                PutObjectResponse response = client.PutObject(request);

                return string.Format("https://s3.amazonaws.com/gounce_import/{0}", s3Name);  
            }
            catch (Exception e) 
            {
                return string.Empty;
            }

                      
        }

        private static void CreateGenres(onea db, GraphClient client, List<UserProfile> businesses)
        {
            var businessIds = businesses.Select(y => y.UserId);
            var categoryBusinessXrefs = db.BusinessUserRestaurantCategoryXRefs.Where(x => businessIds.Contains(x.BusinessUser.UserId)).ToList();
            var categoriesDistinct = categoryBusinessXrefs.Select(x => x.RestaurantCategory).Distinct();

            foreach (var cd in categoriesDistinct)
            {
                client.Cypher
                    .Create("(n:genre {genre})")
                    .WithParam("genre", new { name = cd.Name.ToLower() })
                    .ExecuteWithoutResults();
            }

            foreach (var categoryBusiness in categoryBusinessXrefs)
            {               
                var businessName = db.UserProfiles.Find(categoryBusiness.BusinessUser.UserId).Name;

                try
                {
                    client.Cypher
                        .Match(string.Concat("(b:business {name:\"", businessName.Replace("'", "\'").Replace("\"", "\""), "\"})"))
                        .Match(string.Concat("(g:genre {name:\"", categoryBusiness.RestaurantCategory.Name.ToLower(), "\"})"))
                        .Merge("(b)-[r:HAS_GENRE]->(g)")
                        .ExecuteWithoutResults();
                }
                catch (Exception e) { }
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
            var drinkSpecials = db.BusinessDrinkSpecials.ToList();

            var dayFoodSpecials = Helper.InitializeDayFoodSpecials();
            var dayDrinkSpecials = Helper.InitializeDayDrinkSpecials();

            foreach (var fs in foodSpecials)
            {
                dayFoodSpecials[fs.DayOfWeek].Add(fs);
            }

            foreach (var ds in drinkSpecials)
            {
                dayDrinkSpecials[ds.DayOfWeek].Add(ds);
            }

            foreach (var key in dayFoodSpecials.Keys)
            {
                foreach (var fs in dayFoodSpecials[key]) 
                {
                    var businessId = db.BusinessFoodItemCategories.Find(fs.BusinessFoodItem.BusinessFoodItemCategoryId).FoodMenu.UserId;
                    var businessName = db.UserProfiles.Find(businessId).Name;

                    client.Cypher
                        .Match("(d:day {day:'" + key.ToLower() + "'})")
                        .Match(string.Concat("(p:business {name:\"", businessName.Replace("'", "\'"), "\"})"))
                        .Merge("(d)-[r:HAS_FOOD_SPECIAL]->(p)")
                        .ExecuteWithoutResults();
                    
                    client.Cypher
                        .Create("(n:food {food})")
                        .WithParam("food", new { 
                            name = fs.BusinessFoodItem.Name,
                            imageUrl = S3Put(fs.BusinessFoodItem.ImageUrl, fs.BusinessFoodItem.Name)
                        })
                        .ExecuteWithoutResults();

                    client.Cypher
                        .Match(string.Concat("(p:business{name:\"", businessName.Replace("'", "\'"), "\"})"))
                        .Match(string.Concat("(f:food {name:\"", fs.BusinessFoodItem.Name, "\"})"))
                        .Merge(string.Concat("(p)-[r:FOOD_SPECIAL {price:\"", fs.Price, "\", start:\"", fs.StartTime, "\", end:\"", fs.EndTime, "\", dayOfWeek:\"", fs.DayOfWeek.ToLower(), "\"}]->(f)"))
                        .ExecuteWithoutResults();

                }
            }

            foreach (var key in dayDrinkSpecials.Keys)
            {
                foreach (var ds in dayDrinkSpecials[key])
                {
                    var businessId = db.BarMenuBeverageInstances.Find(ds.BarMenuBeverageInstanceId).BusinessUserBeverageXRef.BusinessUserId;
                    var businessName = db.UserProfiles.Find(businessId).Name;
                    var beverage = db.Beverages.Find(ds.BarMenuBeverageInstance.BusinessUserBeverageXRef.BeverageId);

                    client.Cypher
                        .Match("(d:day {day:'" + key.ToLower() + "'})")
                        .Match(string.Concat("(p:business {name:\"", businessName.Replace("'", "\'"), "\"})"))
                        .Merge("(d)-[r:HAS_DRINK_SPECIAL]->(p)")
                        .ExecuteWithoutResults();
                    client.Cypher
                        .Create("(n:drink {drink})")
                        .WithParam("drink", new { 
                            name = beverage.Name,
                            imageUrl = S3Put(beverage.ImageUrl, beverage.Name)
                        })
                        .ExecuteWithoutResults();

                    client.Cypher
                        .Match(string.Concat("(p:business{name:\"", businessName.Replace("'", "\'"), "\"})"))
                        .Match(string.Concat("(f:drink {name:\"", beverage.Name, "\"})"))
                        .Merge(string.Concat("(p)-[r:DRINK_SPECIAL {price:\"", ds.Price, "\", start:\"", ds.StartTime, "\", end:\"", ds.EndTime, "\", dayOfWeek:\"", ds.DayOfWeek.ToLower(), "\"}]->(f)"))
                        .ExecuteWithoutResults();
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
                    .WithParam("biz", new { 
                        name = business.Name.Replace("\"",""), 
                        longitude = business.Longitude != null ? business.Longitude : 0,
                        latitude = business.Latitude != null ? business.Latitude : 0,
                        postal = business.PostalCode != null ? business.PostalCode : "",
                        address = business.Address != null ? business.Address : "", 
                        about = business.About != null ? business.About : "",
                        city = business.City != null ? business.City.Name : "",
                        state = business.State != null ? business.State.Name: "" ,
                        image = business.PreviewImagePath != null ? business.PreviewImagePath : ""
                    })
                    .ExecuteWithoutResults();                          
            }            
        }
    }
}
