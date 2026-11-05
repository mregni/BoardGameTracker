import { subDays, subMonths, subSeconds } from "date-fns";
import type { components } from "../../src/models/api.generated";

type GameState = components["schemas"]["GameState"];

const now = new Date();

const collection = [
	{
		title: "Sheep in Disguise",
		cover: "sheep-in-disguise.png",
		yearPublished: 2022,
		minPlayers: 2,
		maxPlayers: 6,
		minPlayTime: 20,
		maxPlayTime: 45,
		minAge: 10,
		rating: 6.8,
		weight: 1.2,
		buyingPrice: 53,
		description: "Collect disguised sheep and protect their true identities or risk losing them all!",
	},
	{
		title: "Dwar7s Winter",
		cover: "dwar7s-winter.jpg",
		yearPublished: 2018,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 30,
		maxPlayTime: 45,
		minAge: 13,
		rating: 6.7,
		weight: 2.6,
		buyingPrice: 146,
		description: "Defending your kingdom thru harsh winter with tower defense gameplay.",
	},
	{
		title: "Redwood",
		cover: "redwood.png",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 45,
		maxPlayTime: 60,
		minAge: 10,
		rating: 7.2,
		weight: 2.1,
		buyingPrice: 140,
		description: "Wildlife photography adventures in magnificent North American landscapes.",
	},
	{
		title: "Botany",
		cover: "botany.png",
		yearPublished: 2024,
		minPlayers: 1,
		maxPlayers: 5,
		minPlayTime: 45,
		maxPlayTime: 90,
		minAge: 8,
		rating: 7,
		weight: 2.1,
		buyingPrice: 107,
		description: "Flower hunters compete to find fortune and fame and be named the Royal Botanist.",
	},
	{
		title: "Earth",
		cover: "earth.jpg",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 5,
		minPlayTime: 45,
		maxPlayTime: 90,
		minAge: 13,
		rating: 7.6,
		weight: 2.9,
		buyingPrice: 84,
		description: "Strategically grow your ecosystem card engine with unique flora, fauna, and terrains.",
	},
	{
		title: "City of the Great Machine",
		cover: "city-of-the-great-machine.jpg",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 60,
		maxPlayTime: 90,
		minAge: 14,
		rating: 7.7,
		weight: 3.6,
		buyingPrice: 97,
		description: "Solo/Coop/One-vs-Many strategy game set in the Victorian steampunk universe.",
	},
	{
		title: "Tend",
		cover: "tend.png",
		yearPublished: 2025,
		minPlayers: 1,
		maxPlayers: 6,
		minPlayTime: 60,
		maxPlayTime: 90,
		minAge: 14,
		rating: 7.9,
		weight: 3.1,
		buyingPrice: 107,
		description: "Farm, explore, and fish on an alien world.",
	},
	{
		title: "Bardwood Grove",
		cover: "bardwood-grove.jpg",
		yearPublished: 2024,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 60,
		maxPlayTime: 90,
		minAge: 14,
		rating: 7.3,
		weight: 3.3,
		buyingPrice: 85,
		description: "Craft songs, tell tales, play shows, save creatures, and boast to earn glory!",
	},
	{
		title: "Power Plants",
		cover: "power-plants-kickstarter-edition.jpg",
		yearPublished: 2022,
		minPlayers: 1,
		maxPlayers: 5,
		minPlayTime: 30,
		maxPlayTime: 30,
		minAge: 8,
		rating: 7.6,
		weight: 2.8,
		buyingPrice: 42,
		description: "A strategic tile-laying area control game for 1-5 wizards.",
	},
	{
		title: "Moon",
		cover: "moon.png",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 5,
		minPlayTime: 45,
		maxPlayTime: 90,
		minAge: 10,
		rating: 7.3,
		weight: 2.5,
		buyingPrice: 144,
		description: "Construct the most popular Moon base by drafting cards and placing rovers.",
	},
	{
		title: "Shallow Sea",
		cover: "shallow-sea.jpg",
		yearPublished: 2025,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 30,
		maxPlayTime: 45,
		minAge: 14,
		rating: 7.9,
		weight: 2.3,
		buyingPrice: 63,
		description: "A multi-layer puzzle board game featuring marine animals of the Great Barrier Reef",
	},
	{
		title: "Evolution: Another World",
		cover: "evolution-another-world.png",
		yearPublished: 2024,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 20,
		maxPlayTime: 30,
		minAge: 11,
		rating: 6.2,
		weight: 1.9,
		buyingPrice: 49,
		description: "Create creatures and help them evolve in a strange magical world!",
	},
	{
		title: "Reviving Kathmandu",
		cover: "reviving-kathmandu.jpg",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 6,
		minPlayTime: 20,
		maxPlayTime: 40,
		minAge: 8,
		rating: 6.9,
		weight: 1.8,
		buyingPrice: 56,
		description: "Bid, collect & rebuild the earthquake-hit city of Kathmandu!",
	},
	{
		title: "En Route",
		cover: "en-route.png",
		yearPublished: 2025,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 20,
		maxPlayTime: 40,
		minAge: 14,
		rating: 7.4,
		weight: 2.2,
		buyingPrice: 52,
		description: "Become the best tour guide in a flip-and-write game with intense player interaction.",
	},
	{
		title: "Iliad",
		cover: "iliad.png",
		yearPublished: 2025,
		minPlayers: 2,
		maxPlayers: 2,
		minPlayTime: 30,
		maxPlayTime: 30,
		minAge: 10,
		rating: 7.6,
		weight: 2,
		buyingPrice: 56,
		description: "Vie for the favor of the gods in the Trojan War struggle between Hector and Achilles.",
	},
	{
		title: "Everdell Duo",
		cover: "everdell-duo.png",
		yearPublished: 2025,
		minPlayers: 1,
		maxPlayers: 2,
		minPlayTime: 30,
		maxPlayTime: 30,
		minAge: 10,
		rating: 7.6,
		weight: 2.7,
		buyingPrice: 192,
		description: "Use resources to play cards and create your own woodland city.",
	},
	{
		title: "Flamecraft Duals",
		cover: "flamecraft-duals.png",
		yearPublished: 2025,
		minPlayers: 1,
		maxPlayers: 2,
		minPlayTime: 30,
		maxPlayTime: 45,
		minAge: 10,
		rating: 7.9,
		weight: 1.6,
		buyingPrice: 107,
		description: "Place dragons, fire up powers and match the most shops!",
	},
	{
		title: "Kittens in Space",
		cover: "kittens-in-space.jpg",
		yearPublished: 2025,
		minPlayers: 1,
		maxPlayers: 6,
		minPlayTime: 10,
		maxPlayTime: 15,
		minAge: 7,
		rating: 6.9,
		weight: 1.6,
		buyingPrice: 51,
		description: "Rescue stray space kittens by shedding cards onto shifting stacks in a lunar shelter.",
	},
	{
		title: "Lairs",
		cover: "lairs.jpg",
		yearPublished: 2026,
		minPlayers: 2,
		maxPlayers: 2,
		minPlayTime: 30,
		maxPlayTime: 60,
		minAge: 10,
		rating: 8.1,
		weight: 2.3,
		buyingPrice: 112,
		description: "Build a devious dungeon and explore your rival's creation.",
	},
	{
		title: "Mythwind",
		cover: "mythwind.png",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 30,
		maxPlayTime: 60,
		minAge: 13,
		rating: 7.5,
		weight: 2.8,
		buyingPrice: 81,
		description: "Solo & cooperative 'cozy' board game set in a whimsical valley with magical sprites.",
	},
	{
		title: "Comet",
		cover: "comet.png",
		yearPublished: 2023,
		minPlayers: 2,
		maxPlayers: 4,
		minPlayTime: 45,
		maxPlayTime: 75,
		minAge: 12,
		rating: 7.2,
		weight: 2.1,
		buyingPrice: null,
		description: "Save extinct and endangered species from their deadly fate by the threatening comet!",
	},
	{
		title: "Jurassic Snack",
		cover: "jurassic-snack.jpg",
		yearPublished: 2018,
		minPlayers: 2,
		maxPlayers: 2,
		minPlayTime: 15,
		maxPlayTime: 15,
		minAge: 7,
		rating: 6.5,
		weight: 1.1,
		buyingPrice: null,
		description: "Lead your dinosaurs to snack vegetables before everything, including you, is eaten.",
	},
	{
		title: "First Giants",
		cover: "first-giants.png",
		yearPublished: 2026,
		minPlayers: 2,
		maxPlayers: 5,
		minPlayTime: 30,
		maxPlayTime: 30,
		minAge: 10,
		rating: 6.8,
		weight: 2.1,
		buyingPrice: null,
		description: "Dinosaurs, flying reptiles & giant mammals — create the most prestigious exhibition!",
	},
	{
		title: "Cosmoctopus",
		cover: "cosmoctopus.jpg",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 60,
		maxPlayTime: 90,
		minAge: 10,
		rating: 7.2,
		weight: 2.2,
		buyingPrice: null,
		description: "Earn tentacles to prove your devotion to the cutest cephalopod in the cosmos.",
	},
	{
		title: "Bot Factory",
		cover: "bot-factory.jpg",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 30,
		maxPlayTime: 60,
		minAge: 14,
		rating: 7.3,
		weight: 2.8,
		buyingPrice: 52,
		description: "In a Kanban way, gather projects and parts to build bots to accomplish contracts.",
	},
	{
		title: "Kabuto Sumo",
		cover: "kabuto-sumo.jpg",
		yearPublished: 2021,
		minPlayers: 2,
		maxPlayers: 4,
		minPlayTime: 15,
		maxPlayTime: 15,
		minAge: null,
		rating: 6.7,
		weight: 1.2,
		buyingPrice: 88,
		description: "Sumo wrestling insects compete to push their opponent out of the ring.",
	},
	{
		title: "Stack O' Cats",
		cover: "stack-o-cats.png",
		yearPublished: 2025,
		minPlayers: 1,
		maxPlayers: 6,
		minPlayTime: 7,
		maxPlayTime: 42,
		minAge: 3,
		rating: 7.1,
		weight: 1.2,
		buyingPrice: 31,
		description: "Be the best at keeping the cats under control, and you’ll come out the victor!",
	},
	{
		title: "Isle of Trains: All Aboard",
		cover: "isle-of-trains-all-aboard.jpg",
		yearPublished: 2023,
		minPlayers: 1,
		maxPlayers: 4,
		minPlayTime: 60,
		maxPlayTime: 60,
		minAge: 10,
		rating: 6.9,
		weight: 2.3,
		buyingPrice: 35,
		description: "Build your engine, make deliveries, and become the greatest conductor on the island!",
	},
];

const titles = collection.map((game) => game.title);

const PLAYED_GAMES = 14;
const COOP_GAME = "Mythwind";
const MIXED_COOP_GAME = "City of the Great Machine";
const UNSCORED_GAME = "Kittens in Space";
const OWNED_GAMES = 20;
const WANTED_GAMES = 4;
const PREVIOUSLY_OWNED_GAMES = 1;
const SOLD_GAMES = 1;
const SOLD_PRICE = 60;
const THIS_MONTH_SESSIONS = 3;

const stateOf = (index: number): GameState => {
	if (index < OWNED_GAMES) return "owned";
	if (index < OWNED_GAMES + WANTED_GAMES) return "wanted";
	if (index < OWNED_GAMES + WANTED_GAMES + PREVIOUSLY_OWNED_GAMES) return "previouslyOwned";
	if (index < OWNED_GAMES + WANTED_GAMES + PREVIOUSLY_OWNED_GAMES + SOLD_GAMES) return "sold";
	return "forTrade";
};

const languageOf = (index: number) => {
	if (index % 5 === 4) return null;
	return index % 4 === 3 ? "nl" : "en";
};

export const games = collection.map((game, index) => {
	const state = stateOf(index);
	return {
		...game,
		state,
		language: languageOf(index),
		soldPrice: state === "sold" ? SOLD_PRICE : null,
		hasScoring: game.title !== UNSCORED_GAME,
		hasCoopMode: game.title === COOP_GAME || game.title === MIXED_COOP_GAME,
		coopByDefault: game.title === COOP_GAME,
		additionDate: subDays(now, 600 - index * 15).toISOString(),
	};
});

export const avatars = [
	{ name: "Kermit", avatar: "kermit.jpg" },
	{ name: "Miss Piggy", avatar: "miss-piggy.jpg" },
	{ name: "Fozzie", avatar: "fozzie.jpg" },
	{ name: "Gonzo", avatar: "gonzo.png" },
	{ name: "Rowlf", avatar: "rowlf.png" },
	{ name: "Beaker", avatar: "beaker.png" },
	{ name: "Scooter", avatar: "scooter.png" },
	{ name: "Swedish Chef", avatar: "swedish-chef.png" },
];

export const players = avatars.map((player) => player.name);

export const linkedPlayer = players[0];

export const locations = ["Clubhouse", "Cafe", "Library"];

export const expansions = [
	{ game: titles[0], title: "Sheep in Disguise: Goblin Expansion" },
	{ game: titles[0], title: "Sheep in Disguise: Wizard Expansion" },
	{ game: titles[1], title: "Dwar7s: The Lost Tribes Expansion" },
];

const playedGameIndexes = Array.from({ length: PLAYED_GAMES }, (_, round) =>
	Array.from({ length: PLAYED_GAMES - round }, (_, index) => index),
).flat();

const startOf = (sessionIndex: number) =>
	sessionIndex < THIS_MONTH_SESSIONS
		? subSeconds(now, 1 + sessionIndex)
		: subDays(now, 37 + (sessionIndex - THIS_MONTH_SESSIONS) * 5);

interface SeedSession {
	game: string;
	start: Date;
	minutes: number;
	location: string;
	scores: { player: string; score: number | null }[];
	winner: string | null;
	isCoop: boolean;
	teamWon: boolean | null;
}

const competitiveSessions = playedGameIndexes.map((gameIndex, sessionIndex): SeedSession => {
	const seats = sessionIndex % 3 === 0 ? 4 : 3;
	const scores = Array.from({ length: seats }, (_, seat) => {
		const playerIndex = (sessionIndex + seat) % players.length;
		const edge = (players.length - playerIndex) * 4;
		return { player: players[playerIndex], score: 40 + edge + (((sessionIndex + 1) * (playerIndex + 3) * 7) % 40) };
	});
	const winner = scores.reduce((best, score) => (score.score > best.score ? score : best), scores[0]);
	return {
		game: titles[gameIndex],
		start: startOf(sessionIndex),
		minutes: 30 + (sessionIndex % 5) * 15,
		location: locations[sessionIndex % locations.length],
		scores,
		winner: winner.player,
		isCoop: false,
		teamWon: null,
	};
});

const seatsWithoutScores = players.slice(0, 3).map((player) => ({ player, score: null }));

const coopSession = (game: string, daysAgo: number, teamWon: boolean): SeedSession => ({
	game,
	start: subDays(now, daysAgo),
	minutes: 60,
	location: locations[0],
	scores: seatsWithoutScores,
	winner: null,
	isCoop: true,
	teamWon,
});

const coopSessions = [
	coopSession(COOP_GAME, 36, true),
	coopSession(COOP_GAME, 60, true),
	coopSession(COOP_GAME, 90, false),
	coopSession(COOP_GAME, 120, true),
	coopSession(COOP_GAME, 150, false),
	coopSession(MIXED_COOP_GAME, 200, true),
	coopSession(MIXED_COOP_GAME, 230, false),
	coopSession(MIXED_COOP_GAME, 260, true),
];

const unscoredSessions = [0, 1, 2].map(
	(index): SeedSession => ({
		game: UNSCORED_GAME,
		start: subDays(now, 720 + index * 3),
		minutes: 15,
		location: locations[1],
		scores: seatsWithoutScores,
		winner: players[index],
		isCoop: false,
		teamWon: null,
	}),
);

export const sessions = [...competitiveSessions, ...coopSessions, ...unscoredSessions];

export const gameNights = [
	{
		title: "Strategy night",
		startDate: subDays(now, -3),
		location: locations[0],
		suggestedGames: [titles[14], titles[15], titles[16]],
		rsvps: {
			[players[0]]: "accepted",
			[players[1]]: "accepted",
			[players[2]]: "maybe",
			[players[3]]: "declined",
			[players[4]]: "pending",
		},
	},
	{
		title: "Family afternoon",
		startDate: subDays(now, -10),
		location: locations[1],
		suggestedGames: [titles[10]],
		rsvps: { [players[0]]: "accepted", [players[5]]: "pending" },
	},
];

const playCounts = (since: Date) => {
	const counts = new Map<string, number>();
	for (const session of sessions) {
		if (session.start >= since) counts.set(session.game, (counts.get(session.game) ?? 0) + 1);
	}
	return [...counts.entries()].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]));
};

export const standingsOf = (subset: typeof sessions) =>
	players
		.map((name) => {
			const played = subset.filter((session) => session.scores.some((score) => score.player === name));
			const wins = played.filter((session) => session.winner === name || session.teamWon === true).length;
			return { name, sessions: played.length, wins, winRate: wins / played.length };
		})
		.filter((standing) => standing.sessions > 0)
		.sort((a, b) => b.wins - a.wins || b.winRate - a.winRate || b.sessions - a.sessions);

export const mostPlayedGame = games[0];
export const secondMostPlayedGame = games[1];
export const neverPlayedGame = games[PLAYED_GAMES];
export const wantedGame = games[OWNED_GAMES];
export const previouslyOwnedGame = games[OWNED_GAMES + WANTED_GAMES];
export const soldGame = games[OWNED_GAMES + WANTED_GAMES + PREVIOUSLY_OWNED_GAMES];
export const forTradeGame = games[games.length - 1];
export const coopGame = games.find((game) => game.title === COOP_GAME) ?? games[0];
export const unscoredGame = games.find((game) => game.title === UNSCORED_GAME) ?? games[0];

const shameGames = games.filter(
	(game) =>
		game.state === "owned" &&
		!sessions.some((session) => session.game === game.title && session.start >= subMonths(now, 6)),
);

const ownedPrices = games.filter((game) => game.state === "owned").map((game) => game.buyingPrice ?? 0);
const sum = (values: number[]) => values.reduce((total, value) => total + value, 0);
const thisMonth = sessions.slice(0, THIS_MONTH_SESSIONS);
const playedThisMonth = thisMonth.filter((session) => session.scores.some((score) => score.player === linkedPlayer));

export const expected = {
	gameCount: games.length,
	expansionCount: expansions.length,
	playerCount: players.length,
	sessionCount: sessions.length,
	sessionsThisMonth: thisMonth.length,
	playersThisMonth: new Set(thisMonth.flatMap((session) => session.scores.map((score) => score.player))).size,
	linkedPlayedThisMonth: playedThisMonth.length,
	linkedWonThisMonth: playedThisMonth.filter((session) => session.winner === linkedPlayer).length,
	lastSessions: [...sessions].sort((a, b) => b.start.getTime() - a.start.getTime()).slice(0, 5),
	topPlayers: standingsOf(sessions).slice(0, 3),
	mostPlayedAllTime: playCounts(new Date(0)).slice(0, 4),
	mostPlayedThisMonth: playCounts(new Date(now.getFullYear(), now.getMonth(), 1)).slice(0, 4),
	mostPlayedThisYear: playCounts(new Date(now.getFullYear(), 0, 1)).slice(0, 4),
	ownedCount: OWNED_GAMES,
	wantedCount: WANTED_GAMES,
	previouslyOwnedCount: PREVIOUSLY_OWNED_GAMES,
	soldCount: SOLD_GAMES,
	forTradeCount: games.length - OWNED_GAMES - WANTED_GAMES - PREVIOUSLY_OWNED_GAMES - SOLD_GAMES,
	ownedValue: sum(ownedPrices),
	shameCount: shameGames.length,
	shameNeverPlayedCount: shameGames.filter((game) => !sessions.some((session) => session.game === game.title)).length,
	shameValue: sum(shameGames.map((game) => game.buyingPrice ?? 0)),
	recentlyAdded: titles.slice(-3).reverse(),
};
