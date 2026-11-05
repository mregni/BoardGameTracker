import { readFile } from "node:fs/promises";
import path from "node:path";
import { expect, test as setup } from "@playwright/test";
import { credentials, readerCredentials, writerCredentials } from "../support/fixtures";
import {
	avatars,
	expansions,
	gameNights,
	games,
	linkedPlayer,
	loans,
	locations,
	pastGameNight,
	sessions,
} from "../support/seedData";

setup("seed the database through the api", async ({ request }) => {
	setup.setTimeout(120_000);

	const login = await request.post("/api/auth/login", { data: credentials });
	expect(login.ok()).toBe(true);
	const { accessToken, user } = await login.json();
	const headers = { Authorization: `Bearer ${accessToken}` };

	const send = async (method: "post" | "put", path: string, data: unknown) => {
		const response = await request[method](path, { data, headers });
		expect(response.ok(), `${path}: ${await response.text()}`).toBe(true);
		return response;
	};

	const post = async (path: string, data: unknown): Promise<{ id: number }> => (await send("post", path, data)).json();

	const upload = async (name: string, type: "Game" | "Profile", buffer: Buffer) => {
		const mimeType = name.endsWith(".png") ? "image/png" : "image/jpeg";
		const response = await request.post("/api/image", {
			headers,
			multipart: { file: { name, mimeType, buffer }, type },
		});
		expect(response.ok(), await response.text()).toBe(true);
		return (await response.text()).replaceAll('"', "");
	};

	const locationIds = new Map<string, number>();
	for (const name of locations) {
		const location = await post("/api/location", { name });
		locationIds.set(name, location.id);
	}

	const playerIds = new Map<string, number>();
	const supportFolder = path.join(setup.info().project.testDir, "support");
	for (const { name, avatar } of avatars) {
		const image = await upload(avatar, "Profile", await readFile(path.join(supportFolder, "avatars", avatar)));
		const player = await post("/api/player", { name, image });
		playerIds.set(name, player.id);
	}

	await send("put", `/api/admin/players/${playerIds.get(linkedPlayer)}/account`, { userId: user.id });

	const gameIds = new Map<string, number>();
	for (const { cover, ...game } of games) {
		const image = await upload(cover, "Game", await readFile(path.join(supportFolder, "covers", cover)));
		const created = await post("/api/game", { ...game, image });
		gameIds.set(game.title, created.id);
	}

	for (const expansion of expansions) {
		await post(`/api/game/${gameIds.get(expansion.game)}/expansions/manual`, { title: expansion.title });
	}

	for (const night of gameNights) {
		const created = await post("/api/gamenight", {
			title: night.title,
			notes: "",
			startDate: night.startDate.toISOString(),
			hostId: playerIds.get(linkedPlayer),
			locationId: locationIds.get(night.location),
			suggestedGameIds: night.suggestedGames.map((title) => gameIds.get(title)),
			invitedPlayerIds: Object.keys(night.rsvps).map((name) => playerIds.get(name)),
		});
		const detail = await (await request.get(`/api/gamenight/${created.id}`, { headers })).json();
		for (const [name, state] of Object.entries(night.rsvps)) {
			const playerId = playerIds.get(name);
			const rsvp = detail.gameNight.invitedPlayers.find((x: { playerId: number }) => x.playerId === playerId);
			await send("put", "/api/gamenight/rsvp", { id: rsvp.id, gameNightId: created.id, playerId, state });
		}
	}
	const pastNight = await post("/api/gamenight", {
		title: pastGameNight.title,
		notes: "",
		startDate: pastGameNight.startDate.toISOString(),
		hostId: playerIds.get(linkedPlayer),
		locationId: locationIds.get(pastGameNight.location),
		suggestedGameIds: pastGameNight.suggestedGames.map((title) => gameIds.get(title)),
		invitedPlayerIds: pastGameNight.attendees.map((name) => playerIds.get(name)),
	});

	for (const session of sessions) {
		await post("/api/session", {
			gameId: gameIds.get(session.game),
			start: session.start.toISOString(),
			minutes: session.minutes,
			locationId: locationIds.get(session.location),
			gameNightId: session.gameNight ? pastNight.id : undefined,
			expansionIds: [],
			isCoop: session.isCoop,
			teamWon: session.teamWon,
			playerSessions: session.scores.map(({ player, score }) => ({
				playerId: playerIds.get(player),
				score,
				firstPlay: false,
				won: player === session.winner || session.teamWon === true,
			})),
		});
	}

	await send("post", `/api/gamenight/${pastNight.id}/end`, {});

	for (const loan of loans) {
		const created = await post("/api/loans", {
			gameId: gameIds.get(loan.game),
			playerId: playerIds.get(loan.player),
			loanDate: loan.loanDate.toISOString(),
			dueDate: loan.dueDate?.toISOString() ?? null,
		});
		if (loan.returned) await send("put", "/api/loans/return", { id: created.id, returnDate: new Date().toISOString() });
	}

	for (const user of [readerCredentials, writerCredentials]) {
		await send("post", "/api/auth/register", {
			username: user.username,
			email: `${user.username}@example.test`,
			password: user.password,
			role: user.role,
		});
	}
});
