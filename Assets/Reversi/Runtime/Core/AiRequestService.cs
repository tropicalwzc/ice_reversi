using System;
using System.Threading;
using System.Threading.Tasks;

namespace IceReversi.Core
{
    public sealed class AiRequestService : IDisposable
    {
        private readonly object gate = new object();
        private readonly ReversiAi ai;
        private CancellationTokenSource activeCancellation;
        private int generation;
        private bool disposed;

        public AiRequestService(ReversiAi ai = null)
        {
            this.ai = ai ?? new ReversiAi();
        }

        public AiRequest Start(
            BoardState board,
            PieceColor color,
            AiSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            lock (gate)
            {
                ThrowIfDisposed();
                CancelActiveLocked();
                generation++;
                var requestGeneration = generation;
                activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var token = activeCancellation.Token;
                var task = Task.Run(() => ai.FindBestMove(board, color, options, token), token);
                return new AiRequest(requestGeneration, task);
            }
        }

        public bool IsCurrent(int requestGeneration)
        {
            lock (gate)
            {
                return !disposed && requestGeneration == generation &&
                    activeCancellation != null && !activeCancellation.IsCancellationRequested;
            }
        }

        public void Invalidate()
        {
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                generation++;
                CancelActiveLocked();
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                generation++;
                CancelActiveLocked();
            }
        }

        private void CancelActiveLocked()
        {
            if (activeCancellation == null)
            {
                return;
            }

            activeCancellation.Cancel();
            activeCancellation.Dispose();
            activeCancellation = null;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AiRequestService));
            }
        }
    }

    public readonly struct AiRequest
    {
        public AiRequest(int generation, Task<AiSearchResult> task)
        {
            Generation = generation;
            Task = task;
        }

        public int Generation { get; }
        public Task<AiSearchResult> Task { get; }
    }
}
