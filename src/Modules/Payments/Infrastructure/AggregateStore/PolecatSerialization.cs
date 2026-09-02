using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace CompanyName.MyMeetings.Modules.Payments.Infrastructure.AggregateStore;

internal static class PolecatSerialization
{
    internal static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { EnableGetOnlyProperties }
        };
    }

    private static void EnableGetOnlyProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (property.Set != null)
            {
                continue;
            }

            if (property.AttributeProvider is not PropertyInfo propertyInfo)
            {
                continue;
            }

            var backingField = typeInfo.Type.GetField(
                $"<{propertyInfo.Name}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (backingField is null)
            {
                continue;
            }

            property.Set = (obj, value) => backingField.SetValue(obj, value);
        }
    }
}
