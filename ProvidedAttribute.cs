namespace NSubstitute.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class ProvidedAttribute : Attribute {
	public Type? As { get; set; }
}
