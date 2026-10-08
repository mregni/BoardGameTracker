import type { QueryClient } from "@tanstack/react-query";

import { QUERY_KEYS } from "@/models";

export class QueryInvalidator {
	constructor(private readonly queryClient: QueryClient) {}

	async invalidateGame(gameId: number) {
		await Promise.all([
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] }),
			this.queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, gameId],
			}),
			this.queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.statistics],
			}),
			this.queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.sessions],
			}),
			this.queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.expansions],
			}),
			this.queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.game, gameId, QUERY_KEYS.manuals],
			}),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.trackedPrices] }),
			this.invalidateDashboard(),
			this.invalidateCounts(),
			this.invalidateShames(),
		]);
	}

	async invalidateGameCreated() {
		await Promise.all([
			this.invalidateGames(),
			this.invalidateDashboard(),
			this.invalidateCounts(),
			this.invalidateShames(),
		]);
	}

	async invalidateGameDeleted() {
		await Promise.all([
			this.invalidateGames(),
			this.invalidateDashboard(),
			this.invalidateCounts(),
			this.invalidateShames(),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] }),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] }),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.trackedPrices] }),
			this.invalidatePlayers(),
		]);
	}

	async invalidateSession(sessionId: number, gameId: number, playerIds: number[] = []) {
		await Promise.all([
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.sessions] }),
			this.queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.sessions, sessionId],
			}),
			this.invalidateGame(gameId),
			this.invalidatePlayers(),
			...playerIds.map((playerId) => this.invalidatePlayer(playerId)),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] }),
			this.invalidateCompare(),
			this.invalidateLeaderboard(),
		]);
	}

	async invalidateSessionDeleted(gameId?: number, playerIds: number[] = []) {
		await Promise.all([
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.sessions] }),
			gameId !== undefined ? this.invalidateGame(gameId) : this.invalidateGames(),
			this.invalidatePlayers(),
			...playerIds.map((playerId) => this.invalidatePlayer(playerId)),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] }),
			this.invalidateCompare(),
			this.invalidateCounts(),
			this.invalidateShames(),
			this.invalidateDashboard(),
			this.invalidateLeaderboard(),
		]);
	}

	async invalidateLeaderboard() {
		await this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.leaderboard] });
	}

	async invalidatePlayer(playerId: number) {
		await Promise.all([
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] }),
			this.queryClient.invalidateQueries({
				queryKey: [QUERY_KEYS.player, playerId],
			}),
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

	async invalidateLoan(loanId?: number, gameId?: number) {
		await Promise.all([
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.loans] }),
			...(loanId
				? [
						this.queryClient.invalidateQueries({
							queryKey: [QUERY_KEYS.loans, loanId],
						}),
					]
				: []),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.games] }),
			...(gameId
				? [
						this.queryClient.invalidateQueries({
							queryKey: [QUERY_KEYS.game, gameId],
						}),
					]
				: []),
			this.invalidateCounts(),
		]);
	}

	async invalidateLocation(locationId?: number) {
		await Promise.all([
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.locations] }),
			...(locationId
				? [
						this.queryClient.invalidateQueries({
							queryKey: [QUERY_KEYS.locations, locationId],
						}),
					]
				: []),
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

	async invalidateCounts() {
		await this.queryClient.invalidateQueries({
			queryKey: [QUERY_KEYS.counts],
		});
	}

	async invalidateShames() {
		await this.queryClient.invalidateQueries({
			queryKey: [QUERY_KEYS.shames],
		});
	}

	async invalidateSettings() {
		await Promise.all([
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.settings] }),
			this.queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.trackedPrices] }),
			this.queryClient.invalidateQueries({
				predicate: (query) => query.queryKey[0] === QUERY_KEYS.game && query.queryKey[2] === QUERY_KEYS.price,
			}),
		]);
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
