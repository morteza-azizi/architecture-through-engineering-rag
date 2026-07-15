namespace Rag.Application.Exceptions;

public abstract class DocumentValidationException(string message) : Exception(message);

public sealed class UnsupportedDocumentFormatException(string fileName)
    : DocumentValidationException($"File '{fileName}' is not supported. Only .txt and .md files are accepted.");

public sealed class DocumentTooLargeException(string fileName, long sizeBytes, long maxBytes)
    : DocumentValidationException($"File '{fileName}' ({sizeBytes} bytes) exceeds the maximum allowed size of {maxBytes} bytes.");

public sealed class EmptyDocumentException()
    : DocumentValidationException("The uploaded file is empty.");

public sealed class InvalidDocumentFileNameException(string fileName)
    : DocumentValidationException($"File name '{fileName}' is invalid. Directory paths are not allowed.");
