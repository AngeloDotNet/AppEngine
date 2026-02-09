namespace AppEngine.Tools.OperationResults;

public record class StreamFileContent(Stream Content, string ContentType, string? DownloadFileName = null);

public record class ByteArrayFileContent(byte[] Content, string ContentType, string? DownloadFileName = null);