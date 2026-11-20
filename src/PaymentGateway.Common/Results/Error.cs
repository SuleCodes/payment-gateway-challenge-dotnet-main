namespace PaymentGateway.Common.Results;

/// <summary>
/// An expected, handled failure (as opposed to an exception, which signals a bug or an unexpected fault).
/// </summary>
public sealed record Error(string Code, string Message);
