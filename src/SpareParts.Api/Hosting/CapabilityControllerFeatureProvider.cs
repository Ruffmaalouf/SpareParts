using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace SpareParts.Api.Hosting;

internal sealed class CapabilityControllerFeatureProvider(HashSet<string> allowedControllers)
    : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        for (var index = feature.Controllers.Count - 1; index >= 0; index--)
        {
            var controller = feature.Controllers[index];
            if (!allowedControllers.Contains(controller.Name))
            {
                feature.Controllers.RemoveAt(index);
            }
        }
    }
}
