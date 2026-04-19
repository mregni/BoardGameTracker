import { formOptions } from "@tanstack/react-form";
import { GameState } from "@/models/Games/GameState";

export const gameFormOpts = formOptions({
	defaultValues: {
		id: undefined as number | undefined,
		title: "",
		hasScoring: true,
		description: "",
		state: GameState.Wanted as GameState,
		yearPublished: undefined as number | undefined,
		maxPlayers: undefined as number | undefined,
		minPlayers: undefined as number | undefined,
		maxPlayTime: undefined as number | undefined,
		minPlayTime: undefined as number | undefined,
		minAge: undefined as number | undefined,
		bggId: undefined as number | undefined,
		buyingPrice: 0,
		additionDate: "",
		image: null as string | null,
	},
});
