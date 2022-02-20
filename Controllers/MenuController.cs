using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Mvc;

namespace K8sLogStreaming.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IKubernetes _kubeClient;

        public MenuController(IKubernetes kubeClient)
        {
            _kubeClient = kubeClient;
        }

        [HttpGet]
        [Route("{KubeNamespace}")]
        public async Task<IEnumerable<TopMenuItem>> Get(string KubeNamespace)
        {
            var menu = new List<TopMenuItem>();

            var podListTask = _kubeClient.ListNamespacedPodAsync(KubeNamespace);
            var deploymentListTask = _kubeClient.ListNamespacedDeploymentAsync(KubeNamespace);
            var daemonSetsListTask = _kubeClient.ListNamespacedDaemonSetAsync(KubeNamespace);
            var statefulSetsListTask = _kubeClient.ListNamespacedStatefulSetAsync(KubeNamespace);

            await Task.WhenAll(podListTask, deploymentListTask, daemonSetsListTask, statefulSetsListTask);

            menu.Add(GetTopMenuItem<V1PodList, V1Pod>(podListTask.Result));
            menu.Add(GetTopMenuItem<V1DeploymentList, V1Deployment>(deploymentListTask.Result));
            menu.Add(GetTopMenuItem<V1DaemonSetList, V1DaemonSet>(daemonSetsListTask.Result));
            menu.Add(GetTopMenuItem<V1StatefulSetList, V1StatefulSet>(statefulSetsListTask.Result));

            return menu;
        }

        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<TopMenuItem>> Get()
        {
            var menu = new List<TopMenuItem>();

            var podListTask = _kubeClient.ListPodForAllNamespacesAsync();
            var deploymentListTask = _kubeClient.ListDeploymentForAllNamespacesAsync();
            var daemonSetsListTask = _kubeClient.ListDaemonSetForAllNamespacesAsync();
            var statefulSetsListTask = _kubeClient.ListStatefulSetForAllNamespacesAsync();
            var cronJobsListTask = _kubeClient.ListCronJobForAllNamespacesAsync();

            await Task.WhenAll(podListTask, deploymentListTask, daemonSetsListTask, statefulSetsListTask, cronJobsListTask);

            menu.Add(GetTopMenuItem<V1PodList, V1Pod>(podListTask.Result));
            menu.Add(GetTopMenuItem<V1DeploymentList, V1Deployment>(deploymentListTask.Result));
            menu.Add(GetTopMenuItem<V1DaemonSetList, V1DaemonSet>(daemonSetsListTask.Result));
            menu.Add(GetTopMenuItem<V1StatefulSetList, V1StatefulSet>(statefulSetsListTask.Result));

            return menu;
        }

        private TopMenuItem GetTopMenuItem<TList, T>(TList kubeObjList)
            where T : IKubernetesObject<V1ObjectMeta>, IKubernetesObject, IMetadata<V1ObjectMeta>, IValidate
            where TList : IKubernetesObject<V1ListMeta>, IKubernetesObject, IMetadata<V1ListMeta>, IItems<T>, IValidate
        {
            TopMenuItem topMenuItem = new();
            topMenuItem.Label = kubeObjList.Kind;

            if (kubeObjList.Items == null || kubeObjList.Items.Count == 0) { return topMenuItem; }

            if (kubeObjList.Kind == V1PodList.KubeKind) { kubeObjList.Items = kubeObjList.Items.Where(obj => obj.OwnerReferences() == null).ToList(); }

            foreach (var obj in kubeObjList.Items)
            {
                var kind = obj.GetKubernetesTypeMetadata().Kind;
                topMenuItem.Items.Add(new()
                {
                    Label = obj.Name(),
                    Url = $"{Request.Scheme}://{Request.Host}/?KubeNamespace={obj.Namespace()}&KubeKind={kind}&MetaName={obj.Name()}"
                });
            }
            return topMenuItem;
        }
    }

    public class TopMenuItem
    {
        public string Label { get; set; } = string.Empty;
        public List<SubMenuItem> Items { get; set; } = new List<SubMenuItem>();
    }
    public class SubMenuItem
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
