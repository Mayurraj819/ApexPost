namespace ApexPost.Application.Exceptions;
public sealed class DomainRuleException(string message, Exception? inner = null) : Exception(message, inner);
