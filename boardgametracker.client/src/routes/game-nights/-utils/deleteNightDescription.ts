import type { TFunction } from "i18next";
import type { GameNight } from "@/models";

export const deleteNightDescription = (t: TFunction, gameNight: GameNight | null) => {
	if (!gameNight) return "";

	const { runningGames, gamesPlayed } = gameNight.summary;
	return [
		t("game-nights:delete.description", { title: gameNight.title }),
		runningGames > 0 && t("game-nights:delete.running", { count: runningGames }),
		gamesPlayed > 0 && t("game-nights:delete.sessions", { count: gamesPlayed }),
		t("game-nights:delete.rsvps"),
	]
		.filter(Boolean)
		.join(" ");
};
