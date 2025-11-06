using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Replicated.Services;

namespace Replicated.Resources;

/// <summary>
/// Represents a Replicated customer installation/environment.
/// </summary>
public class Customer
{
    private readonly IReplicatedClient _client;
    private readonly Dictionary<string, object> _data;

    /// <summary>
    /// Gets the customer ID.
    /// </summary>
    public string CustomerId { get; }

    /// <summary>
    /// Gets the installation identifier email address.
    /// </summary>
    public string EmailAddress { get; }

    /// <summary>
    /// Gets the channel.
    /// </summary>
    public string? Channel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Customer"/> class.
    /// </summary>
    /// <param name="client">The Replicated client.</param>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="channel">The channel.</param>
    /// <param name="data">Additional customer data.</param>
    public Customer(
        IReplicatedClient client,
        string customerId,
        string emailAddress,
        string? channel = null,
        Dictionary<string, object>? data = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
        EmailAddress = emailAddress ?? throw new ArgumentNullException(nameof(emailAddress));
        Channel = channel;
        _data = data ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets or creates an instance for this customer.
    /// </summary>
    /// <returns>An <see cref="Instance"/> object that can be used to send metrics and manage instance status.</returns>
    /// <remarks>
    /// The instance ID is automatically retrieved from cached state if available.
    /// If no instance exists, one will be created automatically when needed (e.g., when sending metrics).
    /// </remarks>
    public Instance GetOrCreateInstance()
    {
        return new Instance(_client, CustomerId);
    }

    /// <summary>
    /// Gets or creates an instance for this customer asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="Instance"/> object.</returns>
    /// <remarks>
    /// This is the asynchronous version of <see cref="GetOrCreateInstance"/>. 
    /// Prefer this method in async/await scenarios, though the operation itself is currently synchronous.
    /// </remarks>
    public Task<Instance> GetOrCreateInstanceAsync()
    {
        return Task.FromResult(new Instance(_client, CustomerId));
    }

    /// <summary>
    /// Gets additional customer data by key.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>The data value if found, otherwise null.</returns>
    public object? GetData(string key)
    {
        return _data.TryGetValue(key, out var value) ? value : null;
    }
}
