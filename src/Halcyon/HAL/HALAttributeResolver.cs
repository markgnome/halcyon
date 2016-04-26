using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Halcyon.HAL.Attributes;

namespace Halcyon.HAL
{
    internal class HALAttributeResolver
    {
        internal static void ResolveAttributes(HALResponse halResponse, object model)
        {
            var classAttributes = Attribute.GetCustomAttributes(model.GetType());
            foreach (var attribute in classAttributes)
            {
                var modelAttribute = attribute as HalModelAttribute;
                if (modelAttribute != null)
                {
                    if (modelAttribute.ForceHal.HasValue)
                    {
                        halResponse.Config.ForceHAL = modelAttribute.ForceHal.Value;
                    }
                    if (modelAttribute.LinkBase != null)
                    {
                        halResponse.Config.LinkBase = modelAttribute.LinkBase;
                    }
                }
                var linkAttribute = attribute as HalLinkAttribute;
                if (linkAttribute != null)
                {
                    halResponse.AddLinks(new Link(linkAttribute.Rel, linkAttribute.Href, linkAttribute.Title, linkAttribute.Method));
                }
            }

            var modelProperties = model.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(HalPropertyAttribute)));
            foreach (var propertyInfo in modelProperties)
            {
                var modelValue = propertyInfo.GetValue(model);
                if(modelValue == null) continue;
                var embeddAttribute = propertyInfo.GetCustomAttribute(typeof(HalEmbeddedAttribute)) as HalEmbeddedAttribute;
                if(embeddAttribute == null) continue;

                var embeddedItems = modelValue as IEnumerable<object>;
                if (embeddedItems == null)
                {
                    //Allow non enumerable properties as single value enumerables
                    embeddedItems = new List<object> { modelValue };
                }

                if (embeddedItems.Any())
                {
                    halResponse.AddEmbeddedCollection(embeddAttribute.CollectionName,
                        embeddedItems.Select(embeddedModel => new HALResponse(embeddedModel, halResponse.Config)));
                }
            }
        }
    }
}