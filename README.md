# Tic Tac Toe — Angular + .NET

A browser-based Tic Tac Toe game. The **.NET Web API** owns the game session, rules, move history and scoreboard; the **Angular** app is a thin client that calls the REST API and renders whatever state the backend returns.

```
┌────────────────────┐   REST / JSON    ┌───────────────────────────┐
│ Angular 21 (4200)  │ ───────────────▶ │ ASP.NET Core Web API (5080)│
│ renders GameState  │ ◀─────────────── │ rules · history · scores   │
└────────────────────┘   (dev proxy)    │ in-memory state            │
                                        └───────────────────────────┘
```

## Contents
1. [Tech stack](#tech-stack)
2. [Features implemented](#features-implemented)
3. [How to run the backend](#how-to-run-the-backend)
4. [How to run the frontend](#how-to-run-the-frontend)
5. [API reference](#api-reference)
6. [How to run tests](#how-to-run-tests)
7. [Requirements traceability](#requirements-traceability)
8. [Design decisions](#design-decisions)
9. [Clarifications and assumptions](#clarifications-and-assumptions)
10. [AI tools and prompt summary](#ai-tools-and-prompt-summary)
11. [Known limitations](#known-limitations)
12. [Future improvements](#future-improvements)

## Tech stack

| Layer | Choice |
| --- | --- |
| Frontend | Angular 21 (standalone components, signals), TypeScript, plain CSS |
| Backend | .NET Web API (controllers), C#, targets `net8.0` (rolls forward to newer runtimes) |
| API style | REST + JSON (enums serialized as strings) |
| Storage | In-memory (singleton service) |
| API docs | Swagger UI at `/swagger` (Swashbuckle) |
| Tests | xUnit (+ `WebApplicationFactory` integration tests) · Vitest via Angular's unit-test builder |
| CI | GitHub Actions (`.github/workflows/ci.yml`) |

```
.
├── backend/
│   ├── TicTacToe.sln
│   ├── src/TicTacToe.Api/
│   │   ├── Domain/          Game, ComputerPlayer, Scoreboard, enums, rule exceptions
│   │   ├── Services/        GameService (state owner, singleton)
│   │   ├── Contracts/       request/response DTOs
│   │   ├── Controllers/     GamesController, ScoreboardController
│   │   └── Infrastructure/  exception filter -> consistent 4xx JSON errors
│   └── tests/TicTacToe.Tests/
└── frontend/
    └── src/app/
        ├── components/      board, move-history, scoreboard (presentational)
        ├── services/        GameApiService (REST client)
        ├── models/          API contract types
        └── app.ts|html|css  container: calls the API, holds the latest state
```

## Features implemented

- **Game board** — 3×3, empty cells clickable; moves show X/O; occupied cells are locked.
- **Player turns** — X starts, turns alternate after every *valid* move; the UI always shows whose turn it is; invalid moves never change the turn.
- **Win detection** — rows, columns, diagonals; winner shown, winning cells highlighted, board locked, scoreboard updated.
- **Draw detection** — 9 filled cells, no winner → draw message, board locked, scoreboard updated.
- **Reset Game** — clears board, history, result; turn back to X; fresh game; scoreboard unchanged.
- **Move history** — Move / Player / Position (`Row 1, Column 1`), updated after every valid move.
- **Undo Last Move** — mode-aware (see below); disabled when there is nothing to undo.
- **Scoreboard** — X wins / O wins / draws, served by the backend, updated exactly once per completed game, with a separate **Reset Scoreboard**.
- **Two modes** — *Two Player* and *Play Against Computer* (human = X, computer = O). The computer plays automatically after the human, only plays valid moves, never moves after the game ended, and follows the required priority: **win → block → center → corner → any free cell**.
- **Backend validation** — rejects moves outside the board, on occupied cells, after game completion, and by the wrong player.
- **Resume on refresh** — the browser remembers the current game id for the tab; if the backend no longer has it, a new game starts.

### Undo behaviour

| Mode | What Undo removes | Result |
| --- | --- | --- |
| Two Player | the most recent move only | e.g. X, O played → Undo removes O's move → it is O's turn again |
| Computer | the computer's last move **and** the human move before it | e.g. X, O(computer) → Undo removes both → it is X's turn again |

After an undo the backend recomputes board, current player, status and history from the remaining move list.

## How to run the backend

**Prerequisite:** .NET SDK 8.0 or newer.

```bash
cd backend/src/TicTacToe.Api
dotnet run
```

- API: <http://localhost:5080>
- Swagger UI: <http://localhost:5080/swagger> (try every endpoint from the browser)

## How to run the frontend

**Prerequisite:** Node.js 22 LTS (Angular 21 needs Node ≥ 20.19 / 22.12) and npm.

```bash
cd frontend
npm install
npm start
```

Open <http://localhost:4200>. `ng serve` proxies `/api` to `http://localhost:5080` (see `frontend/proxy.conf.json`), so **start the backend first**. The backend also enables CORS for `http://localhost:4200`, so the app works without the proxy too.

> If `npm install` ever fails with an arborist error such as `Cannot read properties of null (reading 'edgesOut')`, run `npm install --legacy-peer-deps` (the repo's `.npmrc` already sets this).

## API reference

Base URL: `http://localhost:5080`. All bodies are JSON. Enums are strings: `Player` = `"X" | "O"`, `GameMode` = `"TwoPlayer" | "VsComputer"`, `GameStatus` = `"InProgress" | "Won" | "Draw"`.

| Method | Endpoint | Purpose | Success |
| --- | --- | --- | --- |
| POST | `/api/games` | Create a new game session. Body `{ "mode": "TwoPlayer" \| "VsComputer" }` (optional, default `TwoPlayer`) | `201` + game state |
| GET | `/api/games/{id}` | Get current game state | `200` + game state |
| POST | `/api/games/{id}/moves` | Submit a move. Body `{ "player": "X", "cellIndex": 4 }` | `200` + game state |
| POST | `/api/games/{id}/undo` | Undo last move (Two Player) / last move pair (Computer) | `200` + game state |
| POST | `/api/games/{id}/reset` | Reset the current game (same id and mode); scoreboard kept | `200` + game state |
| GET | `/api/scoreboard` | Get the session scoreboard | `200` + scoreboard |
| POST | `/api/scoreboard/reset` | Reset the scoreboard to zero | `200` + scoreboard |

**Move request.** The game id is in the route; the body carries `player` (`"X"`/`"O"`) and `cellIndex` (0–8, row-major: `0` = top-left, `4` = center, `8` = bottom-right). In Computer mode the response to a human move **already contains the computer's reply**.

**Game state response** (`GameStateResponse`):

```json
{
  "id": "0b6f3c8e-6f4e-4c1a-9a53-1f1d6a4c9a10",
  "mode": "TwoPlayer",
  "status": "InProgress",
  "currentPlayer": "O",
  "winner": null,
  "board": ["X", null, null, null, null, null, null, null, null],
  "winningCells": [],
  "moves": [
    { "moveNumber": 1, "player": "X", "cellIndex": 0, "row": 1, "column": 1, "position": "Row 1, Column 1" }
  ],
  "canUndo": true,
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

`currentPlayer` is `null` once the game is completed. `row`/`column` in the history are 1-based (display); `cellIndex` and `winningCells` are 0-based indexes.

**Errors.** Rejected requests return `{ "code": "...", "message": "..." }` and never change game state:

| Situation | HTTP | `code` |
| --- | --- | --- |
| Move outside the board (`cellIndex` not 0–8) | 400 | `OutOfBounds` |
| Move on an occupied cell | 409 | `CellOccupied` |
| Move after the game is completed | 409 | `GameOver` |
| Move by the wrong player (or by O in Computer mode) | 409 | `WrongPlayer` |
| Undo with no moves | 409 | `NothingToUndo` |
| Undo after the game is completed (Option A) | 409 | `UndoNotAllowed` |
| Unknown game id | 404 | `GameNotFound` |
| Missing/invalid body fields (e.g. no `player`) | 400 | ASP.NET validation problem details |

## How to run tests

```bash
# Backend (xUnit: unit + API integration tests)
dotnet test backend/TicTacToe.sln

# Frontend (Vitest: component rendering + REST-contract tests)
cd frontend
npx ng test --watch=false
```

**Backend coverage** (`backend/tests/TicTacToe.Tests`): valid move · invalid moves (outside board / occupied / wrong player) · turn switching · row, column and diagonal wins · draw · reset game · undo in Two Player mode · undo in Computer mode · scoreboard update (once per game, shared across games, reset) · computer move selection (each priority) · move after completion · plus HTTP-level checks of status codes and JSON shape.

**Frontend coverage:** board rendering/locking/highlighting/click events, move-history table, scoreboard, the REST client's URLs/verbs/bodies, and the container component (turn text, winner/draw text, Undo enabled/disabled, mode switch, error display).

## Requirements traceability

| Requirement | Where | Verified by |
| --- | --- | --- |
| 3×3 board, locked cells | `Game.PlayMove`, `board.component.ts` | `ValidMove_…`, `MoveOnOccupiedCell_…`, board spec |
| Alternating turns, invalid move keeps turn | `Game.Recalculate` | `Turns_Alternate…`, `MoveByWrongPlayer_…` |
| Win (row/col/diagonal) + highlight | `Game.WinningLines`, `.winning` CSS | `RowWin_/ColumnWin_/DiagonalWin_…`, board spec |
| Draw | `Game.Recalculate` | `FullBoardWithoutWinner_IsADraw` |
| Reset Game keeps scoreboard | `Game.Reset`, `GameService.Reset` | `Reset_…`, `ResetGame_KeepsTheScoreboard_…` |
| Move history | `GameStateResponse.Moves` | `Response_ContainsMoveHistory_…`, history spec |
| Undo by mode | `Game.Undo` | `Undo_TwoPlayer_…`, `Undo_ComputerMode_…` |
| Scoreboard once per game + reset | `GameService.RecordResultOnce` | `Scoreboard_IsUpdatedOnlyOnce_…`, `ResetScoreboard_…` |
| Computer priority win→block→center→corner→any | `ComputerPlayer` | `ComputerPlayerTests` |
| Backend owns state; REST API | `GameService`, controllers | `ApiTests`, `game-api.service.spec.ts` |

## Design decisions

- **Backend is the single source of truth.** Every action returns the full `GameStateResponse`; the UI never computes rules, status or history itself.
- **The move list is the source of truth inside `Game`.** Board, current player, status, winner and winning cells are re-derived from it after every change (`Recalculate`). That makes Undo trivially correct ("recalculate win or draw status") and rules easy to test.
- **Computer reply happens inside the move request.** One round trip, atomic, no race between a human click and a separate "computer move" call.
- **`cellIndex` (0–8) in requests**, human-friendly `row`/`column`/`position` in history. Row-major indexing keeps `winningCells` simple for the UI.
- **Domain exceptions → HTTP codes** through one MVC exception filter, so controllers stay one-liners and every rejection has the same JSON shape. A filter (rather than exception middleware) keeps headers such as CORS intact on error responses.
- **One lock around the service** keeps each operation atomic; sufficient and simple for a local single-user app.
- **Angular:** standalone components, signals for state, three small presentational components plus one container. A `busy` flag prevents double-clicks; after a rejected request the UI re-fetches the game so it always shows what the backend holds.
- **Dev proxy** (`/api` → `localhost:5080`) so the browser sees one origin; CORS is still enabled as a fallback.

## Clarifications and assumptions

- **Clarification 2 — Option A chosen: Undo is disabled once a game is won or drawn.** The scoreboard result of a finished game is therefore final, which makes "update only once" trivially safe. (Option B would require tracking the recorded result per game and decrementing it when an undo reverses a win/draw.)
- The **scoreboard is session-level**: one scoreboard held by the backend for the lifetime of the API process, shared by all games (new games, mode switches and Reset Game do not touch it).
- **Reset Game** keeps the same game id and mode. **Switching mode** in the UI starts a new game (the scoreboard persists).
- In Computer mode the human is always X and always starts; moves by `O` from a client are rejected.
- X always moves first.
- `currentPlayer` is `null` when a game is completed.
- The computer is deterministic (lowest index wins ties), which keeps behaviour explainable and testable.

## AI tools and prompt summary

> **Edit this section with your own account before submitting.** The panel will ask what you prompted, what the AI generated and what you changed — the text below only records how this code base was produced.

- **Tool:** Claude (Anthropic) in claude.ai.
- **Input:** the *Round 2 – Problem Statement* document, with the prompt: *"create this game and include all the requirements as per mentioned in the document."*
- **Workflow:**
  1. Turned the document into a checklist (functional, must-have, backend, frontend, clarifications, testing, README) — see [Requirements traceability](#requirements-traceability).
  2. Generated the .NET domain first (`Game`, `ComputerPlayer`, `Scoreboard`), then service, controllers and error contract.
  3. Generated the Angular client against that contract (models → API service → components → container).
  4. Generated tests for every item in the testing checklist.
  5. Verification performed during generation: Angular unit tests (24) and the production build were run; the C# game rules and expected computer-move sequences were cross-checked against an independent re-implementation. **Run `dotnet test` yourself** before submission.
- **Reviewed carefully / to re-verify yourself:** win/draw logic (`Game.Recalculate`), Undo in both modes (`Game.Undo`), scoreboard-once guarantee (`GameService.RecordResultOnce` + `Game.ResultRecorded`), computer priority order, HTTP status mapping.
- **What I changed manually:** _(fill in)_
- **Assumptions and trade-offs I decided:** _(fill in — e.g. Option A for Undo, single global scoreboard)_

## Known limitations

- State is in memory: restarting the API loses games and scores (the UI then starts a new game).
- Finished games are never evicted, so the in-memory game dictionary only grows.
- One scoreboard for the whole API process (no per-user/per-browser sessions, no authentication).
- Undo after a finished game is not supported (Option A).
- The computer follows the specified heuristic; it is not an unbeatable (minimax) player.
- UI tests are component-level; there are no browser end-to-end tests.

## Future improvements

- Persist games and scores in SQLite (EF Core) and expire old games.
- Option B: allow Undo after completion with scoreboard adjustment.
- Per-browser sessions (cookie) with their own scoreboards.
- Computer difficulty levels (random / heuristic / minimax).
- Playwright end-to-end tests, Dockerfile / docker-compose for one-command start.
- Real-time two-device play with SignalR.
