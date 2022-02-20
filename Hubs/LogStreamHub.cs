using k8s;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace SignalRChat.Hubs
{
    public class LogStreamHub : Hub
    {
        public const string HUB_URL = "/hubs/logs";
        private readonly IKubernetes _kubeClient;
        private readonly ILogger<LogStreamHub> _logger;

        public LogStreamHub(IKubernetes kubeClient, ILogger<LogStreamHub> logger)
        {
            _kubeClient = kubeClient;
            _logger = logger;
        }

        public async IAsyncEnumerable<string> GetPodLog(string ns, string pod, int tailLines, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = new MemoryStream() as Stream;
            try { stream = await _kubeClient.ReadNamespacedPodLogAsync(pod, ns, limitBytes: (1024 * 512), follow: true, tailLines: tailLines, timestamps: true); }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                yield break;
            }

            string textLine;
            do
            {
                textLine = ReadLine(stream);
                var stringBuilder = new StringBuilder(textLine);
                stringBuilder.Replace("\u001b[40m\u001b[37mtrce\u001b[39m\u001b[22m\u001b[49m", "trce");
                stringBuilder.Replace("\u001b[40m\u001b[37mdbug\u001b[39m\u001b[22m\u001b[49m", "dbug");
                stringBuilder.Replace("\u001b[40m\u001b[32minfo\u001b[39m\u001b[22m\u001b[49m", "info");
                stringBuilder.Replace("\u001b[40m\u001b[1m\u001b[33mwarn\u001b[39m\u001b[22m\u001b[49m", "warn");
                stringBuilder.Replace("\u001b[41m\u001b[30mfail\u001b[39m\u001b[22m\u001b[49m", "fail");
                stringBuilder.Replace("\u001b[41m\u001b[1m\u001b[37mcrit\u001b[39m\u001b[22m\u001b[49m", "crit");
                yield return stringBuilder.ToString();
            } while (textLine != "");
        }
        private static string ReadLine(Stream stream)
        {
            int next = stream.ReadByte();
            if (next < 0) return "";
            var accum = new List<char>();
            int read;
            do
            {
                accum.Add((char)next);
                read = stream.ReadByte();
                next = read;
            } while ((char)next != '\n' && next >= 0);
            if (next < 0) return "";
            return new string(accum.ToArray());
        }
    }
}


