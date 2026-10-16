import { formOptions } from "@tanstack/react-form";

import type { CreateSessionPlayer } from "@/models";

export const sessionFormOpts = formOptions({
	defaultValues: {
		gameId: 0,
		locationId: 0,
		minutes: 30,
		comment: "",
		start: new Date(),
		playerSessions: [] as CreateSessionPlayer[],
		isCoop: false,
		teamWon: null as boolean | null,
		teamScore: null as number | null,
	},
});
