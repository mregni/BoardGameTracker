import type { ChangeDetectionStatus } from "@/models/Games/GamePrice";

export interface ChangeDetectionWatchInfo {
	url: string;
	title: string | null;
	lastCheckedAt: string | null;
}

export interface ChangeDetectionConnectionTest {
	ok: boolean;
	status: ChangeDetectionStatus;
	version: string | null;
}
