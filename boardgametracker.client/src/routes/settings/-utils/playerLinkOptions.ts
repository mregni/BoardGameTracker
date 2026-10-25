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

export const buildPlayerLinkItems = (
	players: Player[],
	users: UserDto[],
	userId: string,
	linkedHint: (username: string) => string,
): BgtSelectItem[] =>
	players.map((p) => {
		const owner = users.find((u) => u.id !== userId && u.playerId === p.id);
		return owner ? { value: p.id, label: p.name, hint: linkedHint(owner.username) } : { value: p.id, label: p.name };
	});
