using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Identity.Web;
using Microsoft.Graph; 

namespace FunctionApp1
{
	public class Function1
	{
		private readonly ITokenAcquisition _tokenAcquisition;
		private readonly GraphServiceClient _graphServiceClient;

		public Function1(ITokenAcquisition tokenAcquisition, GraphServiceClient graphServiceClient)
		{
			_tokenAcquisition = tokenAcquisition;
			_graphServiceClient = graphServiceClient;
		}

		[FunctionName("Function1")]
		public async Task<IActionResult> Run(
						[HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
						ILogger log)
		{
			log.LogInformation("C# HTTP trigger function processed a request.");

			var (authenticationStatus, authenticationResponse) =
					await req.HttpContext.AuthenticateAzureFunctionAsync();
			if (!authenticationStatus) return authenticationResponse;

			var token = await _tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default");

			string name = req.HttpContext.User.Identity.IsAuthenticated ? req.HttpContext.User.Identity.Name : null;

			string responseMessage = string.IsNullOrEmpty(name)
					? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
					: $"Hello, {name}. This HTTP triggered function executed successfully.";

			return new OkObjectResult(responseMessage);
		}

		[FunctionName("Function2")]
		public async Task<IActionResult> Run2(
						[HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
						ILogger log)
		{
			log.LogInformation("C# HTTP trigger function processed a request.");

			var (authenticationStatus, authenticationResponse) =
					await req.HttpContext.AuthenticateAzureFunctionAsync();
			if (!authenticationStatus) return authenticationResponse;

			var token = await _tokenAcquisition.GetAccessTokenForAppAsync("https://graph.microsoft.com/.default");

			//string name = req.HttpContext.User.Identity.IsAuthenticated ? req.HttpContext.User.Identity.Name : null;

			//string responseMessage = string.IsNullOrEmpty(name)
			//		? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
			//		: $"Hello, {name}. This HTTP triggered function executed successfully.";

			var me = await _graphServiceClient.Me.Request().GetAsync();

			return new OkObjectResult(me);
		}
	}
}
