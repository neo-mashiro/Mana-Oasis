using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace x.Restopia.Scripts.Chess {
    // to be attached on the chessboard game object
    // to be further modified and finalized in Mana Oasis
    // don't forget to update the README image in Chess-Oasis once the board & volumetric light is ok
    public class ChessController : MonoBehaviour {
        private Camera _camera;
        private ChessBoard _board;
        private Transform[] _cells;
        private Transform[] _pieces;
        private Transform[] _spotlights;

        private RaycastHit _hit;
        private bool _suspendUpdate;
        
        private Transform _currentCell;
        private static readonly int Fight = Animator.StringToHash("Fight");

        private const int LayerMask = 1 << 8;

        private void Start() {
            _camera = Camera.main;
            _board = new ChessBoard();
            _cells = transform.GetAllDirectChildren();
            _pieces = GameObject.FindGameObjectsWithTag("Piece").Select(go => go.transform).ToArray();
            _spotlights = _cells.Select(t => t.Find("Volumetric Light")).ToArray();
        }

        private void Update() {
            if (_suspendUpdate) { return; }
            
            if (Input.GetMouseButtonDown(0)) {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out _hit, 100.0f, LayerMask)) {
                    _currentCell = _hit.transform;
                    var cells = _board.GetLegalMoves(_currentCell.name);
                    SpotlightOn(cells);
                }
                else {
                    _currentCell = null;
                    SpotlightOff();
                }
            }
            
            else if (Input.GetMouseButtonDown(1)) {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out _hit, 100.0f, LayerMask)) {
                    if (_hit.transform.GetChild(0).gameObject.activeSelf) {
                        SpotlightOff();
                        Move(_currentCell, _hit.transform);
                    }
                }
            }
        }
        
        private IEnumerator AnimateMove(Transform from, Transform to) {
            var sourcePiece = GetPieceOnCell(from);
            var targetPiece = GetPieceOnCell(to);
            
            var agent = sourcePiece.GetComponent<NavMeshAgent>();
            agent.SetDestination(to.position);
            
            yield return new WaitForSeconds(5);

            if (agent.pathStatus == NavMeshPathStatus.PathComplete) {
                if (!ReferenceEquals(targetPiece, null)) {
                    sourcePiece.GetComponent<Animator>().SetTrigger(Fight);
                    yield return new WaitForSeconds(2);
                    targetPiece.gameObject.SetActive(false);
                }
            }

            CheckGameOver();
        }

        private void Move(Transform from, Transform to) {
            _suspendUpdate = true;
            
            _board.Move(from.name, to.name);
            StartCoroutine(AnimateMove(from, to));
            _currentCell = null;
            
            var (source, target) = _board.MinimaxAI();
            StartCoroutine(AnimateMove(transform.Find(source), transform.Find(target)));
            
            _suspendUpdate = false;
        }

        private void CheckGameOver() {
            if (_board.GameOver) {
                Debug.Log($"Game over, {_board.Winner} wins!");
                // open UI dialog which has a restart button and a quit button
            }
        }

        private void SpotlightOn(List<string> cells) {
            for (var i = 0; i < _cells.Length; i++) {
                var on = cells.Contains(_cells[i].name);
                _spotlights[i].gameObject.SetActive(on);
            }
        }
        
        private void SpotlightOff() {
            foreach (var spotlight in _spotlights) {
                spotlight.gameObject.SetActive(false);
            }
        }

        private Transform GetPieceOnCell(Transform cell) {
            return _pieces.FirstOrDefault(piece =>
                piece.position.x > cell.position.x && piece.position.x < cell.position.x + 1 &&
                piece.position.z > cell.position.z && piece.position.z < cell.position.z + 1);
        }
    }

}
