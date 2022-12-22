﻿using AzureDevOpsAPI.Helpers;
using AzureDevOpsAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text;

namespace AzureDevOpsAPI.Services
{
    public class AzureDevOpsManager : IAzureDevOpsManager
    {
        const string DEVOPS_ORG_URL = "https://dev.azure.com/Demo2212";
        private readonly IConfiguration _config;

        public AzureDevOpsManager(IConfiguration config)
        {
            this._config = config;
        }

        private string GetTokenConfig()
        {
            return _config.GetSection("AzureToken").Value;
        }

        private string GetProjectNameConfig()
        {
            return _config.GetSection("ProjectName").Value;
        }

        private VssConnection Authenticate()
        {
            string token = GetTokenConfig();
            string projectName = GetProjectNameConfig();

            var credentials = new VssBasicCredential(string.Empty, token);
            var connection = new VssConnection(new Uri(DEVOPS_ORG_URL), credentials);

            return connection;
        }

        public List<GitRepository> GetGitRepos()
        {
            var conn = Authenticate();

            using (GitHttpClient gitClient = conn.GetClient<GitHttpClient>())
            {
                var allRepos = gitClient.GetRepositoriesAsync().Result;
                return allRepos;
            }
        }

        public List<PipelineEntity> GetPipelines()
        {
            List<PipelineEntity> pipelines = new List<PipelineEntity>();

            string tokenFormat = string.Format("{0}:{1}", "", GetTokenConfig());
            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(tokenFormat));

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(DEVOPS_ORG_URL + "/" + GetProjectNameConfig() + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                HttpResponseMessage response = client.GetAsync("_apis/pipelines").Result;
                if (response.IsSuccessStatusCode)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    dynamic deserialzedJson = JsonConvert.DeserializeObject<ExpandoObject>(jsonResult, new ExpandoObjectConverter());

                    foreach (var item in (IEnumerable<dynamic>)deserialzedJson.value)
                    {
                        PipelineEntity entity = new PipelineEntity()
                        {
                            Name = item.name,
                            URL = item._links.web.href
                        };

                        pipelines.Add(entity);
                    }
                }

                return pipelines;
            }
        }

        public SprintEntity GetSprintData()
        {
            //https://dev.azure.com/{organization}/{project}/_apis/work/teamsettings/iterations?api-version=6.0

            string tokenFormat = string.Format("{0}:{1}", "", GetTokenConfig());
            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(tokenFormat));

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(DEVOPS_ORG_URL + "/" + GetProjectNameConfig() + "/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                HttpResponseMessage response = client.GetAsync("_apis/work/teamsettings/iterations?api-version=6.0").Result;
                if(response.IsSuccessStatusCode)
                {
                    var jsonResult = response.Content.ReadAsStringAsync().Result;
                    SprintEntity sprintEntity = CustomJsonHelper.GetDeserializedJson<SprintEntity>(jsonResult);

                    return sprintEntity;
                }
            }

            return null;
        }
    }
}
