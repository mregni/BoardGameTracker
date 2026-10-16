import { renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const { language, toastSuccess, toastError, toastInfo, toastWarning } = vi.hoisted(() => ({
	language: { current: "en" },
	toastSuccess: vi.fn(),
	toastError: vi.fn(),
	toastInfo: vi.fn(),
	toastWarning: vi.fn(),
}));

vi.mock("sonner", () => ({
	toast: { success: toastSuccess, error: toastError, info: toastInfo, warning: toastWarning },
}));

vi.mock("react-i18next", () => ({
	useTranslation: () => {
		const renderLanguage = language.current;
		return {
			t: (key: string) => `${renderLanguage}:${key}`,
			i18n: {
				t: (key: string, values?: Record<string, unknown>) =>
					`${language.current}:${key}${values ? `:${JSON.stringify(values)}` : ""}`,
			},
		};
	},
}));

import { useToasts } from "./useToasts";

describe("useToasts", () => {
	it("translates in the language that is active when the toast is shown", () => {
		language.current = "en";
		const { result } = renderHook(() => useToasts());

		language.current = "nl";
		result.current.successToast("settings:save.successfull");
		result.current.errorToast("settings:save.failed");

		expect(toastSuccess).toHaveBeenCalledWith("nl:settings:save.successfull");
		expect(toastError).toHaveBeenCalledWith("nl:settings:save.failed");
	});

	it("adds a translated action to an error toast when one is given", () => {
		language.current = "en";
		const onClick = vi.fn();
		const { result } = renderHook(() => useToasts());

		result.current.errorToast("games:import.failed-unauthorized", {
			label: "common:open-integration-settings",
			onClick,
		});

		expect(toastError).toHaveBeenCalledWith("en:games:import.failed-unauthorized", {
			action: { label: "en:common:open-integration-settings", onClick },
		});
	});

	it("adds values and an undo action to a success toast when given", () => {
		language.current = "en";
		const onClick = vi.fn();
		const { result } = renderHook(() => useToasts());

		result.current.successToast("shames:plan.added", { label: "common:undo", onClick }, { game: "Root" });

		expect(toastSuccess).toHaveBeenCalledWith('en:shames:plan.added:{"game":"Root"}', {
			duration: 8000,
			action: { label: "en:common:undo", onClick },
		});
	});

	it("translates info and warning toasts at call time as well", () => {
		language.current = "en";
		const { result } = renderHook(() => useToasts());

		language.current = "es";
		result.current.infoToast("common:info");
		result.current.warningToast("common:warning");

		expect(toastInfo).toHaveBeenCalledWith("es:common:info");
		expect(toastWarning).toHaveBeenCalledWith("es:common:warning");
	});
});
