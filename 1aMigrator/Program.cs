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

                //CreateBusinessNodes(db, client);
                CreateDaysOfWeekNodes(client);
            }
        }

        private static void CreateDaysOfWeekNodes(GraphClient client)
        {
            var days = new string[]{"monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"};
            foreach (var day in days) 
            {
                client.Cypher
                .Create("(user:day {day})")
                .WithParam("day", new { day = day })
                .ExecuteWithoutResults();
            }            
        }

        private static void CreateBusinessNodes(onea db, GraphClient client)
        {
            var businesses = db.UserProfiles.ToList();

            using (var conn = new SqlConnection("Server=RT-PC\\SQL2012;Database=PourtraitBeta;Trusted_Connection=True;"))
            {
                conn.Open();

                foreach (var business in businesses)
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
                                client.Cypher
                                    .Create("(user:business {biz})")
                                    .WithParam("biz", new { name = business.Name })
                                    .ExecuteWithoutResults();
                            }
                        }

                        reader.Close();
                    }
                }
            }
        }
    }
}
