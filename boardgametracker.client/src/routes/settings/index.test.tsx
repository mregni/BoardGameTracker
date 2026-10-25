import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Settings } from "@/models";
import { renderWithProviders, screen, userEvent, within } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	captured: {} as { config: { component: FC } },
	search: {} as { tab?: string },
	saveSettings: vi.fn(),
	refreshIntegrationStatus: vi.fn(),
	ssoEnabled: false,
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.config = config;
		return { ...config, useSearch: () => mocks.search };
	},
	useNavigate: () => vi.fn(),
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ canManageSettings: true, canWrite: true, isAdmin: true }),
}));

const settings: Settings = {
	dateFormat: "dd-MM-yyyy",
	timeFormat: "HH:mm",
	uiLanguage: "en-US",
	currency: "EUR",
	statistics: false,
	updateCheckEnabled: false,
	versionTrack: "stable",
	shelfOfShameEnabled: false,
	shelfOfShameMonthsLimit: 6,
	publicUrl: "",
	gameNightsEnabled: false,
	rsvpAuthenticationEnabled: false,
	emailEnabled: false,
	ragEnabled: false,
	bggStatus: { isConfigured: true, isReadOnly: false, source: "db" },
	bggApiKey: "",
	changeDetectionStatus: { isConfigured: false, isReadOnly: false, source: "none" },
	changeDetectionBaseUrl: "",
	changeDetectionApiKey: "",
	environmentOverrides: {},
};

vi.mock("./-hooks/useSettingsData", () => ({
	useSettingsData: () => ({ settings, saveSettings: mocks.saveSettings, isSaving: false, languages: [] }),
}));

vi.mock("./-hooks/useIntegrationStatus", () => ({
	useRefreshIntegrationStatus: () => mocks.refreshIntegrationStatus,
}));

vi.mock("./-components/BggSettings", () => ({ BggSettings: () => <div>bgg-tab</div> }));
vi.mock("./-hooks/useSingleSignOnEnabled", () => ({ useSingleSignOnEnabled: () => mocks.ssoEnabled }));
vi.mock("./-components/GeneralSettings", () => ({
	GeneralSettings: ({ form }: { form: { setFieldValue: (name: string, value: string) => void } }) => (
		<button type="button" onClick={() => form.setFieldValue("publicUrl", "http://192.168.1.20:8080")}>
			use-lan-address
		</button>
	),
}));
vi.mock("./-components/SettingsSidebar", () => ({ SettingsSidebar: () => null }));
vi.mock("./-components/AccountSettings", () => ({ AccountSettings: () => null }));
vi.mock("./-components/AdvancedSettings", () => ({ AdvancedSettings: () => null }));
vi.mock("./-components/DangerZoneSection", () => ({ DangerZoneSection: () => null }));
vi.mock("./-components/GameNightsSettings", () => ({ GameNightsSettings: () => null }));
vi.mock("./-components/ShelfOfShameSettings", () => ({ ShelfOfShameSettings: () => null }));
vi.mock("./-components/SsoSettings", () => ({ SsoSettings: () => null }));

await import("./index");

const renderPage = () => {
	const Component = mocks.captured.config.component;
	renderWithProviders(<Component />);
};

describe("Settings page save flow", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.ssoEnabled = false;
		mocks.saveSettings.mockResolvedValue(settings);
	});

	it("saves first and then re-tests the integrations on the integrations tab", async () => {
		const user = userEvent.setup();
		mocks.search = { tab: "integrations" };
		renderPage();

		await user.click(screen.getByRole("button", { name: "save.button" }));

		await vi.waitFor(() => expect(mocks.refreshIntegrationStatus).toHaveBeenCalledTimes(1));
		expect(mocks.saveSettings).toHaveBeenCalledTimes(1);
		expect(mocks.saveSettings.mock.invocationCallOrder[0]).toBeLessThan(
			mocks.refreshIntegrationStatus.mock.invocationCallOrder[0],
		);
	});

	it("does not re-test the integrations when saving another tab", async () => {
		const user = userEvent.setup();
		mocks.search = { tab: "general" };
		renderPage();

		await user.click(screen.getByRole("button", { name: "save.button" }));

		await vi.waitFor(() => expect(mocks.saveSettings).toHaveBeenCalledTimes(1));
		expect(mocks.refreshIntegrationStatus).not.toHaveBeenCalled();
	});

	it("asks before saving a public URL that stops single sign-on, and saves once confirmed", async () => {
		const user = userEvent.setup();
		mocks.ssoEnabled = true;
		mocks.search = { tab: "general" };
		renderPage();

		await user.click(screen.getByRole("button", { name: "use-lan-address" }));
		await user.click(screen.getByRole("button", { name: "save.button" }));

		const dialog = await screen.findByRole("dialog", { name: "general.public-url.sso-confirm.title" });
		expect(mocks.saveSettings).not.toHaveBeenCalled();
		await user.click(within(dialog).getByRole("button", { name: "general.public-url.sso-confirm.confirm" }));

		await vi.waitFor(() => expect(mocks.saveSettings).toHaveBeenCalledTimes(1));
		expect(mocks.saveSettings.mock.calls[0][0]).toMatchObject({ publicUrl: "http://192.168.1.20:8080" });
	});

	it("does not save when the admin keeps single sign-on", async () => {
		const user = userEvent.setup();
		mocks.ssoEnabled = true;
		mocks.search = { tab: "general" };
		renderPage();

		await user.click(screen.getByRole("button", { name: "use-lan-address" }));
		await user.click(screen.getByRole("button", { name: "save.button" }));
		const dialog = await screen.findByRole("dialog", { name: "general.public-url.sso-confirm.title" });
		await user.click(within(dialog).getByRole("button", { name: "common:cancel" }));

		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
		expect(mocks.saveSettings).not.toHaveBeenCalled();
	});

	it("saves without asking when no provider is enabled", async () => {
		const user = userEvent.setup();
		mocks.search = { tab: "general" };
		renderPage();

		await user.click(screen.getByRole("button", { name: "use-lan-address" }));
		await user.click(screen.getByRole("button", { name: "save.button" }));

		await vi.waitFor(() => expect(mocks.saveSettings).toHaveBeenCalledTimes(1));
		expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
	});
});
