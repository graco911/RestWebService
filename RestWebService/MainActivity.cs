using Android.App;
using Android.Widget;
using System.Json;
using System;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Android.Locations;
using Android.Content;
using Android.OS;
using Android.Runtime;

namespace RestWebService
{
    [Activity(Label = "RestWebService", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, ILocationListener
    {
        TextView gpsstatus;
        EditText latitude;
        EditText longitude;
        LocationManager locationManager;
        ProgressDialog progress;
        Button buttongps;

        public void OnLocationChanged(Location location)
        {

            latitude.Text = location.Latitude.ToString();
            longitude.Text = location.Longitude.ToString();

        }

        public void OnProviderDisabled(string provider)
        {
            gpsstatus.Text = "Provider Disabled";
        }

        public void OnProviderEnabled(string provider)
        {
            gpsstatus.Text = "Provider Enabled";

        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            throw new NotImplementedException();
        }

        protected override void OnCreate(Android.OS.Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            //Get latitude/longitude EditBox and Button resources
            latitude = FindViewById<EditText>(Resource.Id.latText);
            longitude = FindViewById<EditText>(Resource.Id.longText);
            Button button = FindViewById<Button>(Resource.Id.getWeatherButton);
            buttongps = FindViewById<Button>(Resource.Id.buttonGetGPSData);
            gpsstatus = FindViewById<TextView>(Resource.Id.gpstext);

            progress = new Android.App.ProgressDialog(this);
            progress.Indeterminate = true;
            progress.SetProgressStyle(Android.App.ProgressDialogStyle.Spinner);
            progress.SetMessage("Espere...");
            progress.SetCancelable(false);

            //When the user click the button
            button.Click += async delegate
            {
                progress.SetTitle("Obteniendo Datos");
                progress.Show();
                var username = "gacc911002";
                //Get latitude and longitude entered by the user and create a query
                string url = string.Format("http://api.geonames.org/findNearByWeatherJSON?lat={0}&lng={1}&username={2}", latitude.Text, longitude.Text, username);

                //Fecth the weather information asynchronously
                //parse the results then update the screen:
                Result result = await FetchWeatherAsync(url);
                if(result != null)
                {
                    ParseAndDisplay(result);
                    progress.Hide();
                }
                else
                {
                    progress.Hide();
                    Toast.MakeText(this, "Error", ToastLength.Long).Show();
                }

            };

        }

        protected override void OnResume()
        {
            base.OnResume();

            locationManager = GetSystemService(Context.LocationService) as LocationManager;

            buttongps.Click += delegate
            {
                progress.SetTitle("Esperando Datos del GPS");
                progress.Show();
                if (locationManager.AllProviders.Contains(LocationManager.NetworkProvider) && locationManager.IsProviderEnabled(LocationManager.NetworkProvider))
                {
                    locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 2000, 1, this);
                }
                else
                {
                    progress.Hide();
                    Toast.MakeText(this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show();
                }
                progress.Hide();
            };


        }


        private async Task<Result> FetchWeatherAsync(string url)
        {
            /*//Create an HTTP web request using the URL
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            request.ContentType = "application/json";
            request.Method = "GET";
            WeatherObservation weatherObservations = null;

            //Send the request to the server and wait for the response
            using (WebResponse response = await request.GetResponseAsync())
            {
                //Get a stream representation of the HTTP web response
                using (Stream stream = response.GetResponseStream())
                {
                    //use this stream to build a JSON document object
                    JsonValue jsonDoc = await Task.Run(() => JsonObject.Load(stream));
                    Console.Out.WriteLine("Response: {0}", jsonDoc.ToString());

                    JsonConvert.PopulateObject(jsonDoc.ToString(), weatherObservations);
                    //return the JSON document
                    return weatherObservations;
                }
            }*/

            Result weatherObservations = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string json = JsonConvert.SerializeObject(new Result());

                    using (HttpResponseMessage res = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json")))
                    {
                        var tem = res.Content.ReadAsStringAsync().Result;
                        if(tem.Length > 0)
                        {
                            weatherObservations = new Result();
                            JsonConvert.PopulateObject(tem, weatherObservations);
                        }
                    }
                }
            }catch (Exception e)
            {

            }

            return weatherObservations;
        }

        //Parse the weather data, then write temperature, humidity,
        //conditions, and location to the screen
        private void ParseAndDisplay(Result result)
        {
            //get the weather reporting fields from the layout resource
            TextView location = FindViewById<TextView>(Resource.Id.locationText);
            TextView temperature = FindViewById<TextView>(Resource.Id.tempText);
            TextView humidity = FindViewById<TextView>(Resource.Id.humidText);
            TextView conditions = FindViewById<TextView>(Resource.Id.condText);

            /*
            //Extract the array of name/value results for the field name "weatherobservation
            JsonValue weatherResults = json["weatherObservation"];

            //extract the "stationame" (location string) and write it to the location TextBox
            location.Text = weatherResults["stationName"];

            //the temperature is expressed in Celsius
            double temp = weatherResults["temperature"];
            //convert it to Farenheit
            //temp = ((9.0 / 5.0) * temp) + 32;
            //write the temperature (one decimal place) to the temperature TextBox
            temperature.Text = string.Format("{0:F1}", temp) + "C°";

            //Get the percent humidity and write it to the humidity TextBox
            double humidPercent = weatherResults["humidity"];
            humidity.Text = humidPercent.ToString() + "%";

            //Get the clouds and weatherconditions string and
            //combine them. ignore strings that are reported as n/a
            string cloudy = weatherResults["clouds"];
            if (cloudy.Equals("n/a"))
                cloudy = "";
            string cond = weatherResults["weatherCondition"];
            if (cond.Equals("n/a"))
                cond = "";

            //write the result to the conditions TextBox
            conditions.Text = cloudy + " " + cond;*/
            location.Text = result.weatherObservation.stationName;
            temperature.Text = result.weatherObservation.temperature.ToString();
            humidity.Text = result.weatherObservation.humidity.ToString();
            if (result.weatherObservation.clouds.Equals("n/a"))
                humidity.Text = "";
            if (result.weatherObservation.weatherCondition.Equals("n/a"))
                conditions.Text = "";
            
        }
    }

    public class Result
    {
        public WeatherObservation weatherObservation { get; set; }
    }

    public class WeatherObservation
    {
        public int elevation { get; set; }
        public long lng { get; set; }
        public string observation { get; set; }
        public string ICAO { get; set; }
        public string clouds { get; set; }
        public string dewPoint { get; set; }
        public string cloudsCode { get; set; }
        public string datetime { get; set; }
        public float seaLevelPressure { get; set; }
        public string countryCode { get; set; }
        public string temperature { get; set; }
        public int humidity { get; set; }
        public string stationName { get; set; }
        public string weatherCondition { get; set; }
        public int windDirection { get; set; }
        public string windSpeed { get; set; }
        public long lat { get; set; }
    }

}

