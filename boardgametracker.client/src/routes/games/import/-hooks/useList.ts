import { useMutation, useQueries } from "@tanstack/react-query";
import { useCallback, useMemo, useState } from "react";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import type { Game, ImportGame } from "@/models";
import { useIntegrationSettings } from "@/routes/-hooks/useIntegrationSettings";
import { useToasts } from "@/routes/-hooks/useToasts";
import { importGamesCall } from "@/services/gameService";
import { getBggCollection, getGames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { classifyImportError, needsIntegrationSettings } from "../-utils/importErrors";

export const IMPORT_BATCH_SIZE = 5;

const byTitle = (a: ImportGame, b: ImportGame) => a.title.localeCompare(b.title);

const missingBaseGames = (bggGames: ImportGame[], collectionGames: Game[] | undefined): ImportGame[] => {
	const listed = new Set(bggGames.map((game) => game.bggId));
	const missing = new Map<number, ImportGame>();
	for (const expansion of bggGames) {
		const baseGameBggId = expansion.baseGameBggId;
		if (baseGameBggId == null || listed.has(baseGameBggId) || missing.has(baseGameBggId)) continue;
		const existing = collectionGames?.find((game) => game.bggId === baseGameBggId);
		missing.set(baseGameBggId, {
			...expansion,
			bggId: baseGameBggId,
			title: existing?.title ?? expansion.baseGameTitle ?? "",
			imageUrl: existing?.image ?? "",
			state: existing?.state ?? expansion.state,
			inCollection: !!existing,
			price: existing?.buyingPrice ?? 0,
			hasScoring: existing?.hasScoring ?? true,
			baseGameBggId: null,
			baseGameTitle: null,
			addedForExpansion: !existing,
			checked: false,
		});
	}
	return [...missing.values()];
};

export const groupUnderBaseGames = (games: ImportGame[]): ImportGame[] => {
	const expansions = new Map<number, ImportGame[]>();
	for (const game of games) {
		if (game.baseGameBggId == null) continue;
		expansions.set(game.baseGameBggId, [...(expansions.get(game.baseGameBggId) ?? []), game]);
	}
	return games
		.filter((game) => game.baseGameBggId == null)
		.sort(byTitle)
		.flatMap((game) => [game, ...(expansions.get(game.bggId) ?? []).sort(byTitle)]);
};

export const importBatches = (selected: ImportGame[], size = IMPORT_BATCH_SIZE): ImportGame[][] => {
	const groups = new Map<number, ImportGame[]>();
	for (const game of selected) {
		const key = game.baseGameBggId ?? game.bggId;
		const group = groups.get(key) ?? [];
		if (game.baseGameBggId == null) group.unshift(game);
		else group.push(game);
		groups.set(key, group);
	}
	const batches: ImportGame[][] = [];
	let current: ImportGame[] = [];
	for (const group of groups.values()) {
		if (current.length > 0 && current.length + group.length > size) {
			batches.push(current);
			current = [];
		}
		current = [...current, ...group];
	}
	if (current.length > 0) batches.push(current);
	return batches;
};

interface Props {
	username: string;
}

export const useList = ({ username }: Props) => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();
	const { integrationSettingsAction } = useIntegrationSettings();

	const [bggCollectionQuery, gamesQuery, settingsQuery] = useQueries({
		queries: [getBggCollection(username), getGames(), getSettings()],
	});

	const settings = settingsQuery.data;
	const bggError = bggCollectionQuery.error;
	const bggErrorKind = bggError ? classifyImportError(bggError) : null;
	const retryCollection = useCallback(() => bggCollectionQuery.refetch(), [bggCollectionQuery]);

	const [filterCollected, setFilterCollected] = useState<boolean>(true);

	const processingGames = bggCollectionQuery.isLoading || gamesQuery.isLoading;

	const totalCount = useMemo(() => {
		return bggCollectionQuery.data?.length ?? 0;
	}, [bggCollectionQuery.data]);

	const processedGames = useMemo(() => {
		const bggGames = bggCollectionQuery.data;
		const collectionGames = gamesQuery.data;

		if (!bggGames) return [];

		const collectionExpansions = (collectionGames ?? []).flatMap((game) => game.expansions);
		const listed = bggGames.map((game) => {
			if (game.baseGameBggId != null) {
				const existingExpansion = collectionExpansions.find((expansion) => expansion.bggId === game.bggId);
				return {
					...game,
					inCollection: !!existingExpansion,
					price: existingExpansion?.buyingPrice ?? 0,
					addedDate: existingExpansion?.additionDate
						? new Date(existingExpansion.additionDate)
						: new Date(game.lastModified),
					hasScoring: false,
					checked: false,
				};
			}
			const existingGame = collectionGames?.find((g) => g.bggId === game.bggId);
			return {
				...game,
				inCollection: !!existingGame,
				price: existingGame?.buyingPrice ?? 0,
				addedDate: existingGame?.additionDate ? new Date(existingGame.additionDate) : new Date(game.lastModified),
				hasScoring: existingGame?.hasScoring ?? true,
				checked: false,
			};
		});
		return groupUnderBaseGames([...listed, ...missingBaseGames(listed, collectionGames)]);
	}, [bggCollectionQuery.data, gamesQuery.data]);

	const inCollectionCount = useMemo(() => {
		return processedGames.filter((game) => game.inCollection && !game.addedForExpansion).length;
	}, [processedGames]);

	const [localUpdates, setLocalUpdates] = useState<Map<number, Partial<ImportGame>>>(new Map());

	const games = useMemo(() => {
		const merged = processedGames.map((game) => {
			const updates = localUpdates.get(game.bggId);
			const withUpdates = updates ? { ...game, ...updates } : game;
			return withUpdates.inCollection ? { ...withUpdates, checked: false } : withUpdates;
		});
		const checkedBaseGames = new Set(
			merged.filter((game) => game.checked && game.baseGameBggId != null).map((game) => game.baseGameBggId),
		);
		const openExpansions = new Set(
			merged.filter((game) => !game.inCollection && game.baseGameBggId != null).map((game) => game.baseGameBggId),
		);
		return merged
			.map((game) =>
				!game.inCollection && checkedBaseGames.has(game.bggId)
					? { ...game, checked: true, lockedByExpansion: true }
					: { ...game, lockedByExpansion: false },
			)
			.filter((game) => {
				if (game.addedForExpansion) return openExpansions.has(game.bggId);
				if (!filterCollected || !game.inCollection) return true;
				return game.baseGameBggId == null && openExpansions.has(game.bggId);
			});
	}, [processedGames, filterCollected, localUpdates]);

	const updateGame = useCallback((bggId: number, updates: Partial<ImportGame>) => {
		setLocalUpdates((prev) => {
			const next = new Map(prev);
			next.set(bggId, { ...prev.get(bggId), ...updates });
			return next;
		});
	}, []);

	const setSelection = useCallback((changes: { bggId: number; checked: boolean }[]) => {
		setLocalUpdates((prev) => {
			const next = new Map(prev);
			for (const { bggId, checked } of changes) {
				next.set(bggId, { ...next.get(bggId), checked });
			}
			return next;
		});
	}, []);

	const [importProgress, setImportProgress] = useState({ done: 0, total: 0 });

	const startImportMutation = useMutation({
		mutationFn: async (selected: ImportGame[]) => {
			setImportProgress({ done: 0, total: selected.length });
			let done = 0;
			for (const batch of importBatches(selected)) {
				await importGamesCall(batch);
				done += batch.length;
				setImportProgress({ done, total: selected.length });
			}
		},
		async onSettled() {
			await invalidator.invalidateGameCreated();
		},
		onSuccess() {
			successToast("games:import.success");
		},
		onError(error) {
			const kind = classifyImportError(error);
			errorToast(`games:import.failed-${kind}`, needsIntegrationSettings(kind) ? integrationSettingsAction : undefined);
		},
	});

	return {
		games,
		settings,
		bggError,
		bggErrorKind,
		retryCollection,
		updateGame,
		setSelection,
		filterCollected,
		setFilterCollected,
		inCollectionCount,
		processingGames,
		totalCount,
		startImport: startImportMutation.mutate,
		importing: startImportMutation.isPending,
		importProgress,
	};
};
