import type { PlayerRival, Session } from "@/models";

export const FORM_LENGTH = 10;
const NEMESIS_MIN_SESSIONS = 3;

export const wonSession = (session: Session, playerId: number) =>
	session.isCoop ? session.teamWon === true : session.playerSessions.some((ps) => ps.playerId === playerId && ps.won);

export const winnerIdsOf = (session: Session, playerId: number) =>
	session.isCoop
		? []
		: session.playerSessions.filter((ps) => ps.won && ps.playerId !== playerId).map((ps) => ps.playerId);

export const formOf = (sessions: Session[], playerId: number) =>
	[...sessions]
		.sort((a, b) => new Date(b.start).getTime() - new Date(a.start).getTime())
		.slice(0, FORM_LENGTH)
		.reverse()
		.map((session) => ({ id: session.id, won: wonSession(session, playerId) }));

export const nemesisOf = (rivals: PlayerRival[]) =>
	rivals
		.filter((rival) => rival.sessions >= NEMESIS_MIN_SESSIONS && rival.losses > rival.wins)
		.sort((a, b) => b.losses - b.wins - (a.losses - a.wins) || b.losses - a.losses || a.playerId - b.playerId)[0] ??
	null;
