import { QueryClient } from "@tanstack/react-query";
import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, renderWithProviders, screen, waitFor } from "@/test/test-utils";

interface CapturedRoute {
	component: FC;
	beforeLoad: (args: { context: { queryClient: { query: () => Promise<unknown> } } }) => Promise<void>;
}

const mocks = vi.hoisted(() => ({
	captured: {} as { config: CapturedRoute },
	redirect: vi.fn((options: unknown) => options),
	navigate: vi.fn(),
	errorToast: vi.fn(),
	upload: vi.fn(),
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: CapturedRoute) => {
		mocks.captured.config = config;
		return config;
	},
	redirect: mocks.redirect,
	useNavigate: () => mocks.navigate,
	useRouter: () => ({ history: { back: vi.fn() } }),
}));

vi.mock("react-i18next", () => ({ useTranslation: () => ({ t: (key: string) => key }) }));

vi.mock("@/utils/errorUtils", () => ({
	translateApiError: (message: string, fallback: string) => `translated ${message} ${fallback}`,
}));

vi.mock("@/hooks/usePermissions", () => ({
	usePermissions: () => ({ isAdmin: false, canWrite: true, canManageSettings: false }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: vi.fn(), errorToast: mocks.errorToast }),
}));

vi.mock("@/services/gameService", () => ({ uploadCsvImportCall: mocks.upload }));

vi.mock("@/services/queries/games", () => ({
	csvImportGamesKey: ["csv-games"],
	csvImportRejectedKey: ["csv-rejected"],
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"] }),
}));

await import("./start");

const csvFile = () => new File(["objectid\n13"], "collection.csv", { type: "text/csv" });

const selectFile = (file: File) =>
	fireEvent.change(screen.getByLabelText("import.start.csv.button"), { target: { files: [file] } });

describe("Import start page", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("sends the user to the games page when BGG is not configured", async () => {
		const queryClient = { query: () => Promise.resolve({ bggStatus: { isConfigured: false } }) };

		await expect(mocks.captured.config.beforeLoad({ context: { queryClient } })).rejects.toBeDefined();
		expect(mocks.redirect).toHaveBeenCalledWith({ to: "/games" });
	});

	it("stores the parsed rows and opens the CSV review screen after an upload", async () => {
		const setQueryData = vi.spyOn(QueryClient.prototype, "setQueryData");
		const games = [{ bggId: 13, title: "Catan" }];
		const rejected = [{ line: 3, title: "Broken", bggId: null, reason: "invalid-id" }];
		mocks.upload.mockResolvedValue({ games, rejected });
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		const file = csvFile();
		selectFile(file);

		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/import/csv" }));
		expect(mocks.upload.mock.calls[0][0]).toBe(file);
		expect(setQueryData).toHaveBeenCalledWith(["csv-games"], games);
		expect(setQueryData).toHaveBeenCalledWith(["csv-rejected"], rejected);
		setQueryData.mockRestore();
	});

	it("shows the translated server error when the upload is refused", async () => {
		mocks.upload.mockRejectedValue({ kind: "api", message: "error.import.csv-empty", status: 400 });
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		selectFile(csvFile());

		await waitFor(() =>
			expect(mocks.errorToast).toHaveBeenCalledWith("translated error.import.csv-empty games:import.csv.upload-failed"),
		);
		expect(mocks.navigate).not.toHaveBeenCalled();
	});

	it("falls back to the generic message for an unknown error", async () => {
		mocks.upload.mockRejectedValue(new Error("network down"));
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		selectFile(csvFile());

		await waitFor(() => expect(mocks.errorToast).toHaveBeenCalledWith("games:import.csv.upload-failed"));
	});

	it("disables the upload button while the file is uploading", async () => {
		mocks.upload.mockReturnValue(new Promise(() => {}));
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		selectFile(csvFile());

		expect(await screen.findByRole("button", { name: "import.start.csv.uploading" })).toBeDisabled();
	});

	it("does not upload when the file dialog is closed without a file", () => {
		const Component = mocks.captured.config.component;
		renderWithProviders(<Component />);

		fireEvent.change(screen.getByLabelText("import.start.csv.button"), { target: { files: [] } });

		expect(mocks.upload).not.toHaveBeenCalled();
	});
});
