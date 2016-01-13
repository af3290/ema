using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Linq;


namespace Utils.Quandl
{
    /// <summary>
    /// Wrapper Class for Quandl.com API
    /// Basic functionality implemented for downloading Quandl.com data
    /// GOOG and YAHOO stocks using CSV format have been tested.
    /// </summary>
    public class Quandl
    {
        private const string QUANDL_API_URL = "http://www.quandl.com/api/v1/";
        private string AuthToken;

        /// <summary>
        /// Quandl Object Constructor
        /// </summary>
        /// <param name="authenticationToken">string auth token - if authentication token not specified on construction then access is limited to 10 per day</param>
        public Quandl(string authenticationToken = "")
        {
            AuthToken = authenticationToken;
        }

        /// <summary>
        /// Set the authorization token for the API calls.
        /// </summary>
        public void SetAuthToken(string token)
        {
            AuthToken = token;
        }


        /// <summary>
        /// Fetch the raw string data from Quandl.
        /// </summary>
        /// <param name="dataset"> dataset code as per Quandl.com website</param>
        /// <param name="settings"> as per the the Quandl.com website </param>
        /// <param name="format"> format for data to be returned as, default = "csv". Options are "csv", "plain", "json", "xml" </param>
        /// <returns> Returns string of data from Quandl.com </returns>
        public string GetRawData(string dataset, IDictionary<string, string> settings, string format = "csv")
        {
            string requestUrl = "";
            string rawData = "";
            var requestData = new StringBuilder(settings.Count());

            if (string.IsNullOrEmpty(AuthToken))
            {
                requestUrl = QUANDL_API_URL + String.Format("datasets/{0}.{1}?", dataset, format);
            }
            else
            {
                requestUrl = QUANDL_API_URL + String.Format("datasets/{0}.{1}?auth_token={2}", dataset, format, AuthToken);
            }

            foreach (KeyValuePair<string, string> kvp in settings)
            {
                requestData.Append(String.Format("&{0}={1}", kvp.Key, kvp.Value));
            }
            requestUrl = requestUrl + requestData.ToString();
            try
            {
                //Prevent 404 Errors:
                WebClient client = new WebClient();
                rawData = client.DownloadString(requestUrl);
            }
            catch (Exception err)
            {
                throw new Exception("Sorry there was an error and we could not connect to Quandl: " + err.Message);
            }

            return rawData;
        }

        /// <summary>
        /// Principle function for getting data from Quandl.com
        /// </summary>
        /// <typeparam name="T"> User defined data class</typeparam>
        /// <param name="dataset"> dataset code as per Quandl.com website</param>
        /// <param name="settings"> as per the the Quandl.com website </param>
        /// <param name="format"> format for data to be returned as, default = "csv". Options are "csv", "plain", "json", "xml" </param>
        /// <returns> Returns a list of objects T </returns>
        public IList<T> GetData<T>(string dataset, IDictionary<string, string> settings, string format = "csv")
        {
            //Initialize our generic holder:
            var data = new List<T>();

            //For user defined data should use CSV since easier to parse into class objects
            //format = "csv";

            //Download the required strings:
            string rawData = GetRawData(dataset, settings, format);

            //Convert into a list of class objects
            string[] lines = rawData.Split(new[] { '\r', '\n' });
            Console.WriteLine(lines[0]);


            var createdActivator = GetActivator<T>();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Trim().Length > 0)
                {
                    data.Add(createdActivator(new object[] { line }));
                }
            }

            return data;
        }


        //A faster version to instantiate objects dynamically, compared to Activator.CreateInstance
        /// <summary>
        /// Static function to generate a delegate to construct a given type. Entirely based on : http://rogeralsing.com/2008/02/28/linq-expressions-creating-objects/ with minor tweaks
        /// </summary>
        /// <typeparam name="T"> User defined data class</typeparam>
        /// <returns> Returns a list of objects T </returns>
        private static Func<object[], T> GetActivator<T>()
        {
            var dataType = typeof(T);
            var ctor = dataType.GetConstructors().First();
            var paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            var param =
                    Expression.Parameter(typeof(object[]), "args");

            var argsExp =
                    new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (var i = 0; i < paramsInfo.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = paramsInfo[i].ParameterType;

                var paramAccessorExp =
                        Expression.ArrayIndex(param, index);

                var paramCastExp =
                        Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            var newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            var lambda =
                    Expression.Lambda(typeof(Func<object[], T>), newExp, param);

            //compile it
            return (Func<object[], T>)lambda.Compile();
        }
    }
}

