using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using JustEat.StatsD.Buffered;
using JustEat.StatsD.EndpointLookups;

namespace JustEat.StatsD
{
    /// <summary>
    /// A class representing an implementation of <see cref="IStatsDTransport"/> uses UDP and pools sockets. This class cannot be inherited.
    /// </summary>
    public sealed class PooledUdpTransport : IStatsDTransport, IStatsDBufferedTransport, IDisposable
    {
        private ConnectedSocketPool _pool;
        private readonly IPEndPointSource _endpointSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledUdpTransport"/> class.
        /// </summary>
        /// <param name="endPointSource">The <see cref="IPEndPointSource"/> to use.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="endPointSource"/> is <see langword="null"/>.
        /// </exception>
        public PooledUdpTransport(IPEndPointSource endPointSource)
        {
            _endpointSource = endPointSource ?? throw new ArgumentNullException(nameof(endPointSource));
        }

        /// <inheritdoc />
        public void Send(string metric)
        {
            if (string.IsNullOrWhiteSpace(metric))
            {
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(metric);
            var pool = GetPool(_endpointSource.GetEndpoint());
            var socket = pool.PopOrCreate();

            try
            {
                socket.Send(bytes);
            }
            catch (Exception)
            {
                socket.Dispose();
                throw;
            }

            pool.Push(socket);
        }

        public void Send(ArraySegment<byte> metric)
        {
            if (metric.Count == 0)
            {
                return;
            }

            var pool = GetPool(_endpointSource.GetEndpoint());
            var socket = pool.PopOrCreate();

            try
            {
                socket.Send(metric.Array, 0, metric.Count, SocketFlags.None);
            }
            catch (Exception)
            {
                socket.Dispose();
                throw;
            }

            pool.Push(socket);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _pool?.Dispose();
        }

        private ConnectedSocketPool GetPool(IPEndPoint endPoint)
        {
            var oldPool = _pool;

            if (oldPool != null && (ReferenceEquals(oldPool.IpEndPoint, endPoint) || oldPool.IpEndPoint.Equals(endPoint)))
            {
                return oldPool;
            }
            else
            {
                var newPool = new ConnectedSocketPool(endPoint);

                if (Interlocked.CompareExchange(ref _pool, newPool, oldPool) == oldPool)
                {
                    oldPool?.Dispose();
                    return newPool;
                }
                else
                {
                    newPool.Dispose();
                    return _pool;
                }
            }
        }
    }
}
