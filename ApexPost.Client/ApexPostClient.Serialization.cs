using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApexPost.Client;
// Extension point supplied by NSwag; generated files remain untouched.
public partial class ApexPostClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
        settings.Converters.Add(new JsonStringEnumConverter());
    }
}
