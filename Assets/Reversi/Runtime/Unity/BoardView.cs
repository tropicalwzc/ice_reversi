using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IceReversi.Core;
using UnityEngine;

namespace IceReversi.Unity
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private Camera inputCamera;
        [SerializeField] private Collider inputSurface;
        [SerializeField] private Transform piecesRoot;
        [SerializeField] private Transform hintsRoot;
        [SerializeField] private PieceView piecePrefab;
        [SerializeField] private GameObject hintPrefab;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [SerializeField, Min(0f)] private float pieceHeight = 0.16f;
        [SerializeField, Min(0f)] private float hintHeight = 0.12f;
        [SerializeField, Min(0.05f)] private float placementDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float flipDuration = 0.27f;
        [SerializeField, Min(0f)] private float flipDistanceStagger = 0.035f;
        [SerializeField, Min(0.1f)] private float maximumPresentationDuration = 0.5f;

        private readonly PieceView[,] pieces = new PieceView[BoardState.Size, BoardState.Size];
        private readonly List<GameObject> hints = new List<GameObject>();
        private int presentationGeneration;

        public float MaximumPresentationDuration => maximumPresentationDuration;
        public bool IsPresenting { get; private set; }

        public void Configure(
            Camera camera,
            Collider surface,
            Transform piecesContainer,
            Transform hintsContainer,
            PieceView piece,
            GameObject hint)
        {
            inputCamera = camera;
            inputSurface = surface;
            piecesRoot = piecesContainer;
            hintsRoot = hintsContainer;
            piecePrefab = piece;
            hintPrefab = hint;
        }

        public void ConfigurePresentation(float placement, float flip, float stagger, float maximumDuration)
        {
            placementDuration = Mathf.Max(0.05f, placement);
            flipDuration = Mathf.Max(0.05f, flip);
            flipDistanceStagger = Mathf.Max(0f, stagger);
            maximumPresentationDuration = Mathf.Max(0.1f, maximumDuration);
        }

        public bool TryScreenToCoordinate(Vector2 screenPosition, out BoardCoordinate coordinate)
        {
            coordinate = default;
            if (inputCamera == null || inputSurface == null) return false;
            var ray = inputCamera.ScreenPointToRay(screenPosition);
            if (!inputSurface.Raycast(ray, out var hit, inputCamera.farClipPlane)) return false;
            return TryLocalPointToCoordinate(transform.InverseTransformPoint(hit.point), out coordinate);
        }

        public bool TryLocalPointToCoordinate(Vector3 localPoint, out BoardCoordinate coordinate)
        {
            var halfBoard = (BoardState.Size * cellSize) * 0.5f;
            var column = Mathf.FloorToInt((localPoint.x + halfBoard) / cellSize);
            var row = Mathf.FloorToInt((halfBoard - localPoint.z) / cellSize);
            coordinate = new BoardCoordinate(row, column);
            return coordinate.IsValid;
        }

        public Vector3 CoordinateToLocalPosition(BoardCoordinate coordinate, float height)
        {
            var halfBoard = (BoardState.Size * cellSize) * 0.5f;
            return new Vector3(
                -halfBoard + ((coordinate.Column + 0.5f) * cellSize),
                height,
                halfBoard - ((coordinate.Row + 0.5f) * cellSize));
        }

        public void Synchronize(GameSnapshot snapshot, MoveResult moveResult = null, bool animate = true)
        {
            if (animate && moveResult != null)
            {
                _ = PresentMove(snapshot, moveResult, CancellationToken.None, null, null);
                return;
            }

            CancelAndSynchronize(snapshot);
        }

        public Task PresentMove(
            GameSnapshot snapshot,
            MoveResult moveResult,
            CancellationToken cancellationToken,
            Action onPlacementVisible,
            Action onFirstFlipMidpoint)
        {
            if (snapshot == null || moveResult == null || piecePrefab == null || piecesRoot == null)
            {
                return Task.CompletedTask;
            }

            presentationGeneration++;
            var generation = presentationGeneration;
            CancelCurrentAnimations();
            IsPresenting = true;
            var operations = new List<Task>(moveResult.Flips.Count + 1);
            var flipSoundPlayed = false;

            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    var coordinate = new BoardCoordinate(row, column);
                    var color = snapshot.Board[coordinate];
                    var piece = pieces[row, column];
                    if (color == PieceColor.Empty)
                    {
                        if (piece != null)
                        {
                            Destroy(piece.gameObject);
                            pieces[row, column] = null;
                        }
                        continue;
                    }

                    var isNew = piece == null;
                    if (isNew)
                    {
                        piece = CreatePiece(coordinate);
                    }

                    if (coordinate == moveResult.Move)
                    {
                        operations.Add(piece.PresentPlacement(
                            color,
                            Mathf.Min(placementDuration, maximumPresentationDuration),
                            cancellationToken,
                            onPlacementVisible));
                    }
                    else if (Contains(moveResult.Flips, coordinate))
                    {
                        var distance = Math.Max(
                            Math.Abs(coordinate.Row - moveResult.Move.Row),
                            Math.Abs(coordinate.Column - moveResult.Move.Column));
                        var delay = Mathf.Max(0, distance - 1) * flipDistanceStagger;
                        var boundedFlipDuration = Mathf.Min(flipDuration, maximumPresentationDuration);
                        delay = Mathf.Min(delay, Mathf.Max(0f, maximumPresentationDuration - boundedFlipDuration));
                        operations.Add(piece.PresentFlip(
                            color,
                            delay,
                            boundedFlipDuration,
                            cancellationToken,
                            () =>
                            {
                                if (flipSoundPlayed) return;
                                flipSoundPlayed = true;
                                onFirstFlipMidpoint?.Invoke();
                            }));
                    }
                    else
                    {
                        piece.CancelAndSnap(color);
                    }
                }
            }

            RefreshHints(snapshot.LegalMoves);
            return CompletePresentationAsync(operations, generation, cancellationToken);
        }

        public void CancelAndSynchronize(GameSnapshot snapshot)
        {
            presentationGeneration++;
            IsPresenting = false;
            if (snapshot == null || piecePrefab == null || piecesRoot == null) return;

            CancelPieceAnimations(snapshot.Board);
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    var coordinate = new BoardCoordinate(row, column);
                    var color = snapshot.Board[coordinate];
                    var piece = pieces[row, column];
                    if (color == PieceColor.Empty)
                    {
                        if (piece != null)
                        {
                            Destroy(piece.gameObject);
                            pieces[row, column] = null;
                        }
                        continue;
                    }

                    if (piece == null) piece = CreatePiece(coordinate);
                    piece.CancelAndSnap(color);
                }
            }

            RefreshHints(snapshot.LegalMoves);
        }

        private async Task CompletePresentationAsync(
            IReadOnlyList<Task> operations,
            int generation,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.WhenAll(operations);
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                if (this && generation == presentationGeneration)
                {
                    IsPresenting = false;
                }
            }
        }

        private PieceView CreatePiece(BoardCoordinate coordinate)
        {
            var piece = Instantiate(piecePrefab, piecesRoot);
            piece.name = $"Piece_{coordinate.Row}_{coordinate.Column}";
            piece.transform.localPosition = CoordinateToLocalPosition(coordinate, pieceHeight);
            pieces[coordinate.Row, coordinate.Column] = piece;
            return piece;
        }

        private void CancelPieceAnimations(BoardState authoritativeBoard)
        {
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    var piece = pieces[row, column];
                    if (piece == null) continue;
                    var color = authoritativeBoard[row, column];
                    if (color.IsPlayer()) piece.CancelAndSnap(color);
                }
            }
        }

        private void CancelCurrentAnimations()
        {
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    var piece = pieces[row, column];
                    if (piece != null && piece.DisplayedColor.IsPlayer())
                    {
                        piece.CancelAndSnap(piece.DisplayedColor);
                    }
                }
            }
        }

        private void RefreshHints(IReadOnlyList<BoardCoordinate> legalMoves)
        {
            for (var index = 0; index < hints.Count; index++)
            {
                if (hints[index] != null) Destroy(hints[index]);
            }

            hints.Clear();
            if (hintPrefab == null || hintsRoot == null || legalMoves == null) return;
            for (var index = 0; index < legalMoves.Count; index++)
            {
                var coordinate = legalMoves[index];
                var hint = Instantiate(hintPrefab, hintsRoot);
                hint.name = $"Hint_{coordinate.Row}_{coordinate.Column}";
                hint.transform.localPosition = CoordinateToLocalPosition(coordinate, hintHeight);
                hints.Add(hint);
            }
        }

        private static bool Contains(IReadOnlyList<BoardCoordinate> coordinates, BoardCoordinate target)
        {
            if (coordinates == null) return false;
            for (var index = 0; index < coordinates.Count; index++)
            {
                if (coordinates[index] == target) return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            presentationGeneration++;
            IsPresenting = false;
            for (var row = 0; row < BoardState.Size; row++)
            {
                for (var column = 0; column < BoardState.Size; column++)
                {
                    var piece = pieces[row, column];
                    if (piece != null) piece.CancelAndSnap(piece.DisplayedColor);
                }
            }
        }
    }
}
