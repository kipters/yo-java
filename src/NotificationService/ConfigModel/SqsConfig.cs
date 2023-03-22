using System.ComponentModel.DataAnnotations;

namespace NotificationService.ConfigModel;

public class SqsConfig : IValidatableObject
{
    public Uri QueueUrl { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (QueueUrl is null)
        {
            yield return new("QueueUrl is required", new[] { nameof(QueueUrl) });
        }
    }
}
