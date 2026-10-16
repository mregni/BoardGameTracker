import type { BgtSelectImageItem, Game } from "@/models";
import { splitSuggested } from "./splitSuggested";

export const toGameSelectItems = (
	games: Game[],
	suggestedIds: number[],
	suggestedLabel: string,
	allLabel: string,
): BgtSelectImageItem[] => {
	const { suggested, rest } = splitSuggested(games, suggestedIds);
	const toItem = (game: Game, group?: string) => ({ value: game.id, label: game.title, image: game.image, group });
	if (suggested.length === 0) return games.map((x) => toItem(x));
	return [...suggested.map((x) => toItem(x, suggestedLabel)), ...rest.map((x) => toItem(x, allLabel))];
};
