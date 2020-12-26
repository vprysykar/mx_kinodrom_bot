using RestSharp;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace kinodrom_bot.Service
{
    public class NewYearStatisticService
    {
        readonly string server_url = "";
        public NewYearStatisticService(string url_from_config)
        {
            server_url = url_from_config;
        }

        public List<NewYearStatisticModel> Customers(X509Certificate2 cert)
        {
            Console.WriteLine($"Making GET request url:{server_url}");
            var client = new RestClient(server_url);
            client.ClientCertificates = new X509CertificateCollection() { cert };
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);            

            List<NewYearStatisticModel> data =  Newtonsoft.Json.JsonConvert.DeserializeObject<List<NewYearStatisticModel>>(response.Content);
            return data;
        }
        public List<NewYearStatisticModel> Customers()
        {
            var client = new RestClient(server_url);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            if (response.Content == "")
            {
                Console.WriteLine(response.ErrorException.ToString());
            }

            List<NewYearStatisticModel> data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<NewYearStatisticModel>>(response.Content);
            return data;
        }
    }
}
