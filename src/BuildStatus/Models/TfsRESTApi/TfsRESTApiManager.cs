using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BuildQuery.TfsData.Models.TfsRESTApi;
using Newtonsoft.Json;

namespace BuildStatus.Models.TfsRESTApi
{
    public class TfsRestApiManager
    {
        private readonly NetworkCredential _tfsCredential;
       
        public struct TfsApi
        {
            public const string V2 = "2.0";
        }

        public struct TfsProject
        {
            public const string Fct = "FCT";
        }

        public struct MediaType
        {
            public const string JsonMediaType = "application/json";
            public const string JsonPatchMediaType = "application/json-patch+json";
            public const string HtmlMediaType = "text/html";
            public const string OctetStream = "application/octet-stream";
            public const string Zip = "application/zip";
        }
        
        private readonly string _rootUrl;
        private readonly string _collection;
       

        public TfsRestApiManager(string rootUrl, string collection, NetworkCredential  credential)
        {
            _rootUrl = rootUrl;
            _collection = collection;
            _tfsCredential = credential;

        }

        protected string ApiVersion => TfsApi.V2;

        protected string ProjectName => TfsProject.Fct;

        public async Task<JsonCollection<BuildDefinition>> GetBuildDefinitions(string type=null, string name=null)
        {
            string response;
            if (type == null && name == null)
            {
                response = await GetResponse("build/definitions");
            }
            else
            {
                var args = new Dictionary<string, object>();

                if (type != null)
                {
                    args.Add("type", type);
                }

                if (name != null)
                {
                    args.Add("name", name);
                }

                response = await GetResponse("build/definitions", args);

            }
            return JsonConvert.DeserializeObject<JsonCollection<BuildDefinition>>(response);
        }

        public async Task<BuildDefinition> GetBuildDefinition(int definitionId)
        {
            var response = await GetResponse($"build/definitions/{definitionId}");
            return JsonConvert.DeserializeObject<BuildDefinition>(response);
        }

        public async Task<JsonCollection<Build>> GetLatestBuilds(IEnumerable<int> definitionIds)
        {
            var args = new Dictionary<string, object>
            {
                {"definitions", string.Join(",", definitionIds)},
                {"maxBuildsPerDefinition", 1},
                {"minFinishTime", DateTime.Now.AddDays(-30)}
            };

            string response = await GetResponse("build/builds", args);
            return JsonConvert.DeserializeObject<JsonCollection<Build>>(response);
        }
        public async Task<JsonCollection<Build>> GetLatestCompletedBuilds(IEnumerable<int> definitionIds)
        {
            var args = new Dictionary<string, object>
            {
                {"definitions", string.Join(",", definitionIds)},
                {"maxBuildsPerDefinition", 1},
                {"minFinishTime", DateTime.Now.AddDays(-30)},
                {"statusFilter", "completed"}
            };
            string response = await GetResponse("build/builds", args);
            return JsonConvert.DeserializeObject<JsonCollection<Build>>(response);
        }
        public async Task<JsonCollection<TestRun>> GetTestRunsForBuild(int buildId)
        {
            var args = new Dictionary<string, object>
            {
                {"buildUri", $"vstfs%3a%2f%2f%2fBuild%2fBuild%2f{buildId}"},
                {"includeRunDetails", true}
            };

            string response = await GetResponse("test/runs", args);
            return JsonConvert.DeserializeObject<JsonCollection<TestRun>>(response);
        }

        public async Task<JsonCollection<TestResult>> GetTestResultsForRun(int testRunId)
        {
            var args = new Dictionary<string, object> {{"api-version", "1.0"}};

            var response = await GetResponse($"test/runs/{testRunId}/results",args);
            return JsonConvert.DeserializeObject<JsonCollection<TestResult>>(response);
        }

        protected async Task<string> GetResponse(string path)
        {
            return await GetResponse(path, new Dictionary<string, object>());
        }

        protected async Task<string> GetResponse(string path, IDictionary<string, object> arguments, string mediaType = MediaType.JsonMediaType)
        {
            using (HttpClient client = GetHttpClient(mediaType))
            {
                using (HttpResponseMessage response = client.GetAsync(ConstructUrl(path, arguments)).Result)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    CheckResponse(response, responseBody);

                    return responseBody;
                }
            }
        }

        protected virtual string ConstructUrl(string path, IDictionary<string, object> arguments)
        {
            if (!arguments.ContainsKey("api-version"))
            {
                arguments.Add("api-version", ApiVersion);

            }

            StringBuilder resultUrl = new StringBuilder(
                string.IsNullOrEmpty(ProjectName) ?
                    $"{_rootUrl}/{_collection}/_apis/"
                    : $"{_rootUrl}/{_collection}/{HttpUtility.UrlPathEncode(ProjectName)}/_apis/");

            if (!string.IsNullOrEmpty(path))
            {
                resultUrl.AppendFormat("{0}", path);
            }

            resultUrl.AppendFormat("?{0}", string.Join("&", arguments.Where(kvp => kvp.Value != null).Select(kvp =>
            {
                var value = kvp.Value as IEnumerable<string>;
                if (value != null)
                {
                    return string.Join("&", value.Select(v => $"{kvp.Key}={HttpUtility.UrlPathEncode(v)}"));
                }

                return $"{kvp.Key}={HttpUtility.UrlPathEncode(kvp.Value.ToString())}";
            }
                )));
            return resultUrl.ToString();
        }

        private void CheckResponse(HttpResponseMessage response, object responseBody)
        {
            if (!response.IsSuccessStatusCode)
            {
                var s = responseBody as string;
                if (s != null)
                {
                    throw JsonConvert.DeserializeObject<TfsException>(s);
                }

                throw new TfsException($"{response.StatusCode}");
            }

            if (response.StatusCode == HttpStatusCode.NonAuthoritativeInformation)
            {
                throw new TfsException(HttpStatusCode.NonAuthoritativeInformation.ToString());
            }
        }


        private HttpClient GetHttpClient(string mediaType = MediaType.JsonMediaType)
        {
            var httpClientHandler = new HttpClientHandler
            {
                Credentials = _tfsCredential,
                PreAuthenticate = true
            };
            var client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            //_authProvider.ProcessHeaders(client.DefaultRequestHeaders);
            
            return client;
        }



    }
}
