import type { BgtSelectItem, Player, UserDto } from "@/models";

export const buildLinkablePlayerItems = (
	players: Player[],
	users: UserDto[],
	excludeUserId?: string,
): BgtSelectItem[] => {
	const linkedByOthers = new Set(
		users.filter((u) => u.id !== excludeUserId && u.playerId != null).map((u) => u.playerId as number),
	);

	return players.filter((p) => !linkedByOthers.has(p.id)).map((p) => ({ value: p.id, label: p.name }));
};
