using k8s;

using Microsoft.AspNetCore.SignalR;

using System.Runtime.CompilerServices;
using System.Text;

namespace SignalRChat.Hubs
{
    public class LogStreamHub : Hub
    {
        public const string HUB_URL = "/hubs/logs";

        private readonly IKubernetes _kubeClient;

        public LogStreamHub(IKubernetes kubeClient)
        {
            _kubeClient = kubeClient;
        }

        public async IAsyncEnumerable<string> GetPodLog(string ns, string pod, int sinceSeconds, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stream = await _kubeClient.ReadNamespacedPodLogAsync(pod, ns, follow: true, sinceSeconds: sinceSeconds);

            var buffer = new byte[8192];

            int count;
            do
            {
                count = stream.Read(buffer, 0, buffer.Length);
                if (count != 0)
                {
                    var tmpString = Encoding.Default.GetString(buffer, 0, count);

                    yield return tmpString
                            .Replace("\u001b[40m\u001b[37mtrce\u001b[39m\u001b[22m\u001b[49m", "trce")
                            .Replace("\u001b[40m\u001b[37mdbug\u001b[39m\u001b[22m\u001b[49m", "dbug")
                            .Replace("\u001b[40m\u001b[32minfo\u001b[39m\u001b[22m\u001b[49m", "info")
                            .Replace("\u001b[40m\u001b[1m\u001b[33mwarn\u001b[39m\u001b[22m\u001b[49m", "warn")
                            .Replace("\u001b[41m\u001b[30mfail\u001b[39m\u001b[22m\u001b[49m", "fail")
                            .Replace("\u001b[41m\u001b[1m\u001b[37mcrit\u001b[39m\u001b[22m\u001b[49m", "crit");
                }
            } while (count > 0);
        }
    }
}


