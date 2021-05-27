﻿using System;
using System.Collections.Generic;
using System.Linq;
using Git.hub.Errors;
using Git.hub.util;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serialization.Json;

namespace Git.hub
{
    public class Repository
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string Homepage { get; internal set; }
        public string DefaultBranch { get; internal set; }
        public User Owner { get; internal set; }
        public bool Fork { get; internal set; }
        public int Forks { get; internal set; }
        public bool Private { get; internal set; }
        public Organization Organization { get; internal set; }

        private Repository _Parent;
        public Repository Parent
        {
            get
            {
                if (!Detailed)
                    throw new NotSupportedException();
                return _Parent;
            }
            private set
            {
                _Parent = value;
            }
        }

        /// <summary>
        /// Read-only clone url
        /// git://github.com/{user}/{repo}.git
        /// </summary>
        public string GitUrl { get; internal set; }

        /// <summary>
        /// Read/Write clone url via SSH
        /// git@github.com/{user}/{repo.git}
        /// </summary>
        public string SshUrl { get; internal set; }

        /// <summary>
        /// Read/Write clone url via HTTPS
        /// https://github.com/{user}/{repo}.git
        /// </summary>
        public string CloneUrl { get; internal set; }

        internal RestClient _client;

        /// <summary>
        /// true if fetched from github.com/{user}/{repo}, false if from github.com/{user}
        /// </summary>
        public bool Detailed { get; internal set; }

        /// <summary>
        /// Forks this repository into your own account.
        /// </summary>
        /// <returns></returns>
        public Repository CreateFork()
        {
            RestRequest request = new RestRequest("/repos/{user}/{repo}/forks");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            var response = _client.Post<Repository>(request);

            if (!response.IsSuccessful)
            {
                ManageError(response);
            }
            Repository forked = response.Data;
            forked._client = _client;
            return forked;
        }

        /// <summary>
        /// Lists all branches
        /// </summary>
        /// <remarks>Not really sure if that's even useful, mind the 'git branch'</remarks>
        /// <returns>list of all branches</returns>
        public IList<Branch> GetBranches()
        {
            RestRequest request = new RestRequest("/repos/{user}/{repo}/branches");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            return _client.GetList<Branch>(request);
        }

        /// <summary>
        /// Retrieves the name of the default branch
        /// </summary>
        /// <returns>The name of the default branch</returns>
        public string GetDefaultBranch()
        {
            RestRequest request = new RestRequest("/repos/{user}/{repo}");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            var response = _client.Get<Repository>(request);

            if (!response.IsSuccessful)
            {
                ManageError(response);
            }

            var repo = response.Data;

            return repo.DefaultBranch;
        }

        /// <summary>
        /// Lists all open pull requests
        /// </summary>
        /// <returns>list of all open pull requests</returns>
        public IList<PullRequest> GetPullRequests()
        {
            var request = new RestRequest("/repos/{user}/{repo}/pulls");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);

            var list = _client.GetList<PullRequest>(request);
            if (list == null)
                return null;

            list.ForEach(pr => { pr._client = _client; pr.Repository = this; });
            return list;
        }

        /// <summary>
        /// Returns a single pull request.
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>the single pull request</returns>
        public PullRequest GetPullRequest(int id)
        {
            var request = new RestRequest("/repos/{user}/{repo}/pulls/{pull}");
            request.AddUrlSegment("user", Owner.Login);
            request.AddUrlSegment("repo", Name);
            request.AddUrlSegment("pull", id.ToString());

            var response = _client.Get<PullRequest>(request);

            if (!response.IsSuccessful)
            {
                ManageError(response);
            }

            var pullrequest = response.Data;

            pullrequest._client = _client;
            pullrequest.Repository = this;
            return pullrequest;
        }
        /// <summary>
        /// Creates a new pull request
        /// </summary>
        /// <param name="headBranch">branch in the own repository, like mabako:new-awesome-thing</param>
        /// <param name="baseBranch">branch it should be merged into in the original repository, like master</param>
        /// <param name="title">title of the request</param>
        /// <param name="body">body/message</param>
        /// <returns></returns>
        public PullRequest CreatePullRequest(string headBranch, string baseBranch, string title, string body)
        {
            var request = new RestRequest("/repos/{name}/{repo}/pulls");
            request.AddUrlSegment("name", Owner.Login);
            request.AddUrlSegment("repo", Name);

            request.RequestFormat = DataFormat.Json;
            request.JsonSerializer = new ReplacingJsonSerializer("\"x__custom__base\":\"", "\"base\":\"");
            request.AddJsonBody(new
            {
                title = title,
                body = body,
                head = headBranch,
                x__custom__base = baseBranch
            });

            var response = _client.Post<PullRequest>(request);

            if (!response.IsSuccessful)
            {
                ManageError(response);
            }

            var pullrequest = response.Data;

            pullrequest._client = _client;
            pullrequest.Repository = this;
            return pullrequest;
        }

        public GitHubReference GetRef(string refName)
        {
            var request = new RestRequest("/repos/{owner}/{repo}/git/refs/{ref}");
            request.AddUrlSegment("owner", Owner.Login);
            request.AddUrlSegment("repo", Name);
            request.AddUrlSegment("ref", refName);

            var response = _client.Get<GitHubReference>(request);

            if (!response.IsSuccessful)
            {
                ManageError(response);
            }

            var ghRef = response.Data;

            ghRef._client = _client;
            ghRef.Repository = this;
            return ghRef;
        }

        /// <summary>
        /// Creates a new issue
        /// </summary>
        /// <param name="title">title</param>
        /// <param name="body">body</param>
        /// <returns>the issue if successful, null otherwise</returns>
        public Issue CreateIssue(string title, string body)
        {
            var request = new RestRequest("/repos/{owner}/{repo}/issues");
            request.AddUrlSegment("owner", Owner.Login);
            request.AddUrlSegment("repo", Name);

            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(new
            {
                title = title,
                body = body
            });

            var response = _client.Post<Issue>(request);

            if (!response.IsSuccessful)
            {
                ManageError(response);
            }

            var issue = response.Data;

            issue._client = _client;
            issue.Repository = this;
            return issue;
        }

        public override bool Equals(object obj)
        {
            return obj is Repository && GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() + ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Owner.Login + "/" + Name;
        }

        public void ManageError(IRestResponse response)
        {
            var error = JsonConvert.DeserializeObject<ApiError>(response.Content);

            var errorContent = string.Join("\r\n", error?.errors?.Select(e => e.message) ?? new string[0]);

            if (string.IsNullOrEmpty(errorContent))
            {
                errorContent = error.message;
            }

            throw new Exception(errorContent);
        }
    }
}
