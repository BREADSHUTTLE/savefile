using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CAPYBARA
{
    public static class ProtoFraming
    {
        public static async UniTask WriteMessageAsync(NetworkStream ns, byte[] payload, CancellationToken ct)
        {
            if (ns == null || !ns.CanWrite) throw new IOException("Stream not writable");
            int len = payload?.Length ?? 0;
            if (len <= 0) throw new IOException("Empty payload");

            // header (Big-Endian)
            var header = new byte[4];
            header[0] = (byte)((len >> 24) & 0xFF);
            header[1] = (byte)((len >> 16) & 0xFF);
            header[2] = (byte)((len >> 8) & 0xFF);
            header[3] = (byte)(len & 0xFF);

            // Task를 UniTask 메서드에서 그냥 await 해도 됨
            await ns.WriteAsync(header, 0, 4, ct);
            await ns.WriteAsync(payload, 0, len, ct);

            // FlushAsync는 런타임별 지원 미묘하니 동기 Flush로 안전하게
            ns.Flush();
        }

        // 즉시 전송용
        public static void WriteMessageSync(NetworkStream ns, byte[] payload)
        {
            if (ns == null || !ns.CanWrite) return;
            int len = payload?.Length ?? 0;
            if (len <= 0) return;

            var header = new byte[4];
            header[0] = (byte)((len >> 24) & 0xFF);
            header[1] = (byte)((len >> 16) & 0xFF);
            header[2] = (byte)((len >> 8) & 0xFF);
            header[3] = (byte)(len & 0xFF);

            ns.Write(header, 0, 4);
            ns.Write(payload, 0, len);
            ns.Flush();
        }

        public static async UniTask<byte[]> ReadMessageAsync(NetworkStream ns, CancellationToken ct)
        {
            if (ns == null || !ns.CanRead) throw new IOException("Stream not readable");


            var header = await ReadExactAsync(ns, 4, ct);
            int len = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
            if (len <= 0) throw new IOException($"Invalid message length: {len}");
            
            return await ReadExactAsync(ns, len, ct);
        }

        private static async UniTask<byte[]> ReadExactAsync(NetworkStream ns, int len, CancellationToken ct)
        {
            var buf = new byte[len];
            int read = 0;
            while (read < len)
            {
                int r = await ns.ReadAsync(buf, read, len - read, ct);
                if (r == 0) throw new IOException("Remote closed");
                read += r;
            }
            return buf;
        }
    }
} 
