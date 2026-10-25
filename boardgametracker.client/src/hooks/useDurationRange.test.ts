import { renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("react-i18next", () => ({
	useTranslation: () => ({
		i18n: { language: "en-US" },
		t: (key: string, options: { minutes?: string; hours?: string }) => {
			if (key === "short-duration.minutes") return `${options.minutes} min`;
			if (key === "short-duration.hours") return `${options.hours}h`;
			return `${options.hours}h ${options.minutes}m`;
		},
	}),
}));

import { useDurationRange } from "./useDurationRange";

const range = () => renderHook(() => useDurationRange()).result.current;

describe("useDurationRange", () => {
	it("writes a range of whole hours once with the unit", () => {
		expect(range()(240, 480)).toBe("4–8h");
	});

	it("writes a range under an hour in minutes", () => {
		expect(range()(30, 45)).toBe("30–45 min");
	});

	it("keeps a range that starts below an hour in minutes", () => {
		expect(range()(45, 60)).toBe("45–60 min");
		expect(range()(30, 90)).toBe("30–90 min");
	});

	it("writes mixed ranges above an hour as two durations", () => {
		expect(range()(90, 120)).toBe("1h 30m–2h");
	});

	it("writes a single duration when both ends match or one is missing", () => {
		expect(range()(60, 60)).toBe("1h");
		expect(range()(null, 75)).toBe("1h 15m");
		expect(range()(30, undefined)).toBe("30 min");
	});

	it("gives nothing without a play time", () => {
		expect(range()(null, null)).toBeNull();
	});
});
