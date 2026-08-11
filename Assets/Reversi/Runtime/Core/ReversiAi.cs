using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace IceReversi.Core
{
    public sealed class ReversiAi
    {
        private const int TerminalMultiplier = 1000000;
        private const int NegativeInfinity = int.MinValue + 4096;
        private const int PositiveInfinity = int.MaxValue - 4096;

        private static readonly int[,] PositionalWeights =
        {
            { 120, -35, 18, 8, 8, 18, -35, 120 },
            { -35, -65, -12, -8, -8, -12, -65, -35 },
            { 18, -12, 10, 3, 3, 10, -12, 18 },
            { 8, -8, 3, 2, 2, 3, -8, 8 },
            { 8, -8, 3, 2, 2, 3, -8, 8 },
            { 18, -12, 10, 3, 3, 10, -12, 18 },
            { -35, -65, -12, -8, -8, -12, -65, -35 },
            { 120, -35, 18, 8, 8, 18, -35, 120 }
        };

        public AiSearchResult FindBestMove(
            BoardState board,
            PieceColor color,
            AiSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (!color.IsPlayer())
            {
                throw new ArgumentException("AI color must be Black or White.", nameof(color));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var legalMoves = ReversiRules.GetLegalMoves(board, color);
            if (legalMoves.Count == 0)
            {
                return new AiSearchResult(null, Evaluate(board, color, color), 0, false);
            }

            var sanitized = (options ?? AiSearchOptions.Default).Sanitized();
            var context = new SearchContext(sanitized, cancellationToken);
            var orderedFallback = OrderMoves(board, color, legalMoves, null, context);
            var selectedMove = orderedFallback[0].Move;
            var selectedScore = Evaluate(board, color, color);
            IReadOnlyList<AiRootScore> completedScores = Array.Empty<AiRootScore>();
            var completedDepth = 0;
            var solvedToEnd = false;

            for (var depth = 1; depth <= sanitized.MaximumDepth; depth++)
            {
                try
                {
                    var iteration = SearchRoot(board, color, depth, false, context);
                    completedScores = iteration;
                    completedDepth = depth;
                    SelectMove(iteration, board, color, sanitized, out selectedMove, out selectedScore);
                }
                catch (SearchLimitException)
                {
                    break;
                }
            }

            if (!context.ReachedLimit && sanitized.ExactEndgameThreshold > 0 &&
                board.EmptyCount <= sanitized.ExactEndgameThreshold)
            {
                try
                {
                    var exactScores = SearchRoot(board, color, board.EmptyCount, true, context);
                    completedScores = exactScores;
                    completedDepth = Math.Max(completedDepth, board.EmptyCount);
                    solvedToEnd = true;
                    SelectMove(exactScores, board, color, sanitized, out selectedMove, out selectedScore);
                }
                catch (SearchLimitException)
                {
                    // Keep the deepest fully completed ordinary iteration.
                }
            }

            return new AiSearchResult(
                selectedMove,
                selectedScore,
                context.ExpandedNodes,
                context.ReachedLimit,
                completedDepth,
                context.CacheHits,
                context.CacheEntries,
                completedScores,
                solvedToEnd);
        }

        private static IReadOnlyList<AiRootScore> SearchRoot(
            BoardState board,
            PieceColor color,
            int depth,
            bool exact,
            SearchContext context)
        {
            context.CheckLimits();
            var moves = ReversiRules.GetLegalMoves(board, color);
            var cachedBest = context.Table.GetBestMove(board.Identity(color));
            var ordered = OrderMoves(board, color, moves, cachedBest, context);
            var scores = new List<AiRootScore>(ordered.Count);
            for (var index = 0; index < ordered.Count; index++)
            {
                context.CheckLimits();
                var candidate = ordered[index];
                var score = Search(
                    candidate.Board,
                    color.Opponent(),
                    color,
                    depth - 1,
                    NegativeInfinity,
                    PositiveInfinity,
                    exact,
                    context);
                scores.Add(new AiRootScore(candidate.Move, score));
            }

            return scores;
        }

        private static int Search(
            BoardState board,
            PieceColor turn,
            PieceColor maximizingColor,
            int depth,
            int alpha,
            int beta,
            bool exact,
            SearchContext context)
        {
            context.EnterNode();
            if (!exact && depth <= 0)
            {
                return Evaluate(board, maximizingColor, turn);
            }

            var key = board.Identity(turn);
            var alphaOriginal = alpha;
            var betaOriginal = beta;
            if (context.Table.TryGet(key, depth, exact, ref alpha, ref beta, out var cachedScore))
            {
                context.RecordCacheHit();
                return cachedScore;
            }

            context.CheckLimits();
            var moves = ReversiRules.GetLegalMoves(board, turn);
            if (moves.Count == 0)
            {
                var opponent = turn.Opponent();
                if (!ReversiRules.HasAnyLegalMove(board, opponent))
                {
                    return TerminalScore(board, maximizingColor);
                }

                var passedScore = Search(board, opponent, maximizingColor, depth, alpha, beta, exact, context);
                context.Table.Store(key, depth, passedScore, BoundType.Exact, null, exact);
                return passedScore;
            }

            if (exact && depth <= 0)
            {
                return Evaluate(board, maximizingColor, turn);
            }

            var cachedBest = context.Table.GetBestMove(key);
            var ordered = OrderMoves(board, turn, moves, cachedBest, context);
            var maximizing = turn == maximizingColor;
            var bestScore = maximizing ? NegativeInfinity : PositiveInfinity;
            BoardCoordinate? bestMove = null;
            for (var index = 0; index < ordered.Count; index++)
            {
                context.CheckLimits();
                var candidate = ordered[index];
                var score = Search(
                    candidate.Board,
                    turn.Opponent(),
                    maximizingColor,
                    depth - 1,
                    alpha,
                    beta,
                    exact,
                    context);

                if (maximizing ? score > bestScore : score < bestScore)
                {
                    bestScore = score;
                    bestMove = candidate.Move;
                }

                if (maximizing)
                {
                    alpha = Math.Max(alpha, bestScore);
                }
                else
                {
                    beta = Math.Min(beta, bestScore);
                }

                if (beta <= alpha)
                {
                    break;
                }
            }

            var bound = bestScore <= alphaOriginal
                ? BoundType.Upper
                : bestScore >= betaOriginal ? BoundType.Lower : BoundType.Exact;
            context.Table.Store(key, depth, bestScore, bound, bestMove, exact);
            return bestScore;
        }

        private static List<MoveCandidate> OrderMoves(
            BoardState board,
            PieceColor turn,
            IReadOnlyList<BoardCoordinate> moves,
            BoardCoordinate? cachedBest,
            SearchContext context)
        {
            var candidates = new List<MoveCandidate>(moves.Count);
            for (var index = 0; index < moves.Count; index++)
            {
                context.CheckLimits();
                var move = moves[index];
                var next = ReversiRules.ApplyMove(board, move, turn, out _);
                var priority = PositionalWeights[move.Row, move.Column] * 100;
                if (IsCorner(move))
                {
                    priority += 1000000;
                }

                if (cachedBest.HasValue && cachedBest.Value == move)
                {
                    priority += 2000000;
                }

                priority -= ReversiRules.GetLegalMoves(next, turn.Opponent()).Count * 250;
                candidates.Add(new MoveCandidate(move, next, priority));
            }

            candidates.Sort((left, right) =>
            {
                var priority = right.Priority.CompareTo(left.Priority);
                return priority != 0 ? priority : left.Move.CompareTo(right.Move);
            });
            return candidates;
        }

        private static void SelectMove(
            IReadOnlyList<AiRootScore> scores,
            BoardState board,
            PieceColor color,
            AiSearchOptions options,
            out BoardCoordinate selectedMove,
            out int selectedScore)
        {
            var ranked = new List<AiRootScore>(scores);
            ranked.Sort((left, right) =>
            {
                var score = right.Score.CompareTo(left.Score);
                return score != 0 ? score : left.Move.CompareTo(right.Move);
            });

            var selectedIndex = 0;
            if (options.RootChoicePolicy == AiRootChoicePolicy.ControlledTopThree && ranked.Count > 1)
            {
                var identity = board.Identity(color);
                var seed = unchecked(options.RandomSeed ^ identity.GetHashCode());
                var roll = new Random(seed).Next(100);
                selectedIndex = roll < 65 ? 0 : roll < 90 ? Math.Min(1, ranked.Count - 1) : Math.Min(2, ranked.Count - 1);
            }

            selectedMove = ranked[selectedIndex].Move;
            selectedScore = ranked[selectedIndex].Score;
        }

        private static int Evaluate(BoardState board, PieceColor perspective, PieceColor turn)
        {
            var opponent = perspective.Opponent();
            var empties = board.EmptyCount;
            var early = empties > 40;
            var late = empties <= 16;
            var pieceWeight = early ? 8 : late ? 1100 : 45;
            var mobilityWeight = early ? 900 : late ? 180 : 700;
            var positionalWeight = early ? 32 : late ? 8 : 24;
            var stableWeight = early ? 700 : late ? 2800 : 1500;
            var frontierWeight = early ? 420 : late ? 80 : 300;
            var cornerWeight = early ? 24000 : late ? 32000 : 28000;
            var cornerSafetyWeight = early ? 3500 : late ? 500 : 2400;

            var pieceDifference = board.Count(perspective) - board.Count(opponent);
            var mobilityDifference = ReversiRules.GetLegalMoves(board, perspective).Count -
                ReversiRules.GetLegalMoves(board, opponent).Count;
            var positionalDifference = 0;
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    var cell = board[row, column];
                    if (cell == perspective)
                    {
                        positionalDifference += PositionalWeights[row, column];
                    }
                    else if (cell == opponent)
                    {
                        positionalDifference -= PositionalWeights[row, column];
                    }
                }
            }

            var stableDifference = EstimateStableEdgePieces(board, perspective) -
                EstimateStableEdgePieces(board, opponent);
            var frontierDifference = CountFrontier(board, perspective) - CountFrontier(board, opponent);
            var cornerDifference = CountCorners(board, perspective) - CountCorners(board, opponent);
            var cornerSafety = CountUnsafeCornerAdjacency(board, opponent) -
                CountUnsafeCornerAdjacency(board, perspective);
            var parity = late ? ((empties & 1) == 0 ? -1 : 1) * (turn == perspective ? 1 : -1) : 0;

            return (pieceDifference * pieceWeight) +
                (mobilityDifference * mobilityWeight) +
                (positionalDifference * positionalWeight) +
                (stableDifference * stableWeight) -
                (frontierDifference * frontierWeight) +
                (cornerDifference * cornerWeight) +
                (cornerSafety * cornerSafetyWeight) +
                (parity * 350);
        }

        private static int TerminalScore(BoardState board, PieceColor perspective)
        {
            return (board.Count(perspective) - board.Count(perspective.Opponent())) * TerminalMultiplier;
        }

        private static int CountCorners(BoardState board, PieceColor color)
        {
            var count = 0;
            if (board[0, 0] == color) count++;
            if (board[0, 7] == color) count++;
            if (board[7, 0] == color) count++;
            if (board[7, 7] == color) count++;
            return count;
        }

        private static int CountUnsafeCornerAdjacency(BoardState board, PieceColor color)
        {
            var count = 0;
            count += CountCornerAdjacency(board, color, 0, 0, 1, 1);
            count += CountCornerAdjacency(board, color, 0, 7, 1, -1);
            count += CountCornerAdjacency(board, color, 7, 0, -1, 1);
            count += CountCornerAdjacency(board, color, 7, 7, -1, -1);
            return count;
        }

        private static int CountCornerAdjacency(
            BoardState board,
            PieceColor color,
            int cornerRow,
            int cornerColumn,
            int rowDirection,
            int columnDirection)
        {
            if (board[cornerRow, cornerColumn] != PieceColor.Empty)
            {
                return 0;
            }

            var count = 0;
            if (board[cornerRow + rowDirection, cornerColumn] == color) count++;
            if (board[cornerRow, cornerColumn + columnDirection] == color) count++;
            if (board[cornerRow + rowDirection, cornerColumn + columnDirection] == color) count++;
            return count;
        }

        private static int CountFrontier(BoardState board, PieceColor color)
        {
            var count = 0;
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    if (board[row, column] != color)
                    {
                        continue;
                    }

                    var frontier = false;
                    for (var rowOffset = -1; rowOffset <= 1 && !frontier; rowOffset++)
                    {
                        for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
                        {
                            var checkRow = row + rowOffset;
                            var checkColumn = column + columnOffset;
                            if ((rowOffset != 0 || columnOffset != 0) && checkRow >= 0 && checkRow < BoardState.Size &&
                                checkColumn >= 0 && checkColumn < BoardState.Size &&
                                board[checkRow, checkColumn] == PieceColor.Empty)
                            {
                                frontier = true;
                                break;
                            }
                        }
                    }

                    if (frontier) count++;
                }
            }

            return count;
        }

        private static int EstimateStableEdgePieces(BoardState board, PieceColor color)
        {
            var stable = new HashSet<BoardCoordinate>();
            AddStableRun(board, color, 0, 0, 0, 1, stable);
            AddStableRun(board, color, 0, 0, 1, 0, stable);
            AddStableRun(board, color, 0, 7, 0, -1, stable);
            AddStableRun(board, color, 0, 7, 1, 0, stable);
            AddStableRun(board, color, 7, 0, 0, 1, stable);
            AddStableRun(board, color, 7, 0, -1, 0, stable);
            AddStableRun(board, color, 7, 7, 0, -1, stable);
            AddStableRun(board, color, 7, 7, -1, 0, stable);
            return stable.Count;
        }

        private static void AddStableRun(
            BoardState board,
            PieceColor color,
            int startRow,
            int startColumn,
            int rowStep,
            int columnStep,
            ISet<BoardCoordinate> stable)
        {
            if (board[startRow, startColumn] != color) return;
            var row = startRow;
            var column = startColumn;
            while (row >= 0 && row < BoardState.Size && column >= 0 && column < BoardState.Size &&
                   board[row, column] == color)
            {
                stable.Add(new BoardCoordinate(row, column));
                row += rowStep;
                column += columnStep;
            }
        }

        private static bool IsCorner(BoardCoordinate move)
        {
            return (move.Row == 0 || move.Row == 7) && (move.Column == 0 || move.Column == 7);
        }

        private readonly struct MoveCandidate
        {
            public MoveCandidate(BoardCoordinate move, BoardState board, int priority)
            {
                Move = move;
                Board = board;
                Priority = priority;
            }

            public BoardCoordinate Move { get; }
            public BoardState Board { get; }
            public int Priority { get; }
        }

        private enum BoundType
        {
            Exact,
            Lower,
            Upper
        }

        private sealed class TranspositionTable
        {
            private readonly Entry[] entries;
            private int count;

            public TranspositionTable(int capacity)
            {
                entries = capacity > 0 ? new Entry[capacity] : Array.Empty<Entry>();
            }

            public int Count => count;

            public BoardCoordinate? GetBestMove(PositionIdentity key)
            {
                if (!TryFind(key, out var entry)) return null;
                return entry.BestMove;
            }

            public bool TryGet(
                PositionIdentity key,
                int depth,
                bool exact,
                ref int alpha,
                ref int beta,
                out int score)
            {
                score = 0;
                if (!TryFind(key, out var entry) || entry.Depth < depth || (exact && !entry.SolvedToEnd))
                {
                    return false;
                }

                score = entry.Score;
                if (entry.Bound == BoundType.Exact) return true;
                if (entry.Bound == BoundType.Lower) alpha = Math.Max(alpha, score);
                else beta = Math.Min(beta, score);
                return alpha >= beta;
            }

            public void Store(
                PositionIdentity key,
                int depth,
                int score,
                BoundType bound,
                BoardCoordinate? bestMove,
                bool solvedToEnd)
            {
                if (entries.Length == 0) return;
                var index = IndexFor(key);
                var existing = entries[index];
                if (existing.Valid && existing.Key != key && existing.Depth > depth)
                {
                    return;
                }

                if (!existing.Valid) count++;
                entries[index] = new Entry(key, depth, score, bound, bestMove, solvedToEnd);
            }

            private bool TryFind(PositionIdentity key, out Entry entry)
            {
                if (entries.Length == 0)
                {
                    entry = default;
                    return false;
                }

                entry = entries[IndexFor(key)];
                return entry.Valid && entry.Key == key;
            }

            private int IndexFor(PositionIdentity key)
            {
                return (key.GetHashCode() & 0x7fffffff) % entries.Length;
            }

            private readonly struct Entry
            {
                public Entry(
                    PositionIdentity key,
                    int depth,
                    int score,
                    BoundType bound,
                    BoardCoordinate? bestMove,
                    bool solvedToEnd)
                {
                    Key = key;
                    Depth = depth;
                    Score = score;
                    Bound = bound;
                    BestMove = bestMove;
                    SolvedToEnd = solvedToEnd;
                    Valid = true;
                }

                public PositionIdentity Key { get; }
                public int Depth { get; }
                public int Score { get; }
                public BoundType Bound { get; }
                public BoardCoordinate? BestMove { get; }
                public bool SolvedToEnd { get; }
                public bool Valid { get; }
            }
        }

        private sealed class SearchContext
        {
            private readonly AiSearchOptions options;
            private readonly CancellationToken cancellationToken;
            private readonly Stopwatch stopwatch = Stopwatch.StartNew();

            public SearchContext(AiSearchOptions options, CancellationToken cancellationToken)
            {
                this.options = options;
                this.cancellationToken = cancellationToken;
                Table = new TranspositionTable(options.TranspositionCapacity);
            }

            public int ExpandedNodes { get; private set; }
            public int CacheHits { get; private set; }
            public int CacheEntries => Table.Count;
            public bool ReachedLimit { get; private set; }
            public TranspositionTable Table { get; }

            public void EnterNode()
            {
                CheckLimits();
                ExpandedNodes++;
            }

            public void RecordCacheHit() => CacheHits++;

            public void CheckLimits()
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ExpandedNodes >= options.MaximumNodes ||
                    stopwatch.ElapsedMilliseconds >= options.MaximumThinkTimeMilliseconds)
                {
                    ReachedLimit = true;
                    throw SearchLimitException.Instance;
                }
            }
        }

        private sealed class SearchLimitException : Exception
        {
            public static readonly SearchLimitException Instance = new SearchLimitException();
            private SearchLimitException() { }
        }
    }
}
