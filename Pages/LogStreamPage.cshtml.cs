using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace K8sLogStreaming.Pages
{
    public class LogStreamPageModel : PageModel
    {
        public string Namespace = string.Empty;
        public string KubeKind = string.Empty;
        public string MetaName = string.Empty;
        public List<string> PodNameList = new List<string>();
        public void OnGet()
        {
        }
    }
}
