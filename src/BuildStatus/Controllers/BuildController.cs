namespace BuildStatus.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Web.Http;
    using Models;

    public class BuildController : ApiController
    {
        [HttpGet]
        [Route("api/build/status")]
        public IHttpActionResult GetStatus()
        {
            try
            {
                var buildModel = new BuildModel();

                return Ok(buildModel.LatestBuildStatus);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/build/builds")]
        public IHttpActionResult GetBuilds()
        {
            try
            {
                return Ok(MonitoredBuilds);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        private IEnumerable<string> MonitoredBuilds
        {
            get
            {
                var monitoredBuilds = ConfigurationManager.AppSettings["MonitoredBuilds"];
                return monitoredBuilds.Split(',');
            }
        }
    }
}
