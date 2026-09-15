import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ExternalLogin, OidcProvider } from "@/models";
import { renderWithProviders, screen, userEvent } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	state: {} as {
		logins: ExternalLogin[];
		provider: OidcProvider | null;
		isLoading: boolean;
		unlink: ReturnType<typeof vi.fn>;
		isUnlinking: boolean;
		link: ReturnType<typeof vi.fn>;
		isLinking: boolean;
	},
}));

vi.mock("../-hooks/useExternalLogins", () => ({
	useExternalLogins: () => mocks.state,
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ dateFormat: "yyyy-MM-dd", timeFormat: "HH:mm", uiLanguage: "en-US" }),
	}),
}));

import { ExternalLoginsSection } from "./ExternalLoginsSection";

const provider: OidcProvider = { name: "keycloak", displayName: "Company login", iconUrl: null, buttonColor: null };
const login: ExternalLogin = {
	id: 9,
	provider: "keycloak",
	providerKey: "sub-1",
	providerDisplayName: "Jane Doe",
	linkedAt: new Date("2026-09-01T10:00:00Z"),
	lastUsedAt: null,
};

describe("ExternalLoginsSection", () => {
	beforeEach(() => {
		mocks.state = {
			logins: [],
			provider,
			isLoading: false,
			unlink: vi.fn(),
			isUnlinking: false,
			link: vi.fn(),
			isLinking: false,
		};
	});

	it("renders nothing when no provider exists and nothing is linked", () => {
		mocks.state.provider = null;
		const { container } = renderWithProviders(<ExternalLoginsSection />);

		expect(container).toBeEmptyDOMElement();
	});

	it("offers to link the provider when it is not linked yet", async () => {
		const user = userEvent.setup();
		renderWithProviders(<ExternalLoginsSection />);

		expect(screen.getByText("account.external-logins.none")).toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "account.external-logins.link" }));

		expect(mocks.state.link).toHaveBeenCalledWith("keycloak");
	});

	it("lists linked logins with the provider label and lets the user unlink", async () => {
		const user = userEvent.setup();
		mocks.state.logins = [login];
		renderWithProviders(<ExternalLoginsSection />);

		expect(screen.getByText("Company login")).toBeInTheDocument();
		expect(screen.getByText(/Jane Doe/)).toBeInTheDocument();
		expect(screen.queryByRole("button", { name: "account.external-logins.link" })).not.toBeInTheDocument();
		await user.click(screen.getByRole("button", { name: "account.external-logins.unlink" }));

		expect(mocks.state.unlink).toHaveBeenCalledWith(9);
	});
});
