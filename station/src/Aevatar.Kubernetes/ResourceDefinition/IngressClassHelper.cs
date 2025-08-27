using k8s.Models;

namespace Aevatar.Kubernetes.ResourceDefinition;

public class IngressClassHelper
{
    public static V1IngressClass CreateDefaultIngressClassDefinition(string ingressClassName, string controllerName)
    {
        // Define the IngressClass resource with the default class annotation
        var ingressClass = new V1IngressClass
        {
            Metadata = new V1ObjectMeta
            {
                Name = ingressClassName,
                Annotations = new Dictionary<string, string>
                {
                    { "ingressclass.kubernetes.io/is-default-class", "true" }
                }
            },
            Spec = new V1IngressClassSpec
            {
                Controller = controllerName
            }
        };

        return ingressClass;
    }
}