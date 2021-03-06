using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace K8sLogStreaming.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KubeController : ControllerBase
    {
        private readonly IKubernetes _kubeClient;
        public KubeController(IKubernetes kubeClient)
        {
            _kubeClient = kubeClient;
        }

        [HttpGet]
        [Route("namespace/list")]
        public async Task<IActionResult> NamespaceList()
        {
            var kubeObjList = await _kubeClient.ListNamespaceAsync();
            var simList = kubeObjList.Items.Select(pod => ConvertToSimObj(pod));
            return Ok(simList);
        }

        [HttpGet]
        [Route("pod/list")]
        public async Task<IActionResult> PodList(string KubeNamespace, string KubeKind, string MetaName)
        {
            //[FromRoute] K8sSimpleObj k8sSimpleObj
            var ns = KubeNamespace.ToLower();
            var kind = KubeKind.ToLower();
            var name = MetaName.ToLower();

            if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(name))
            { return BadRequest(new { message = "" }); }

            var podList = new List<K8sSimpleObj>();

            if (kind == "pod") { podList.Add(new K8sSimpleObj { KubeNamespace = ns, KubeKind = kind, MetaName = name }); }

            if (kind == "deployment")
            {
                var k8sObjList = (await _kubeClient.ListNamespacedDeploymentAsync(ns, fieldSelector: $"metadata.name={name}"));
                var k8sObj = k8sObjList.Items.First(d => d.Metadata.Name == name);
                podList = (await GetPodListAsync(ns, k8sObj.Spec.Selector)).ToList();
            }
            if (kind == "daemonset")
            {
                var k8sObjList = (await _kubeClient.ListNamespacedDaemonSetAsync(ns, fieldSelector: $"metadata.name={name}"));
                var k8sObj = k8sObjList.Items.First(d => d.Metadata.Name == name);
                podList = (await GetPodListAsync(ns, k8sObj.Spec.Selector)).ToList();
            }
            if (kind == "statefulset")
            {
                var k8sObjList = (await _kubeClient.ListNamespacedStatefulSetAsync(ns, fieldSelector: $"metadata.name={name}"));
                var k8sObj = k8sObjList.Items.First(d => d.Metadata.Name == name);
                podList = (await GetPodListAsync(ns, k8sObj.Spec.Selector)).ToList();
            }

            return Ok(podList);
        }

        private async Task<IEnumerable<K8sSimpleObj>> GetPodListAsync(string KubeNamespace, V1LabelSelector selector)
        {
            var labelMatchs = selector.MatchLabels.Select(pair => pair.Key + "=" + pair.Value);
            var labelStr = string.Join(',', labelMatchs);
            var k8sPodList = await _kubeClient.ListNamespacedPodAsync(KubeNamespace, labelSelector: labelStr);
            var podList = k8sPodList.Items.Select(pod => ConvertToSimObj(pod));
            return podList;
        }

        private static K8sSimpleObj ConvertToSimObj<T>(T kubeObj) where T : IKubernetesObject, IMetadata<V1ObjectMeta>
        {
            var result = new K8sSimpleObj
            {
                KubeKind = kubeObj.GetKubernetesTypeMetadata().Kind,
                KubeNamespace = kubeObj.Namespace(),
                MetaName = kubeObj.Name()
            };
            return result;
        }

        public struct K8sSimpleObj
        {
            public string KubeNamespace;
            public string KubeKind;
            public string MetaName;
        }
    }
}
