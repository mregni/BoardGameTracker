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
			i18n: { t: (key: string) => `${language.current}:${key}` },
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
