using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using k8s;
using k8s.Models;

namespace K8sLogStreaming.Pages
{
    public class LogStreamModel : PageModel
    {
        public string Namespace = string.Empty;
        public string Resource = string.Empty;
        public string MetaName = string.Empty;

        public List<string> PodNameList = new List<string>();
        public void OnGet(string Namespace, string Resource, string MetaName)
        {
            this.Namespace = Namespace;
            this.Resource = Resource;
            this.MetaName = MetaName;

            if (Resource.ToLower() == V1Pod.KubeKind.ToLower())
            {
                PodNameList.Add(MetaName);
            }
        }
    }
}
