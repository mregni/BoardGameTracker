export type ChangeDetectionStatus =
	| "ok"
	| "notConfigured"
	| "pending"
	| "misconfigured"
	| "unauthorized"
	| "watchNotFound"
	| "unreachable"
	| "parseError";

export interface GamePrice {
	gameId: number;
	watchId: string | null;
	available: boolean;
	status: ChangeDetectionStatus;
	inStock: boolean | null;
	price: number | null;
	currency: string | null;
	checkedAt: string | null;
	shopUrl: string | null;
	recheckQueued: boolean;
	fetchedAt: string | null;
}

const ERROR_STATUSES: ReadonlySet<ChangeDetectionStatus> = new Set([
	"misconfigured",
	"unauthorized",
	"watchNotFound",
	"unreachable",
	"parseError",
]);

export const isPriceError = (status: ChangeDetectionStatus | undefined): boolean =>
	status !== undefined && ERROR_STATUSES.has(status);

export const priceErrorKey = (status: ChangeDetectionStatus): string => `game:price.unavailable.${status}`;
