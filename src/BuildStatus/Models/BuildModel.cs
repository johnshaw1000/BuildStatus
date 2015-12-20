using System.Linq;
using System.Net;
using BuildQuery.TfsData.Models.TfsRESTApi;
using BuildStatus.Models.TfsRESTApi;

namespace BuildStatus.Models
{
    using TfsApi;
    using System.Collections.Generic;
    using System.Configuration;
    using Microsoft.TeamFoundation.Build.Client;

    public class BuildModel
    {
        private BuildStatus _latestBuildStatus = BuildStatus.None;

        public string LatestBuildStatus
        {
            get { return _latestBuildStatus.ToString(); }
        }

        private IEnumerable<string> MonitoredBuilds
        {
            get
            {
                var monitoredBuilds = ConfigurationManager.AppSettings["MonitoredBuilds"];
                return monitoredBuilds.Split(',');
            }
        }

        public BuildModel()
        {
            var buildStatusTfsApi = new BuildApi();

            var tfsAccessCredential = new NetworkCredential(ConfigurationManager.AppSettings["TfsUsername"], ConfigurationManager.AppSettings["TfsPassword"], ConfigurationManager.AppSettings["TfsDomain"]);
            IHttpRequestHeaderFilter filter = new BasicAuthenticationFilter(tfsAccessCredential);
            var buildStatusRestApi = new TfsRestApiManager(ConfigurationManager.AppSettings["TfsUrl"], ConfigurationManager.AppSettings["TfsCollection"], tfsAccessCredential);


            foreach (var build in MonitoredBuilds)
            {
                var buildStatus = buildStatusTfsApi.Status(build);

                if (buildStatus == BuildStatus.None)
                {
                    buildStatus = GetRestApiBuildStatus(buildStatusRestApi, build);
                }

                SetBuildStatus(buildStatus);
            }
        }

        private BuildStatus GetRestApiBuildStatus(TfsRestApiManager buildStatusRestApi, string build)
        {
            var defList = buildStatusRestApi.GetBuildDefinitions("build", build).Result;

            if (defList.Count == 0)
            {
                return BuildStatus.None;
            }

            var buildList = buildStatusRestApi.GetLatestBuilds(defList.Items.Where(y => y.Id.HasValue).Select(x => x.Id.Value)).Result;

            if (buildList.Count == 0)
            {
                return BuildStatus.None;
            }

            if (buildList[0].Result.Equals("succeeded"))
            {
                return BuildStatus.Succeeded;
            }
            else if (buildList[0].Result.Equals("partiallySucceeded"))
            {
                return BuildStatus.PartiallySucceeded;
            }
            else if (buildList[0].Result.Equals("failed"))
            {
                return BuildStatus.Failed;
            }
            else if (buildList[0].Result.Equals("canceled"))
            {
                return BuildStatus.Stopped;
            }
            else
            {
                return BuildStatus.None;
            }
        }

        private void SetBuildStatus(BuildStatus status)
        {
            if ((status == BuildStatus.Succeeded || status == BuildStatus.PartiallySucceeded ||
                 status == BuildStatus.Failed) && status > _latestBuildStatus)
            {
                _latestBuildStatus = status;
            }
        }
    }
}