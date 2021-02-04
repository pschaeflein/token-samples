using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Graph.Community.Samples
{
	public static class Diagnostics
	{
		public static async Task Run()
		{
			/////////////////////////////
			//
			// Programmer configuration
			//
			/////////////////////////////

			var sharepointDomain = "demo.sharepoint.com";
			var siteCollectionPath = "/sites/SiteGroupsTest";

			/////////////////
			//
			// Configuration
			//
			/////////////////

			AzureAdOptions azureAdOptions = new AzureAdOptions();

			var settingsFilename = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
			var builder = new ConfigurationBuilder()
													.AddJsonFile(settingsFilename, optional: false);
			var config = builder.Build();
			config.Bind("AzureAd", azureAdOptions);

			////////////////////////////////////////
			//
			// Capture all diagnostic information
			//
			///////////////////////////////////////

			// Start with an IHttpMessageLogger that will write to a StringBuilder 
			var logger = new StringBuilderHttpMessageLogger();
			/*
       *  Could also use the Console if preferred...
       *  
       *  var logger = new ConsoleHttpMessageLogger();
       */


			// MSAL provides logging via a callback on the client application.
			//  Write those entries to the same logger, prefixed with MSAL
			async void MSALLogging(LogLevel level, string message, bool containsPii)
			{
				await logger.WriteLine($"MSAL {level} {containsPii} {message}");
			}


			// GraphCommunity uses an EventSource to publish diagnostics in the handler.
			//    This follows the pattern used by the Azure SDK.
			var listener = new Community.Diagnostics.GraphCommunityEventSourceListener(async (args, message) =>
			{
				if (args.EventSource.Name.StartsWith("Graph-Community"))
				{
					// create a dictionary of the properties of the args object
					var properties = args.PayloadNames
														.Zip(args.Payload, (string k, object v) => new { Key = k, Value = v })
														.ToDictionary(x => x.Key, x => x.Value.ToString());

					// log the message and payload, prefixed with COMM
					var traceMessage = string.Format(args.Message, args.Payload.ToArray());
					await logger.WriteLine($"COMM {traceMessage}");
				}
			}, System.Diagnostics.Tracing.EventLevel.LogAlways);



			// Use the system browser to login
			//  https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/System-Browser-on-.Net-Core#how-to-use-the-system-browser-ie-the-default-browser-of-the-os

			var options = new PublicClientApplicationOptions()
			{
				AadAuthorityAudience = AadAuthorityAudience.AzureAdMyOrg,
				AzureCloudInstance = AzureCloudInstance.AzurePublic,
				ClientId = azureAdOptions.ClientId,
				TenantId = azureAdOptions.TenantId,
				RedirectUri = "http://localhost"
			};

			// Create the public client application (desktop app), with a default redirect URI
			var pca = PublicClientApplicationBuilder.CreateWithApplicationOptions(options)
					.WithLogging(MSALLogging, LogLevel.Verbose, true, true)
					.Build();

			// Enable a simple token cache serialiation so that the user does not need to
			// re-sign-in each time the application is run
			TokenCacheHelper.EnableSerialization(pca.UserTokenCache);


			// Create an authentication provider to attach the token to requests
			var scopes = new string[] { "https://graph.microsoft.com/User.ReadBasic.All" };
			IAuthenticationProvider ap = new InteractiveAuthenticationProvider(pca, scopes);


			////////////////////////////////////////////////////////////////
			//
			//  Create a GraphClient with the SharePoint service handler
			//
			////////////////////////////////////////////////////////////////

			// Configure our client
			CommunityGraphClientOptions clientOptions = new CommunityGraphClientOptions()
			{
				UserAgent = "ExtendedCapabilitiesSample"
			};

			var graphServiceClient = CommunityGraphClientFactory.Create(clientOptions, logger, ap);


			////////////////////////////
			//
			// Setup is complete, run the sample
			//
			////////////////////////////

			try
			{
				var me = await graphServiceClient
										.Me
										.Request()
										.WithScopes(new string[] { "https://graph.microsoft.com/User.Read" })
										.GetAsync();

				Console.WriteLine($"Me.DisplayName: {me.DisplayName}");


				var account = pca.GetAccountsAsync().Result.First();
				var fnScope = new string[] { "api://ed3e30c0-0f00-4150-82f1-1c5cee58a0d7/access_as_user" };

				AuthenticationResult authResult = default;
				try
				{
					authResult = await pca.AcquireTokenSilent(fnScope, account).ExecuteAsync();
				}
				catch (MsalUiRequiredException uiReqEx)
				{
					authResult = await pca.AcquireTokenInteractive(fnScope).ExecuteAsync();
				}
				catch (Exception ex)
				{
					Console.Write(ex.ToString());
				}


				Console.WriteLine(authResult.AccessToken);
			}
			catch (Exception ex)
			{
				await logger.WriteLine("");
				await logger.WriteLine("================== Exception caught ==================");
				await logger.WriteLine(ex.ToString());
			}


			Console.WriteLine("Press enter to show log");
			Console.ReadLine();
			Console.WriteLine();
			var log = logger.GetLog();
			Console.WriteLine(log);
		}
	}

}

