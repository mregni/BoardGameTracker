import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { getCompare, getDefaultComparePair } from "@/services/queries/compare";
import { getPlayers } from "@/services/queries/players";
import { getSettings } from "@/services/queries/settings";
import { mirrorCompare, resolveComparePlayers } from "../-utils/compareUtils";

interface Props {
	left: number | undefined;
	right: number | undefined;
}

export const useCompareData = ({ left, right }: Props) => {
	const playersQuery = useQuery(getPlayers());
	const settingsQuery = useQuery(getSettings());

	const players = useMemo(() => playersQuery.data ?? [], [playersQuery.data]);
	const playerIds = useMemo(() => players.map((player) => player.id), [players]);
	const hasValidPair =
		left !== undefined &&
		right !== undefined &&
		left !== right &&
		playerIds.includes(left) &&
		playerIds.includes(right);

	const pairQuery = useQuery(getDefaultComparePair(playerIds.length >= 2 && !hasValidPair));
	const pairKnown = hasValidPair || pairQuery.isSuccess || pairQuery.isError;

	const [leftId, rightId] = useMemo(() => {
		if (!pairKnown) {
			return [0, 0];
		}

		const preferred = pairQuery.data ? [pairQuery.data.playerOneId, pairQuery.data.playerTwoId] : [];
		return resolveComparePlayers(playerIds, left, right, preferred);
	}, [pairKnown, pairQuery.data, playerIds, left, right]);

	const compareQuery = useQuery({
		...getCompare(leftId, rightId),
		placeholderData: (previous) =>
			previous?.playerOneId === rightId && previous.playerTwoId === leftId
				? { playerOneId: leftId, playerTwoId: rightId, result: mirrorCompare(previous.result) }
				: previous,
	});
	const shownLeftId = compareQuery.data?.playerOneId ?? 0;
	const shownRightId = compareQuery.data?.playerTwoId ?? 0;

	return {
		players,
		leftId,
		rightId,
		compare: compareQuery.data?.result,
		shownLeftId,
		shownRightId,
		isUpdating: compareQuery.isPlaceholderData && (shownLeftId !== leftId || shownRightId !== rightId),
		settings: settingsQuery.data,
		isLoading:
			playersQuery.isLoading ||
			settingsQuery.isLoading ||
			(playerIds.length >= 2 && !pairKnown) ||
			compareQuery.isLoading,
	};
};
