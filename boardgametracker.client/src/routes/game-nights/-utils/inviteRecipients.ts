import { type GameNight, GameNightRsvpState, type Player } from "@/models";
import { byName, sortRsvps } from "@/utils/sortRsvps";

export interface InviteRecipients {
	withEmail: string[];
	withoutEmail: string[];
}

export const inviteRecipients = (gameNight: GameNight, players: Player[]): InviteRecipients => {
	const pending = sortRsvps(gameNight.invitedPlayers, gameNight.hostId, byName).filter(
		(rsvp) => rsvp.state === GameNightRsvpState.Pending,
	);
	const emailOf = (playerId: number) => players.find((player) => player.id === playerId)?.email?.trim() ?? "";

	return {
		withEmail: pending.filter((rsvp) => emailOf(rsvp.playerId) !== "").map((rsvp) => rsvp.player.name),
		withoutEmail: pending.filter((rsvp) => emailOf(rsvp.playerId) === "").map((rsvp) => rsvp.player.name),
	};
};

export type InviteBlocker = "email-disabled" | "public-url-missing" | null;

export const inviteBlocker = (emailEnabled: boolean, publicUrlConfigured: boolean): InviteBlocker => {
	if (!emailEnabled) return "email-disabled";
	if (!publicUrlConfigured) return "public-url-missing";
	return null;
};
