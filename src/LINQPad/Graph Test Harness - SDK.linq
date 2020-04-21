<Query Kind="Program">
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference Prerelease="true">Microsoft.Graph.Auth</NuGetReference>
  <NuGetReference>Microsoft.Identity.Client</NuGetReference>
  <Namespace>Microsoft.Graph</Namespace>
  <Namespace>Microsoft.Graph.Auth</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

private static GraphServiceClient _graphClient;

void Main()
{
	GraphServiceClient graphClient = GetAuthenticatedGraphClient();
	
	graphClient.Users.Request().GetAsync().Result[0].DisplayName.Dump("Graph result");
}

private GraphServiceClient GetAuthenticatedGraphClient()
{
	var authenticationProvider = CreateAuthorizationProvider();
	_graphClient = new GraphServiceClient(authenticationProvider);
	return _graphClient;
}

private static IAuthenticationProvider CreateAuthorizationProvider()
{
	var clientId = Util.GetPassword("clientId");
	var clientSecret = Util.GetPassword("clientSecret");
	var redirectUri = Util.GetPassword("redirectUri");
	var tenantId = Util.GetPassword("tenantId");
	var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

	//this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes
	List<string> scopes = new List<string>();
	scopes.Add("https://graph.microsoft.com/.default");

	var cca = ConfidentialClientApplicationBuilder.Create(clientId)
											.WithAuthority(authority)
											.WithRedirectUri(redirectUri)
											.WithClientSecret(clientSecret)
											.Build();
	return new MsalAuthenticationProvider(cca, scopes.ToArray());
}

// Define other methods and classes here
public class MsalAuthenticationProvider : IAuthenticationProvider
{
	private IConfidentialClientApplication _clientApplication;
	private string[] _scopes;

	public MsalAuthenticationProvider(IConfidentialClientApplication clientApplication, string[] scopes)
	{
		_clientApplication = clientApplication;
		_scopes = scopes;
	}

	/// <summary>
	/// Update HttpRequestMessage with credentials
	/// </summary>
	public async Task AuthenticateRequestAsync(HttpRequestMessage request)
	{
		var token = await GetTokenAsync();
		request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
	}

	/// <summary>
	/// Acquire Token 
	/// </summary>
	public async Task<string> GetTokenAsync()
	{
		AuthenticationResult authResult = null;
		authResult = await _clientApplication.AcquireTokenForClient(_scopes)
							.ExecuteAsync();
		return authResult.AccessToken;
	}
}

public class AuthHandler : DelegatingHandler
{
	private IAuthenticationProvider _authenticationProvider;

	public AuthHandler(IAuthenticationProvider authenticationProvider, HttpMessageHandler innerHandler)
	{
		InnerHandler = innerHandler;
		_authenticationProvider = authenticationProvider;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		await _authenticationProvider.AuthenticateRequestAsync(request);
		return await base.SendAsync(request, cancellationToken);
	}
}