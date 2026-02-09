using System.Diagnostics.CodeAnalysis;

namespace AppEngine.Tools.OperationResults;

public interface IGenericResult<T> : IGenericResult
{
    public T? Content { get; }
    public bool TryGetContent([NotNullWhen(returnValue: true)] out T? content);
}