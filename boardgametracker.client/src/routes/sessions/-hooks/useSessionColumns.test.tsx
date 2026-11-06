import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { DataTableProps } from "@/components/BgtTable/BgtDataTable";
import type { Session, Settings } from "@/models";
import { useSessionColumns } from "./useSessionColumns";

const mocks = vi.hoisted(() => ({ canWrite: false }));

vi.mock("@tanstack/react-router", () => ({ useNavigate: () => vi.fn() }));
vi.mock("react-i18next", () => ({ useTranslation: () => ({ t: (key: string) => key }) }));
vi.mock("@/hooks/useNumberFormat", () => ({ useNumberFormat: () => ({ decimal: String }) }));
vi.mock("@/hooks/useShortDuration", () => ({ useShortDuration: () => String }));
vi.mock("@/hooks/usePermissions", () => ({ usePermissions: () => ({ canWrite: mocks.canWrite }) }));

const settings = { dateFormat: "dd-MM-yyyy", timeFormat: "HH:mm", uiLanguage: "en-US" } as Settings;
const onDelete = vi.fn();
const subjectColumn: DataTableProps<Session>["columns"][number] = { id: "game", header: "game" };

const idsOf = (columns: DataTableProps<Session>["columns"]) =>
	columns.map((column) => column.id ?? (column as { accessorKey?: string }).accessorKey);

const columnsFor = (input: Partial<Parameters<typeof useSessionColumns>[0]> = {}) =>
	renderHook(() => useSessionColumns({ settings, players: [], onDelete, ...input })).result.current;

const sortValue = (columns: DataTableProps<Session>["columns"], id: string, session: Partial<Session>) => {
	const column = columns.find((x) => x.id === id) as { accessorFn: (session: Session, index: number) => unknown };
	return column.accessorFn(session as Session, 0);
};

describe("useSessionColumns", () => {
	beforeEach(() => {
		mocks.canWrite = false;
	});

	it("stays empty until the settings are known", () => {
		expect(columnsFor({ settings: undefined })).toEqual([]);
	});

	it("leaves the actions out for a reader", () => {
		expect(idsOf(columnsFor())).toEqual(["date", "time", "minutes", "players", "comment"]);
	});

	it("adds the actions for a writer and puts the subject after the time", () => {
		mocks.canWrite = true;

		expect(idsOf(columnsFor({ subjectColumn }))).toEqual([
			"date",
			"time",
			"game",
			"minutes",
			"players",
			"comment",
			"actions",
		]);
	});

	it("sorts on the moment and on the time of day", () => {
		const start = new Date(2026, 9, 5, 20, 15);
		const columns = columnsFor();

		expect(sortValue(columns, "date", { start })).toBe(start.getTime());
		expect(sortValue(columns, "time", { start })).toBe(20 * 60 + 15);
	});
});
