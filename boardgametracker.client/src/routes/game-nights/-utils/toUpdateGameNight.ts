import type { GameNight, UpdateGameNight } from "@/models";

export const toUpdateGameNight = (gameNight: GameNight): UpdateGameNight => ({
	id: gameNight.id,
	title: gameNight.title,
	notes: gameNight.notes,
	startDate: new Date(gameNight.startDate),
	hostId: gameNight.hostId,
	locationId: gameNight.locationId,
	suggestedGameIds: gameNight.suggestedGames.map((game) => game.id),
	invitedPlayerIds: gameNight.invitedPlayers
		.filter((rsvp) => rsvp.playerId !== gameNight.hostId)
		.map((rsvp) => rsvp.playerId),
});
