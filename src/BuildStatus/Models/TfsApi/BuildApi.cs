using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Net;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;

namespace BuildStatus.Models.TfsApi
{
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

    public class BuildApi
    {
        private string TfsUsername => ConfigurationManager.AppSettings["TfsUsername"];

        private string TfsPassword => ConfigurationManager.AppSettings["TfsPassword"];

        private string TfsDomain => ConfigurationManager.AppSettings["TfsDomain"];

        private string TfsUrl => ConfigurationManager.AppSettings["TfsUrl"];

        private string TfsProject => ConfigurationManager.AppSettings["TfsProject"];

        private readonly IBuildServer _buildServer;

        public BuildApi()
        {
            var tfsUri = new Uri(TfsUrl);

            var connect = new ConnectByImplementingCredentialsProvider();
            ICredentials iCred = new NetworkCredential(
                TfsUsername,
                TfsPassword,
                TfsDomain);
            connect.GetCredentials(tfsUri, iCred);

            var cred = new TfsClientCredentials(new WindowsCredential(iCred));

            var configurationServer = new TfsConfigurationServer(tfsUri, cred);
            configurationServer.EnsureAuthenticated();

            // Get the catalog of team project collections
            ReadOnlyCollection<CatalogNode> collectionNodes = configurationServer.CatalogNode.QueryChildren(
                new[] { CatalogResourceTypes.ProjectCollection },
                false, CatalogQueryOptions.None);

            // List the team project collections
            foreach (CatalogNode collectionNode in collectionNodes)
            {
                // Use the InstanceId property to get the team project collection
                Guid collectionId = new Guid(collectionNode.Resource.Properties["InstanceId"]);
                TfsTeamProjectCollection teamProjectCollection =
                    configurationServer.GetTeamProjectCollection(collectionId);

                _buildServer = (IBuildServer)teamProjectCollection.GetService(typeof(IBuildServer));
            }
        }

        public Microsoft.TeamFoundation.Build.Client.BuildStatus Status(string build)
        {
            var buildSpec = _buildServer.CreateBuildDetailSpec(TfsProject, build);
            buildSpec.InformationTypes = null; // This line is important for speeding up results
            buildSpec.MinFinishTime = DateTime.Now.AddHours(-72);
            var buildDetails = _buildServer.QueryBuilds(buildSpec).Builds;

            if (buildDetails != null && buildDetails.Any())
            {
                var latestBuildDetail = buildDetails.Last();
                return latestBuildDetail.Status;
            }

            return Microsoft.TeamFoundation.Build.Client.BuildStatus.None;
        }
    }
}