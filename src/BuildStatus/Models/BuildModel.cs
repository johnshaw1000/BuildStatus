namespace BuildStatus.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.Framework.Client;
    using Microsoft.TeamFoundation.Framework.Common;

    public class ConnectByImplementingCredentialsProvider : ICredentialsProvider
    {
        public ICredentials GetCredentials(Uri uri, ICredentials iCredentials)
        {
            return new NetworkCredential("UserName", "Password", "Domain");
        }

        public void NotifyCredentialsAuthenticated(Uri uri)
        {
            throw new ApplicationException("Unable to authenticate");
        }
    }

    public class BuildModel
    {
        public string LatestBuildStatus
        {
            get { return _latestBuildStatus.ToString(); }
        }

        private string TfsUsername
        {
            get { return ConfigurationManager.AppSettings["TfsUsername"]; }
        }

        private string TfsPassword
        {
            get { return ConfigurationManager.AppSettings["TfsPassword"]; }
        }

        private string TfsDomain
        {
            get { return ConfigurationManager.AppSettings["TfsDomain"]; }
        }

        private string TfsUrl
        {
            get { return ConfigurationManager.AppSettings["TfsUrl"]; }
        }

        private string TfsProject
        {
            get { return ConfigurationManager.AppSettings["TfsProject"]; }
        }

        private IEnumerable<string> MonitoredBuilds
        {
            get
            {
                var monitoredBuilds = ConfigurationManager.AppSettings["MonitoredBuilds"];
                return monitoredBuilds.Split(',');
            }
        }

        private BuildStatus _latestBuildStatus = BuildStatus.None;

        public BuildModel()
        {
            Uri tfsUri = new Uri(TfsUrl);

            ConnectByImplementingCredentialsProvider connect = new ConnectByImplementingCredentialsProvider();
            ICredentials iCred = new NetworkCredential(
                TfsUsername,
                TfsPassword,
                TfsDomain);
            connect.GetCredentials(tfsUri, iCred);

            TfsClientCredentials cred = new TfsClientCredentials(new WindowsCredential(iCred));

            TfsConfigurationServer configurationServer =
                new TfsConfigurationServer(tfsUri, cred);
            configurationServer.EnsureAuthenticated();

            // Get the catalog of team project collections
            ReadOnlyCollection<CatalogNode> collectionNodes = configurationServer.CatalogNode.QueryChildren(
                new[] {CatalogResourceTypes.ProjectCollection},
                false, CatalogQueryOptions.None);

            // List the team project collections
            foreach (CatalogNode collectionNode in collectionNodes)
            {
                // Use the InstanceId property to get the team project collection
                Guid collectionId = new Guid(collectionNode.Resource.Properties["InstanceId"]);
                TfsTeamProjectCollection teamProjectCollection =
                    configurationServer.GetTeamProjectCollection(collectionId);

                IBuildServer buildServer = (IBuildServer) teamProjectCollection.GetService(typeof (IBuildServer));

                foreach (var build in MonitoredBuilds)
                {
                    var buildSpec = buildServer.CreateBuildDetailSpec(TfsProject, build);
                    buildSpec.InformationTypes = null; // This line is important for speeding up results
                    buildSpec.MinFinishTime = DateTime.Now.AddHours(-24);
                    var buildDetails = buildServer.QueryBuilds(buildSpec).Builds;

                    if (buildDetails != null && buildDetails.Any())
                    {
                        var latestBuildDetail = buildDetails.Last();
                        SetBuildStatus(latestBuildDetail.Status);
                    }
                }
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