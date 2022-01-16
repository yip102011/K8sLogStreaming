using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using k8s;
using k8s.Models;

namespace K8sLogStreaming.Pages
{
    public class LogStreamModel : PageModel
    {
        private readonly IKubernetes _kubeClient;
        public LogStreamModel(IKubernetes kubeClient)
        {
            _kubeClient = kubeClient;
        }


        public string Namespace = string.Empty;
        public string KubeKind = string.Empty;
        public string MetaName = string.Empty;
        public List<string> PodNameList = new List<string>();
        public async Task OnGetAsync(string Namespace, string KubeKind, string MetaName)
        {
            this.Namespace = Namespace;
            this.KubeKind = KubeKind;
            this.MetaName = MetaName;

            if (KubeKind.ToLower() == V1Pod.KubeKind.ToLower())
            {
                PodNameList.Add(MetaName);
            }

            if (KubeKind.ToLower() == V1Deployment.KubeKind.ToLower())
            {
                var k8sObjList = (await _kubeClient.ListNamespacedDeploymentAsync(Namespace));
                var k8sObj = k8sObjList.Items.First(d => d.Metadata.Name == MetaName);
                PodNameList = await GetPodNameListAsync(k8sObj.Spec.Selector);
            }

            if (KubeKind.ToLower() == V1DaemonSet.KubeKind.ToLower())
            {
                var k8sObjList = (await _kubeClient.ListNamespacedDaemonSetAsync(Namespace));
                var k8sObj = k8sObjList.Items.First(d => d.Metadata.Name == MetaName);
                PodNameList = await GetPodNameListAsync(k8sObj.Spec.Selector);
            }

            if (KubeKind.ToLower() == V1StatefulSet.KubeKind.ToLower())
            {
                var k8sObjList = (await _kubeClient.ListNamespacedStatefulSetAsync(Namespace));
                var k8sObj = k8sObjList.Items.First(d => d.Metadata.Name == MetaName);
                PodNameList = await GetPodNameListAsync(k8sObj.Spec.Selector);
            }
        }
        public async Task<List<string>> GetPodNameListAsync(V1LabelSelector selector)
        {
            var labelMatchs = selector.MatchLabels.Select(pair => pair.Key + "=" + pair.Value);
            var labelStr = string.Join(',', labelMatchs);
            var podList = await _kubeClient.ListNamespacedPodAsync(Namespace, labelSelector: labelStr);
            var podNameList = podList.Items.Select(pod => pod.Metadata.Name).ToList();
            return podNameList;
        }
    }
}
