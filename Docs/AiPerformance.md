# AI Search Performance

The Reversi AI runs against immutable `BoardState` snapshots on a worker task. The runtime default policy is:

- maximum depth: 4
- maximum expanded nodes: 60,000
- maximum think time: 500 ms
- cancellation and generation invalidation on restart, undo, mode/side change, exit, or a newer request

`ReversiAiTests.RepresentativeSearches_AreLegalAndBounded` profiles deterministic positions produced after 0, 24, and 48 plies. On Unity 6000.5.7f1 on the migration Mac, the 2026-08-11 EditMode run recorded:

| Stage | Plies | Elapsed | Expanded nodes | Limit reached |
| --- | ---: | ---: | ---: | --- |
| Early | 0 | 49 ms | 228 | No |
| Middle | 24 | 82 ms | 2,847 | No |
| Late | 48 | 8 ms | 993 | No |

The tests also force a 50-node policy to verify hard node bounding, assert that each selected move is legal for its submitted snapshot, and verify that starting the background request returns control promptly. Timings are observations rather than cross-machine guarantees; the node/time/cancellation limits are the enforced contract.
