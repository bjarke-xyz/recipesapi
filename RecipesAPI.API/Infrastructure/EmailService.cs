using Newtonsoft.Json;

namespace RecipesAPI.API.Infrastructure;

public interface IEmailService
{
    Task SendEmail(string to, string subject, string content);
}

public class EmailInput
{
    [JsonProperty("from")]
    public EmailInputFrom From { get; set; } = new EmailInputFrom();
    [JsonProperty("to")]
    public EmailInputTo To { get; set; } = new EmailInputTo();
    [JsonProperty("subject")]
    public string Subject { get; set; } = default!;
    [JsonProperty("content")]
    public List<EmailInputContent> Content { get; set; } = new List<EmailInputContent>();
}
public class EmailInputFrom
{
    [JsonProperty("email")]
    public string Email { get; set; } = default!;
    [JsonProperty("name")]
    public string Name { get; set; } = default!;
}
public class EmailInputTo
{
    [JsonProperty("email")]
    public string Email { get; set; } = default!;
}
public class EmailInputContent
{
    [JsonProperty("type")]
    public string Type { get; set; } = default!;
    [JsonProperty("value")]
    public string Value { get; set; } = default!;
}
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> logger;
    private readonly string apiUrl;
    private readonly HttpClient httpClient;
    private readonly string apiUser;
    private readonly string apiPassword;
    public EmailService(string apiUrl, string apiUser, string apiPassword, ILogger<EmailService> logger)
    {
        this.apiUrl = apiUrl;
        this.apiUser = apiUser;
        this.apiPassword = apiPassword;
        httpClient = new HttpClient();
        this.logger = logger;
    }
    public async Task SendEmail(string to, string subject, string content)
    {
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(content);

        var input = new EmailInput
        {
            From = new EmailInputFrom { Email = "gastrik@bjarke.xyz", Name = "Gastrik" },
            To = new EmailInputTo { Email = to },
            Subject = subject,
            Content = new List<EmailInputContent>{
                new EmailInputContent{
                    Type = "text/plain",
                    Value = content
                }
            }
        };
        var authString = $"{apiUser}:{apiPassword}";
        var base64EncodedAuthString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authString));
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{apiUrl}/email"),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonConvert.SerializeObject(input), System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Authorization", $"Basic {base64EncodedAuthString}");

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to send email to {to}", to);
            throw;
        }
    }
}