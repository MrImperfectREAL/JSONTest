using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using static JSONTest.WebForm1;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading;


namespace JSONTest
{
    public class MyApiService
    {
        string apiUrlYr = "https://api.met.no/weatherapi/nowcast/2.0/complete?lat=59.9333&lon=10.7166";
        string apiUrlSmartCitizen = "https://api.smartcitizen.me/v0/devices/14057";

        public double airTemperature;
        public double windSpeed;
        public double windFromDirection;

        public double airQuality;
        //oppgave 1
        public double GetAirTemperature()
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = httpClient.GetAsync(apiUrlYr).Result;
                string json = response.Content.ReadAsStringAsync().Result;
                dynamic data = JsonConvert.DeserializeObject(json);

                airTemperature = data.properties.time_series[0].data.instant.details.air_temperature;
                return airTemperature;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong");
                return 0;
            }
        }
        //oppgave 2
        public double GetWindSpeed()
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = httpClient.GetAsync(apiUrlYr).Result;
                string json = response.Content.ReadAsStringAsync().Result;
                dynamic data = JsonConvert.DeserializeObject(json);

                windSpeed = data.properties.time_series[0].data.instant.details.wind_speed;
                return windSpeed;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong");
                return 0;
            }
        }
        //oppgave 3
        public double GetWindFromDirection()
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = httpClient.GetAsync(apiUrlYr).Result;
                string json = response.Content.ReadAsStringAsync().Result;
                dynamic data = JsonConvert.DeserializeObject(json);

                windFromDirection = data.properties.time_series[0].data.instant.details.wind_from_direction;
                return windFromDirection;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong");
                return 0;
            }
        }
        //oppgave 4
        public WeatherData GetWeatherData()
        {
            MyApiService apiService = new MyApiService();
            try
            {
                double airTemperature = apiService.GetAirTemperature();
                double windSpeed = apiService.GetWindSpeed();
                double windFromDirection = apiService.GetWindFromDirection();

                WeatherData weatherData = new WeatherData();

                return weatherData;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong");
                return null;
            }
        }
        //oppgave 5
        public double GetAirQuality()
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = httpClient.GetAsync(apiUrlSmartCitizen).Result;
                string json = response.Content.ReadAsStringAsync().Result;
                dynamic data = JsonConvert.DeserializeObject(json);

                airQuality = data.location.sensors[8].value;
                return airQuality;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong");
                return 0;
            }
        }

        public static void InsertWeatherTableData(DateTime dateAndTime, float airTemperature, float windSpeed, float windFromDirection)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["ConnTemperature"].ConnectionString;
            SqlParameter param;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("insert into weather_table values(@dateAndTime,ROUND(@airTemperature, 1),ROUND(@windSpeed, 1) ROUND(@windFromDirection, 1))", conn);
                cmd.CommandType = CommandType.Text;

                param = new SqlParameter("@dateAndTime", SqlDbType.DateTime);
                param.Value = dateAndTime;
                cmd.Parameters.Add(param);

                param = new SqlParameter("@airTemperature", SqlDbType.Float);
                param.Value = airTemperature;
                cmd.Parameters.Add(param);

                param = new SqlParameter("@windSpeed", SqlDbType.Float);
                param.Value = windSpeed;
                cmd.Parameters.Add(param);

                param = new SqlParameter("@windFromDirection", SqlDbType.Float);
                param.Value = windFromDirection;
                cmd.Parameters.Add(param);

                int rows = cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void GetInsertWeatherValues()
        {
            //http://jsonviewer.stack.hu/
            //59.202752, 10.953535

            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.met.no/weatherapi/nowcast/2.0/complete?lat=59.9333&lon=10.7166");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";
                httpWebRequest.UserAgent = "bolle";
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                using (var streamReader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JObject jObj = JObject.Parse(result);

                    JToken Props = jObj.SelectToken("properties.timeseries[0].data.instant.details");
                    float airTemperature = Props.Value<float>("air_temperature");
                    float windSpeed = Props.Value<float>("wind_speed");
                    float windFromDirection = Props.Value<float>("wind_speed");
                    MyApiService.InsertWeatherTableData(DateTime.Now, airTemperature, windSpeed, windFromDirection);
                }
            }
            catch (Exception ex)
            {
            }

        }

        //oppgave 6 + 12
        public void Harvest()
        {
            MyApiService apiService = new MyApiService();

            for (; ; )
            {
                if (DateTime.Now.Minute == 0)
                {
                    apiService.GetInsertWeatherValues();
                    var timeOfDay = DateTime.Now.TimeOfDay;
                    var nextFullHour = TimeSpan.FromHours(Math.Ceiling(timeOfDay.TotalHours));
                    var delta = (nextFullHour - timeOfDay).TotalMilliseconds;
                    int Wait = 5 * 60 * 1000;
                    Thread.Sleep(Convert.ToInt32(delta) - Wait);
                }
                Thread.Sleep(10000);
            }
        }
        //oppgave 7
        public double WarmestAirTemperature()
        {
            double warmestAirTemperature = 0.0;
            var connectionString = ConfigurationManager.ConnectionStrings["ConnTemperature"].ConnectionString;
            SqlParameter param;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT MAX(air_temperature) FROM weather_data", conn);
                cmd.CommandType = CommandType.Text;
                object result = cmd.ExecuteReader();
                if (result != DBNull.Value && result != null)
                {
                    warmestAirTemperature = Convert.ToDouble(result);
                }
            }
            return warmestAirTemperature;
        }
        //oppgave 8
        public DataTable Top24Rows()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["ConnTemperature"].ConnectionString;
            SqlParameter param;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT TOP24 * FROM weather_data ORDER BY date_and_time DESC", conn);
                cmd.CommandType = CommandType.Text;
                object result = cmd.ExecuteReader();
                DataTable dataTable = new DataTable();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);


                adapter.Fill(dataTable);

                return dataTable;
            }

        }
        //oppgave 9 
        public int ShowTotalRows()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["ConnTemperature"].ConnectionString;
            int count = 0;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM weather_data", conn);
                cmd.CommandType = CommandType.Text;

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    count = Convert.ToInt32(reader[0]);
                }
            }
            return count;
        }

        //oppgave 13
        static double SolveEquation(string equation)
        {
            // Deler ligningen opp i venstre og høyre side av likhetstegnet
            string[] parts = equation.Split('=');
            string leftSide = parts[0].Trim();
            string rightSide = parts[1].Trim();

            // Konverterer venstre og høyre side av ligningen til numeriske verdier
            double leftValue = double.Parse(leftSide.Substring(leftSide.IndexOfAny(new char[] { '-', '+' })));
            double rightValue = double.Parse(rightSide.Substring(rightSide.IndexOfAny(new char[] { '-', '+' })));

            // Bestemmer hvilket operatør som brukes mellom likhetstegnet og ukjent
            char op = leftSide[leftSide.IndexOfAny(new char[] { '-', '+' })];

            // Regner ut verdien til ukjent
            double unknownValue = 0;
            switch (op)
            {
                case '+':
                    unknownValue = rightValue - leftValue;
                    break;
                case '-':
                    unknownValue = rightValue + leftValue;
                    break;
            }

            return unknownValue;
        }

        //oppgave 14
        static string ReverseString(string str)
        {
            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public class WeatherData
        {
            public double airTemperature { get; set; }
            public double windSpeed { get; set; }
            public double windFromDirection { get; set; }
        }
        public partial class WebForm1 : System.Web.UI.Page
        {

            protected void Page_Load(object sender, EventArgs e)
            {
            }
        }
    }
}