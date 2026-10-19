import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "@/hooks/useAuth";
import { useAppForm } from "@/hooks/form";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";
import { settingsFormOpts } from "../-utils/settingsFormOpts";
import { GeneralSettings } from "./GeneralSettings";

const mocks = vi.hoisted(() => ({
	providers: [] as { id: number; name: string; displayName: string; enabled: boolean }[],
}));

vi.mock("@/services/oidcAdminService", () => ({
	getOidcProvidersCall: () => Promise.resolve(mocks.providers),
	getOidcProviderConfigCall: vi.fn(),
	getOidcSetupCall: vi.fn(),
}));

const Harness = ({ publicUrl }: { publicUrl: string }) => {
	const form = useAppForm({
		...settingsFormOpts,
		defaultValues: { ...settingsFormOpts.defaultValues, publicUrl },
	});

	return <GeneralSettings form={form} languages={[]} disabled={false} overrides={{}} />;
};

const publicUrlInput = () => screen.getByPlaceholderText("general.public-url.placeholder");

describe("GeneralSettings", () => {
	beforeEach(() => {
		mocks.providers = [];
	});

	afterEach(() => {
		useAuth.setState({ authStatus: null, user: null });
	});

	it("groups language, currency and formats under one title without repeating it as a label", () => {
		renderWithProviders(<Harness publicUrl="" />);

		expect(screen.getByText("general.regional.title")).toBeInTheDocument();
		expect(screen.getByPlaceholderText("dd-MM-yyyy")).toBeInTheDocument();
		expect(screen.getByPlaceholderText("€")).toBeInTheDocument();
		expect(screen.queryByText("general.language.title")).not.toBeInTheDocument();
	});

	it("leaves the field empty when unset, with an https example as placeholder and a not-set hint", () => {
		renderWithProviders(<Harness publicUrl="" />);

		expect(publicUrlInput()).toHaveValue("");
		expect(screen.getByTestId("public-url-not-set")).toHaveTextContent("general.public-url.not-set");
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	it("drops the not-set hint once a URL is typed and warns about another host", async () => {
		const user = userEvent.setup();
		renderWithProviders(<Harness publicUrl="" />);

		await user.type(publicUrlInput(), "https://games.example.com");

		expect(screen.queryByTestId("public-url-not-set")).not.toBeInTheDocument();
		expect(screen.getByRole("alert")).toHaveTextContent("general.public-url.host-mismatch-warning");
	});

	it("shows neither hint nor warning for the address the app is opened on", () => {
		renderWithProviders(<Harness publicUrl={window.location.origin} />);

		expect(publicUrlInput()).toHaveValue(window.location.origin);
		expect(screen.queryByTestId("public-url-not-set")).not.toBeInTheDocument();
		expect(screen.queryByRole("alert")).not.toBeInTheDocument();
	});

	describe("with single sign-on enabled", () => {
		beforeEach(() => {
			useAuth.setState({
				authStatus: { authEnabled: true },
				user: { id: "1", username: "admin", displayName: null, roles: ["Admin"] },
			});
			mocks.providers = [{ id: 1, name: "keycloak", displayName: "Company login", enabled: true }];
		});

		it("warns that an http address stops single sign-on", async () => {
			renderWithProviders(<Harness publicUrl="http://192.168.1.20:8080" />);

			expect(await screen.findByText("general.public-url.sso-warning")).toBeInTheDocument();
		});

		it("does not warn for an https address", async () => {
			renderWithProviders(<Harness publicUrl="https://games.example.com" />);

			await screen.findByText("general.public-url.host-mismatch-warning");
			expect(screen.queryByText("general.public-url.sso-warning")).not.toBeInTheDocument();
		});

		it("warns when no public URL is set and the app is opened over http on a real host", async () => {
			vi.stubGlobal("location", { ...window.location, origin: "http://192.168.1.20:8080" });
			renderWithProviders(<Harness publicUrl="" />);

			expect(await screen.findByText("general.public-url.sso-warning")).toBeInTheDocument();
			vi.unstubAllGlobals();
		});

		it("does not warn when the provider is switched off", async () => {
			mocks.providers = [{ id: 1, name: "keycloak", displayName: "Company login", enabled: false }];
			renderWithProviders(<Harness publicUrl="http://192.168.1.20:8080" />);

			await new Promise((resolve) => setTimeout(resolve, 0));
			expect(screen.queryByText("general.public-url.sso-warning")).not.toBeInTheDocument();
		});
	});
});
