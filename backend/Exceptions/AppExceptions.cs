namespace SchoolAdmission.Api.Exceptions;

public class BadInputException : Exception
{
    public BadInputException(string message) : base(message) { }
}

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
