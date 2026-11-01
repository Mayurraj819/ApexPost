namespace ApexPost.Application.Exceptions;
public sealed class ConcurrencyConflictException(string message, Exception? inner = null) : Exception(message, inner);
public sealed class UniqueConflictException(string message, Exception? inner = null) : Exception(message, inner);
