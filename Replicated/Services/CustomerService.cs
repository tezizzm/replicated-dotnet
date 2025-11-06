using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated.Resources;
using Replicated.Validation;
using System.Text.Json;

namespace Replicated.Services;

/// <summary>
/// Service for managing customers.
/// </summary>
public class CustomerService
{
    private readonly IReplicatedClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerService"/> class.
    /// </summary>
    /// <param name="client">The Replicated client.</param>
    public CustomerService(IReplicatedClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Gets or creates a customer installation by email address.
    /// </summary>
    /// <param name="emailAddress">The installation identifier email address. Must be a valid email format. Represents the software customer installation/environment.</param>
    /// <param name="channel">The channel name (default: "Stable"). Must contain only alphanumeric characters, underscores, hyphens, and spaces.</param>
    /// <param name="name">Optional installation/environment name for identification purposes (e.g., "Production", "Acme Corp - Main Install").</param>
    /// <returns>A <see cref="Customer"/> instance representing the customer installation.</returns>
    /// <exception cref="ArgumentException">Thrown when email address, channel, or name is invalid.</exception>
    /// <exception cref="ReplicatedApiError">Thrown when the API returns an error response.</exception>
    /// <exception cref="ReplicatedNetworkError">Thrown when a network error occurs.</exception>
    /// <remarks>
    /// This method will automatically cache the customer ID and email address in local state storage.
    /// If a different email address is used for a previously cached customer, the state will be cleared.
    /// The method may receive a dynamic token (serviceToken) from the API response which will be cached for future authentication.
    /// </remarks>
    /// <example>
    /// <code>
    /// var customer = client.Customer.GetOrCreate("install@customer.com", "Stable", "Production Environment");
    /// Console.WriteLine($"Customer ID: {customer.CustomerId}");
    /// </code>
    /// </example>
    public Customer GetOrCreate(
        string emailAddress,
        string channel = Constants.DefaultChannel,
        string? name = null)
    {
        // Validate inputs
        var sanitizedEmail = InputValidator.ValidateAndSanitizeEmail(emailAddress);
        InputValidator.ValidateChannel(channel);
        InputValidator.ValidateCustomerName(name);
        
        // Check if customer ID is cached and email matches
        var cachedCustomerId = _client.StateManager.GetCustomerId();
        var cachedEmail = _client.StateManager.GetCustomerEmail();

        if (!string.IsNullOrEmpty(cachedCustomerId) && cachedEmail == sanitizedEmail)
        {
            return new Customer(
                _client,
                cachedCustomerId,
                sanitizedEmail,
                channel);
        }
        else if (!string.IsNullOrEmpty(cachedCustomerId) && cachedEmail != sanitizedEmail)
        {
            _client.StateManager.ClearState();
        }

        // Create or fetch customer
        var response = _client.MakeRequest(
            Constants.HttpMethodPost,
            Constants.CustomerEndpoint,
            _client.GetAuthHeaders(),
            new Dictionary<string, object>
            {
                ["email_address"] = sanitizedEmail,
                ["channel"] = channel,
                ["name"] = name ?? "",
                ["app_slug"] = _client.AppSlug
            });

        var customerDict = response["customer"] as Dictionary<string, object> ?? throw new InvalidOperationException("Invalid customer response");
        var customerId = customerDict["id"]?.ToString() ?? throw new InvalidOperationException("Failed to get customer ID");
        customerDict.TryGetValue("instanceId", out var instanceIdObj);
        var instanceId = instanceIdObj?.ToString();
        
        _client.StateManager.SetCustomerId(customerId);
        _client.StateManager.SetCustomerEmail(sanitizedEmail);
        
        if (!string.IsNullOrEmpty(instanceId))
        {
            _client.StateManager.SetInstanceId(instanceId);
        }

        // Store dynamic token if provided
        if (response.TryGetValue("dynamic_token", out var dynamicToken) && dynamicToken != null)
        {
            _client.StateManager.SetDynamicToken(dynamicToken.ToString()!);
        }
        else if (response.TryGetValue("customer", out var customerObj) && 
                 customerObj is Dictionary<string, object> customerData &&
                 customerData.TryGetValue("serviceToken", out var serviceToken) && 
                 serviceToken != null)
        {
            _client.StateManager.SetDynamicToken(serviceToken.ToString()!);
        }

        var responseData = new Dictionary<string, object>(response);
        responseData.Remove("email_address");

        return new Customer(
            _client,
            customerId,
            sanitizedEmail,
            channel,
            responseData);
    }

    /// <summary>
    /// Gets or creates a customer installation by email address asynchronously.
    /// </summary>
    /// <param name="emailAddress">The installation identifier email address. Must be a valid email format. Represents the software customer installation/environment.</param>
    /// <param name="channel">The channel name (default: "Stable"). Must contain only alphanumeric characters, underscores, hyphens, and spaces.</param>
    /// <param name="name">Optional installation/environment name for identification purposes (e.g., "Production", "Acme Corp - Main Install").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Customer"/> instance representing the customer installation.</returns>
    /// <exception cref="ArgumentException">Thrown when email address, channel, or name is invalid.</exception>
    /// <exception cref="ReplicatedApiError">Thrown when the API returns an error response.</exception>
    /// <exception cref="ReplicatedNetworkError">Thrown when a network error occurs.</exception>
    /// <remarks>
    /// This is the asynchronous version of <see cref="GetOrCreate"/>. Prefer this method in async/await scenarios.
    /// The method will automatically cache the customer ID and email address in local state storage.
    /// </remarks>
    public async Task<Customer> GetOrCreateAsync(
        string emailAddress,
        string channel = Constants.DefaultChannel,
        string? name = null)
    {
        // Validate inputs
        var sanitizedEmail = InputValidator.ValidateAndSanitizeEmail(emailAddress);
        InputValidator.ValidateChannel(channel);
        InputValidator.ValidateCustomerName(name);
        
        // Check if customer ID is cached and email matches
        var cachedCustomerId = _client.StateManager.GetCustomerId();
        var cachedEmail = _client.StateManager.GetCustomerEmail();

        if (!string.IsNullOrEmpty(cachedCustomerId) && cachedEmail == sanitizedEmail)
        {
            return new Customer(
                _client,
                cachedCustomerId,
                sanitizedEmail,
                channel);
        }
        else if (!string.IsNullOrEmpty(cachedCustomerId) && cachedEmail != emailAddress)
        {
            _client.StateManager.ClearState();
        }

        // Create or fetch customer
        var response = await _client.MakeRequestAsync(
            Constants.HttpMethodPost,
            Constants.CustomerEndpoint,
            _client.GetAuthHeaders(),
            new Dictionary<string, object>
            {
                ["email_address"] = sanitizedEmail,
                ["channel"] = channel,
                ["name"] = name ?? "",
                ["app_slug"] = _client.AppSlug
            });

        var customerDict = response["customer"] as Dictionary<string, object> ?? throw new InvalidOperationException("Invalid customer response");
        var customerId = customerDict["id"]?.ToString() ?? throw new InvalidOperationException("Failed to get customer ID");
        customerDict.TryGetValue("instanceId", out var asyncInstanceIdObj);
        var instanceId = asyncInstanceIdObj?.ToString();
        
        _client.StateManager.SetCustomerId(customerId);
        _client.StateManager.SetCustomerEmail(sanitizedEmail);
        
        if (!string.IsNullOrEmpty(instanceId))
        {
            _client.StateManager.SetInstanceId(instanceId);
        }

        // Store dynamic token if provided
        if (response.TryGetValue("dynamic_token", out var dynamicToken) && dynamicToken != null)
        {
            _client.StateManager.SetDynamicToken(dynamicToken.ToString()!);
        }
        else if (response.TryGetValue("customer", out var customerObj) && 
                 customerObj is Dictionary<string, object> customerData &&
                 customerData.TryGetValue("serviceToken", out var serviceToken) && 
                 serviceToken != null)
        {
            _client.StateManager.SetDynamicToken(serviceToken.ToString()!);
        }

        var responseData = new Dictionary<string, object>(response);
        responseData.Remove("email_address");

        return new Customer(
            _client,
            customerId,
            sanitizedEmail,
            channel,
            responseData);
    }
}
