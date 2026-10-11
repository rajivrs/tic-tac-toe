import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { emptyGame } from '../testing/game-fixtures';
import { GameApiService } from './game-api.service';

describe('GameApiService (REST contract)', () => {
  let service: GameApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(GameApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('POST /api/games creates a game with the selected mode', () => {
    service.createGame('VsComputer').subscribe((g) => expect(g.id).toBe('game-1'));
    const req = http.expectOne('/api/games');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ mode: 'VsComputer' });
    req.flush(emptyGame());
  });

  it('GET /api/games/{id} loads the current state', () => {
    service.getGame('abc').subscribe();
    const req = http.expectOne('/api/games/abc');
    expect(req.request.method).toBe('GET');
    req.flush(emptyGame());
  });

  it('POST /api/games/{id}/moves submits player and cell index', () => {
    service.playMove('abc', 'X', 4).subscribe();
    const req = http.expectOne('/api/games/abc/moves');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ player: 'X', cellIndex: 4 });
    req.flush(emptyGame());
  });

  it('POST /api/games/{id}/undo and /reset', () => {
    service.undo('abc').subscribe();
    expect(http.expectOne('/api/games/abc/undo').request.method).toBe('POST');
    service.resetGame('abc').subscribe();
    expect(http.expectOne('/api/games/abc/reset').request.method).toBe('POST');
  });

  it('GET /api/scoreboard and POST /api/scoreboard/reset', () => {
    service.getScoreboard().subscribe();
    expect(http.expectOne('/api/scoreboard').request.method).toBe('GET');
    service.resetScoreboard().subscribe();
    expect(http.expectOne('/api/scoreboard/reset').request.method).toBe('POST');
  });
});
