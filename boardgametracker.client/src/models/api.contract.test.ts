import { describe, it } from "vitest";
import type {
	Badge,
	CreateGame,
	CreateLocation,
	CreatePlayer,
	CreateSession,
	Expansion,
	Game,
	GameNight,
	Leaderboard,
	LeaderboardEntry,
	Location,
	Player,
	PlayerSession,
	Session,
	Settings,
} from "@/models";
import type { LoginRequest, LoginResponse, OidcProvider, ProfileResponse, User } from "@/models/Auth/Auth";
import type { ExternalLogin, OidcDiscoveryResult, OidcProviderConfig, OidcSetup } from "@/models/Auth/Oidc";
import type { Loan } from "@/models/Loan/Loan";
import type { VersionInfo } from "@/models/Settings/VersionInfo";
import type { components } from "./api.generated";

type Schemas = components["schemas"];

type KeyDrift<Model, Schema> = [Exclude<keyof Schema, keyof Model>] extends [never]
	? [Exclude<keyof Model, keyof Schema>] extends [never]
		? true
		: { onlyInFrontend: Exclude<keyof Model, keyof Schema> }
	: { onlyInApi: Exclude<keyof Schema, keyof Model> };

type SameUnion<A, B> = [A] extends [B]
	? [B] extends [A]
		? true
		: { onlyInApi: Exclude<A, B> }
	: { onlyInFrontend: Exclude<B, A> };

const sameKeys = <Model, Schema>(_aligned: KeyDrift<Model, Schema>) => undefined;
const coversApiKeys = <Model, Schema>(
	_aligned: [Exclude<keyof Schema, keyof Model>] extends [never]
		? true
		: { onlyInApi: Exclude<keyof Schema, keyof Model> },
) => undefined;
const sameValues = <A, B>(_aligned: SameUnion<A, B>) => undefined;

describe("API contract", () => {
	it("keeps the response models aligned with the generated schemas", () => {
		sameKeys<Game, Schemas["GameDto"]>(true);
		sameKeys<Player, Schemas["PlayerDto"]>(true);
		sameKeys<Session, Schemas["SessionDto"]>(true);
		sameKeys<PlayerSession, Schemas["PlayerSessionDto"]>(true);
		sameKeys<Expansion, Schemas["ExpansionDto"]>(true);
		sameKeys<Location, Schemas["LocationDto"]>(true);
		sameKeys<Loan, Schemas["LoanDto"]>(true);
		sameKeys<GameNight, Schemas["GameNightDto"]>(true);
		sameKeys<Badge, Schemas["BadgeDto"]>(true);
		sameKeys<Leaderboard, Schemas["LeaderboardDto"]>(true);
		sameKeys<LeaderboardEntry, Schemas["LeaderboardEntryDto"]>(true);
		sameKeys<Settings, Schemas["UIResourceDto"]>(true);
		sameKeys<VersionInfo, Schemas["UpdateStatusDto"]>(true);
		sameKeys<User, Schemas["UserInfo"]>(true);
		sameKeys<LoginResponse, Schemas["LoginResponse"]>(true);
		sameKeys<ProfileResponse, Schemas["ProfileResponse"]>(true);
		sameKeys<OidcProvider, Schemas["OidcProviderInfo"]>(true);
		sameKeys<OidcProviderConfig, Schemas["OidcProviderDto"]>(true);
		sameKeys<OidcSetup, Schemas["OidcSetupDto"]>(true);
		sameKeys<OidcDiscoveryResult, Schemas["OidcDiscoveryResultDto"]>(true);
		sameKeys<ExternalLogin, Schemas["ExternalLoginDto"]>(true);
	});

	it("keeps the commands aligned with the generated request bodies", () => {
		sameKeys<CreateGame, Omit<Schemas["UpdateGameCommand"], "id" | "shopUrl">>(true);
		coversApiKeys<Player, Schemas["CreatePlayerCommand"]>(true);
		coversApiKeys<CreatePlayer, Omit<Schemas["CreatePlayerCommand"], "image">>(true);
		sameKeys<CreateLocation, Schemas["CreateLocationCommand"]>(true);
		sameKeys<CreateSession, Schemas["CreateSessionCommand"]>(true);
		sameKeys<LoginRequest, Schemas["LoginRequest"]>(true);
	});

	it("keeps the string enums aligned", () => {
		sameValues<Schemas["GameState"], `${Game["state"]}`>(true);
		sameValues<Schemas["BadgeType"], `${Badge["type"]}`>(true);
	});
});
