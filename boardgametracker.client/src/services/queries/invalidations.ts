import { QueryClient } from '@tanstack/react-query';

import { QUERY_KEYS } from '@/models';

export class QueryInvalidator {
  constructor(private queryClient: QueryClient) {}

  async invalidateGame(gameId: number) {
    await Promise.all([
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] }),
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.game, gameId] }),
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.statistics],
      }),
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.sessions],
      }),
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.expansions],
      }),
      this.invalidateDashboard(),
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.counts] }),
    ]);
  }

  async invalidateSession(sessionId: number, gameId: number) {
    await Promise.all([
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.sessions] }),
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.sessions, sessionId] }),
      this.invalidateGame(gameId),
      this.invalidatePlayers(),
      this.invalidateDashboard(),
    ]);
  }

  async invalidatePlayer(playerId: number) {
    await Promise.all([
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] }),
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.player, playerId] }),
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.statistics],
      }),
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.sessions],
      }),
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.player, playerId, QUERY_KEYS.badges],
      }),
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.compare] }),
      this.invalidateDashboard(),
    ]);
  }

  async invalidateLoan(loanId?: number) {
    await Promise.all([
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] }),
      ...(loanId ? [this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans, loanId] })] : []),
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] }),
    ]);
  }

  async invalidateLocation(locationId?: number) {
    await Promise.all([
      this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] }),
      ...(locationId ? [this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations, locationId] })] : []),
    ]);
  }

  async invalidateDashboard() {
    await Promise.all([
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.dashboard, QUERY_KEYS.statistics],
      }),
      this.queryClient.invalidateQueries({
        queryKey: [QUERY_KEYS.dashboard, QUERY_KEYS.charts],
      }),
    ]);
  }

  async invalidatePlayers() {
    await this.queryClient.invalidateQueries({
      queryKey: [QUERY_KEYS.players],
    });
  }

  async invalidateGames() {
    await this.queryClient.invalidateQueries({
      queryKey: [QUERY_KEYS.games],
    });
  }

  async invalidateSettings() {
    await this.queryClient.invalidateQueries({
      queryKey: [QUERY_KEYS.settings],
    });
  }

  async invalidateCompare() {
    await this.queryClient.invalidateQueries({
      queryKey: [QUERY_KEYS.compare],
    });
  }

  async invalidateAll() {
    await this.queryClient.invalidateQueries();
  }
}

export const invalidateGameRelated = (queryClient: QueryClient, gameId: number) => {
  const invalidator = new QueryInvalidator(queryClient);
  return invalidator.invalidateGame(gameId);
};

export const invalidateSessionRelated = (queryClient: QueryClient, sessionId: number, gameId: number) => {
  const invalidator = new QueryInvalidator(queryClient);
  return invalidator.invalidateSession(sessionId, gameId);
};

export const invalidatePlayerRelated = (queryClient: QueryClient, playerId: number) => {
  const invalidator = new QueryInvalidator(queryClient);
  return invalidator.invalidatePlayer(playerId);
};

export const invalidateLoanRelated = (queryClient: QueryClient, loanId?: number) => {
  const invalidator = new QueryInvalidator(queryClient);
  return invalidator.invalidateLoan(loanId);
};

export const invalidateLocationRelated = (queryClient: QueryClient, locationId?: number) => {
  const invalidator = new QueryInvalidator(queryClient);
  return invalidator.invalidateLocation(locationId);
};

export const invalidateDashboardRelated = (queryClient: QueryClient) => {
  const invalidator = new QueryInvalidator(queryClient);
  return invalidator.invalidateDashboard();
};
