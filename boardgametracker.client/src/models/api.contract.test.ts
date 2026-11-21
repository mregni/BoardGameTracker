import { describe, expect, it } from "vitest";
import type {
	Badge,
	CompareGame,
	ComparePair,
	CompareResult,
	CompareSession,
	CompareStreak,
	CompareTeamRecord,
	CreateGame,
	CreateLocation,
	CreatePlayer,
	CreateSession,
	DashboardStatistics,
	Expansion,
	FinishRunningGame,
	Game,
	GameNight,
	GameNightDetail,
	GameNightRecord,
	GameNightRecords,
	GameNightReference,
	GameNightRsvpUpdate,
	GameNightStatistics,
	GameNightStatisticsPeriod,
	GameNightStreaks,
	GameNightSummary,
	GameStreak,
	Leaderboard,
	LeaderboardEntry,
	Location,
	LocationStatistics,
	NightAttendance,
	NightGameStatistic,
	NightHistory,
	NightWinnerStatistic,
	PlannedGameStatistic,
	Player,
	PlayerOverview,
	PlayerSession,
	PlayerStreak,
	RunningGame,
	Session,
	SessionSuggestions,
	Settings,
	Shame,
	ShameGameNight,
	ShameStatistics,
	StartRunningGame,
	StorageLocation,
	UpdateAttendance,
	UpdateExpansion,
} from "@/models";
import type {
	LoginRequest,
	LoginResponse,
	OidcProvider,
	PlayerLinkCandidate,
	PlayerLinkMatchReason,
	PlayerLinkSuggestion,
	PlayerLinkSuggestions,
	ProfileResponse,
	User,
} from "@/models/Auth/Auth";
import type { ExternalLogin, OidcDiscoveryResult, OidcProviderConfig, OidcSetup } from "@/models/Auth/Oidc";
import type { Loan } from "@/models/Loan/Loan";
import type { IntegrationStatus } from "@/models/Settings/IntegrationStatus";
import type { VersionInfo } from "@/models/Settings/VersionInfo";
import type {
	ArrangeStorage,
	AssignStorage,
	BoxOrientation,
	CreateStorageLocation,
	MoveStorageLocation,
	StorageItemReference,
	StorageItemType,
	StorageLayout,
	UpdateBoxOrientation,
	UpdateStorageLocation,
} from "@/models/Storage/StorageLocation";
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

const sameKeys = <Model, Schema>(aligned: KeyDrift<Model, Schema>) => expect(aligned).toBe(true);
const coversApiKeys = <Model, Schema>(
	aligned: [Exclude<keyof Schema, keyof Model>] extends [never]
		? true
		: { onlyInApi: Exclude<keyof Schema, keyof Model> },
) => expect(aligned).toBe(true);
const sameValues = <A, B>(aligned: SameUnion<A, B>) => expect(aligned).toBe(true);

describe("API contract", () => {
	it("keeps the response models aligned with the generated schemas", () => {
		sameKeys<Game, Schemas["GameDto"]>(true);
		sameKeys<Player, Schemas["PlayerDto"]>(true);
		sameKeys<Session, Schemas["SessionDto"]>(true);
		sameKeys<PlayerSession, Schemas["PlayerSessionDto"]>(true);
		sameKeys<SessionSuggestions, Schemas["SessionSuggestionsDto"]>(true);
		sameKeys<Expansion, Schemas["ExpansionDto"]>(true);
		sameKeys<Location, Schemas["LocationDto"]>(true);
		sameKeys<LocationStatistics, Schemas["LocationStatisticsDto"]>(true);
		sameKeys<StorageLocation, Schemas["StorageLocationDto"]>(true);
		sameKeys<PlayerOverview, Schemas["PlayerOverviewDto"]>(true);
		sameKeys<Loan, Schemas["LoanDto"]>(true);
		sameKeys<GameNight, Schemas["GameNightDto"]>(true);
		sameKeys<GameNightSummary, Schemas["GameNightSummaryDto"]>(true);
		sameKeys<GameNightDetail, Schemas["GameNightDetailDto"]>(true);
		sameKeys<GameNightStatistics, Schemas["GameNightStatisticsDto"]>(true);
		sameKeys<NightWinnerStatistic, Schemas["NightWinnerStatisticDto"]>(true);
		sameKeys<NightHistory, Schemas["NightHistoryDto"]>(true);
		sameKeys<NightGameStatistic, Schemas["NightGameStatisticDto"]>(true);
		sameKeys<PlannedGameStatistic, Schemas["PlannedGameStatisticDto"]>(true);
		sameKeys<GameNightStreaks, Schemas["GameNightStreaksDto"]>(true);
		sameKeys<PlayerStreak, Schemas["PlayerStreakDto"]>(true);
		sameKeys<GameStreak, Schemas["GameStreakDto"]>(true);
		sameKeys<GameNightReference, Schemas["GameNightReferenceDto"]>(true);
		sameKeys<NightAttendance, Schemas["NightAttendanceDto"]>(true);
		sameKeys<GameNightRecords, Schemas["GameNightRecordsDto"]>(true);
		sameKeys<GameNightRecord, Schemas["GameNightRecordDto"]>(true);
		sameKeys<RunningGame, Schemas["RunningGameDto"]>(true);
		sameKeys<GameNightRsvpUpdate, Schemas["GameNightRsvpUpdateDto"]>(true);
		sameKeys<Badge, Schemas["BadgeDto"]>(true);
		sameKeys<DashboardStatistics, Schemas["DashboardStatisticsDto"]>(true);
		sameKeys<DashboardStatistics["nextGameNight"] & object, Schemas["DashboardGameNightDto"]>(true);
		sameKeys<DashboardStatistics["linkedPlayer"] & object, Schemas["DashboardLinkedPlayerDto"]>(true);
		sameKeys<DashboardStatistics["shelfOfShame"] & object, Schemas["DashboardShelfOfShameDto"]>(true);
		sameKeys<DashboardStatistics["recentActivities"][number], Schemas["RecentActivityDto"]>(true);
		sameKeys<Leaderboard, Schemas["LeaderboardDto"]>(true);
		sameKeys<LeaderboardEntry, Schemas["LeaderboardEntryDto"]>(true);
		sameKeys<Settings, Schemas["UIResourceDto"]>(true);
		sameKeys<Shame, Schemas["ShameDto"]>(true);
		sameKeys<ShameStatistics, Schemas["ShameStatisticsDto"]>(true);
		sameKeys<ShameGameNight, Schemas["ShameGameNightDto"]>(true);
		sameKeys<VersionInfo, Schemas["UpdateStatusDto"]>(true);
		sameKeys<IntegrationStatus, Schemas["IntegrationStatusDto"]>(true);
		sameKeys<User, Schemas["UserInfo"]>(true);
		sameKeys<LoginResponse, Schemas["LoginResponse"]>(true);
		sameKeys<ProfileResponse, Schemas["ProfileResponse"]>(true);
		sameKeys<PlayerLinkSuggestions, Schemas["PlayerLinkSuggestionsResponse"]>(true);
		sameKeys<PlayerLinkSuggestion, Schemas["PlayerLinkSuggestionDto"]>(true);
		sameKeys<PlayerLinkCandidate, Schemas["PlayerLinkCandidateDto"]>(true);
		sameValues<Schemas["PlayerLinkMatchReason"], PlayerLinkMatchReason>(true);
		sameKeys<OidcProvider, Schemas["OidcProviderInfo"]>(true);
		sameKeys<OidcProviderConfig, Schemas["OidcProviderDto"]>(true);
		sameKeys<OidcSetup, Schemas["OidcSetupDto"]>(true);
		sameKeys<OidcDiscoveryResult, Schemas["OidcDiscoveryResultDto"]>(true);
		sameKeys<ExternalLogin, Schemas["ExternalLoginDto"]>(true);
		sameKeys<CompareResult, Schemas["CompareResultDto"]>(true);
		sameKeys<CompareGame, Schemas["CompareGame"]>(true);
		sameKeys<CompareSession, Schemas["CompareSession"]>(true);
		sameKeys<CompareStreak, Schemas["CompareStreak"]>(true);
		sameKeys<ComparePair, Schemas["ComparePairDto"]>(true);
		sameKeys<CompareTeamRecord, Schemas["CompareTeamRecord"]>(true);
	});

	it("keeps the commands aligned with the generated request bodies", () => {
		sameKeys<CreateGame, Omit<Schemas["UpdateGameCommand"], "id" | "shopUrl">>(true);
		coversApiKeys<Player, Schemas["CreatePlayerCommand"]>(true);
		coversApiKeys<CreatePlayer, Omit<Schemas["CreatePlayerCommand"], "image">>(true);
		sameKeys<CreateLocation, Schemas["CreateLocationCommand"]>(true);
		sameKeys<UpdateExpansion, Schemas["UpdateExpansionCommand"]>(true);
		sameKeys<CreateStorageLocation, Schemas["CreateStorageLocationCommand"]>(true);
		sameKeys<UpdateStorageLocation, Schemas["UpdateStorageLocationCommand"]>(true);
		sameKeys<MoveStorageLocation, Schemas["MoveStorageLocationCommand"]>(true);
		sameKeys<StorageItemReference, Schemas["StorageItemReference"]>(true);
		sameKeys<AssignStorage, Schemas["AssignStorageCommand"]>(true);
		sameKeys<ArrangeStorage, Schemas["ArrangeStorageCommand"]>(true);
		sameKeys<UpdateBoxOrientation, Schemas["UpdateBoxOrientationCommand"]>(true);
		sameKeys<CreateSession, Schemas["CreateSessionCommand"]>(true);
		sameKeys<StartRunningGame, Schemas["StartRunningGameCommand"]>(true);
		sameKeys<UpdateAttendance, Schemas["UpdateAttendanceCommand"]>(true);
		sameKeys<FinishRunningGame, Schemas["FinishRunningGameCommand"]>(true);
		sameKeys<LoginRequest, Schemas["LoginRequest"]>(true);
	});

	it("keeps the string enums aligned", () => {
		sameValues<Schemas["GameState"], `${Game["state"]}`>(true);
		sameValues<Schemas["BadgeType"], `${Badge["type"]}`>(true);
		sameValues<Schemas["StorageLayout"], `${StorageLayout}`>(true);
		sameValues<Schemas["BoxOrientation"], `${BoxOrientation}`>(true);
		sameValues<Schemas["StorageItemType"], `${StorageItemType}`>(true);
		sameValues<Schemas["CompareOutcome"], CompareGame["lastOutcome"]>(true);
		sameValues<Schemas["GameNightStatisticsPeriod"], `${GameNightStatisticsPeriod}`>(true);
		sameValues<Schemas["IntegrationKind"], IntegrationStatus["integration"]>(true);
		sameValues<Schemas["IntegrationConnectionStatus"], IntegrationStatus["status"]>(true);
	});
});
