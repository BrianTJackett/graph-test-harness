<Query Kind="Program">
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference>Microsoft.Identity.Client</NuGetReference>
  <Namespace>Microsoft.Graph</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

private static HttpClient _httpClient;

void Main()
{
	HttpClient httpClient = GetAuthenticatedHTTPClient();

	// if prefer to get access token directly, uncomment below lines and the corresponding GetAccessToken() method and AuthResult class
	//var accessToken = GetAccessToken();
	//httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {accessToken}");

	var version = "v1.0";
	var graphRequestUrl = $"https://graph.microsoft.com/{version}/users?$top=1";
	
	httpClient.GetStringAsync(graphRequestUrl).Result.Dump("Http request result");
}

private static HttpClient GetAuthenticatedHTTPClient()
{
	var authenticationProvider = CreateAuthorizationProvider();
	_httpClient = new HttpClient(new AuthHandler(authenticationProvider, new HttpClientHandler()));
	return _httpClient;
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

// if prefer to get access token directly, uncomment GetAccessToken() method and AuthResult class
//public string GetAccessToken()
//{
//	var tenantId = Util.GetPassword("tenantId");
//	var authTokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
//	HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, authTokenUrl);
//
//	var pairs = new List<KeyValuePair<string, string>>
//	{
//		new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
//		new KeyValuePair<string, string>("client_id", Util.GetPassword("ClientId")),
//		new KeyValuePair<string, string>("client_secret", Util.GetPassword("ClientSecret")),
//		new KeyValuePair<string, string>("grant_type", "client_credentials"),
//	};
//
//	message.Content = new FormUrlEncodedContent(pairs);
//
//	var result = _httpClient.SendAsync(message).Result;
//	var JsonResponse = result.Content.ReadAsStringAsync().Result;
//
//	return (JsonSerializer.Deserialize<AuthResult>(JsonResponse).AccessToken);
//}
//
//public class AuthResult
//{
//	[JsonPropertyName("token_type")]
//	public string TokenType {get; set;}
//
//	[JsonPropertyName("expires_in")]
//	public int ExpiresIn {get; set;}
//
//	[JsonPropertyName("ext_expires_in")]
//	public int ExtExpiresIn { get; set; }
//
//	[JsonPropertyName("access_token")]
//	public string AccessToken { get; set; }
//}