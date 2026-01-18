import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, userEvent, renderWithTheme, render } from '@/test/test-utils';

import { BgtPlayerAvatar } from './BgtPlayerAvatar';
import { Game, Player, PlayerSession, GameState } from '@/models';

const mockNavigate = vi.fn();

vi.mock('@tanstack/react-router', () => ({
  useNavigate: () => mockNavigate,
}));

describe('BgtPlayerAvatar', () => {
  const createPlayerSession = (overrides: Partial<PlayerSession> = {}): PlayerSession => ({
    id: 1,
    playerId: 1,
    sessionId: 1,
    score: 100,
    isWinner: false,
    firstPlay: false,
    ...overrides,
  });

  const createPlayer = (overrides: Partial<Player> = {}): Player => ({
    id: 1,
    name: 'John Doe',
    image: '/player.jpg',
    ...overrides,
  });

  const createGame = (overrides: Partial<Game> = {}): Game =>
    ({
      id: 1,
      title: 'Catan',
      hasScoring: true,
      state: GameState.Owned,
      ...overrides,
    }) as Game;

  beforeEach(() => {
    mockNavigate.mockClear();
  });

  describe('Rendering', () => {
    it('should render player name in title when game has no scoring', () => {
      renderWithTheme(
        <BgtPlayerAvatar
          playerSession={createPlayerSession()}
          player={createPlayer()}
          game={createGame({ hasScoring: false })}
        />
      );
      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('alt', 'John Doe');
    });

    it('should render player name with score when game has scoring', () => {
      renderWithTheme(
        <BgtPlayerAvatar
          playerSession={createPlayerSession({ score: 150 })}
          player={createPlayer()}
          game={createGame({ hasScoring: true })}
        />
      );
      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('alt', 'John Doe (150)');
    });

    it('should render player image', () => {
      renderWithTheme(
        <BgtPlayerAvatar
          playerSession={createPlayerSession()}
          player={createPlayer({ image: '/avatar.png' })}
          game={createGame()}
        />
      );
      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('src', '/avatar.png');
    });

    it('should render first letter when no image', () => {
      renderWithTheme(
        <BgtPlayerAvatar
          playerSession={createPlayerSession()}
          player={createPlayer({ image: null })}
          game={createGame()}
        />
      );
      expect(screen.getByText('J')).toBeInTheDocument();
    });
  });

  describe('Null Handling', () => {
    it('should return null when player is undefined', () => {
      const { container } = render(
        <BgtPlayerAvatar playerSession={createPlayerSession()} player={undefined} game={createGame()} />
      );
      expect(container.firstChild).toBeNull();
    });

    it('should return null when game is undefined', () => {
      const { container } = render(
        <BgtPlayerAvatar playerSession={createPlayerSession()} player={createPlayer()} game={undefined} />
      );
      expect(container.firstChild).toBeNull();
    });
  });

  describe('Navigation', () => {
    it('should navigate to player page on click', async () => {
      const user = userEvent.setup();
      renderWithTheme(
        <BgtPlayerAvatar playerSession={createPlayerSession()} player={createPlayer({ id: 42 })} game={createGame()} />
      );

      await user.click(screen.getByRole('img'));

      expect(mockNavigate).toHaveBeenCalledWith({ to: '/players/42' });
    });
  });

  describe('Combined Props', () => {
    it('should handle all props correctly', async () => {
      const user = userEvent.setup();
      renderWithTheme(
        <BgtPlayerAvatar
          playerSession={createPlayerSession({ playerId: 5, sessionId: 10, score: 200 })}
          player={createPlayer({ id: 5, name: 'Alice', image: '/alice.jpg' })}
          game={createGame({ hasScoring: true })}
        />
      );

      const img = screen.getByRole('img');
      expect(img).toHaveAttribute('alt', 'Alice (200)');
      expect(img).toHaveAttribute('src', '/alice.jpg');

      await user.click(img);
      expect(mockNavigate).toHaveBeenCalledWith({ to: '/players/5' });
    });
  });
});
