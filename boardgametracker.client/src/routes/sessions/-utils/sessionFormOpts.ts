import { formOptions } from "@tanstack/react-form";

import type { CreatePlayerSessionNoScoring, CreateSessionPlayer } from "@/models";

export const sessionFormOpts = formOptions({
	defaultValues: {
		gameId: 0,
		locationId: 0,
		minutes: 30,
		comment: "",
		start: new Date(),
		playerSessions: [] as (CreateSessionPlayer | CreatePlayerSessionNoScoring)[],
	},
});
