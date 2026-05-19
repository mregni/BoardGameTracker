import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, userEvent } from "@/test/test-utils";

import { type SettingsCategory, SettingsSidebar } from "./SettingsSidebar";

let mockAuthStatus: { authEnabled: boolean } | null = null;

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: { authStatus: typeof mockAuthStatus }) => unknown) =>
		selector({ authStatus: mockAuthStatus }),
}));

const defaultProps = {
	activeCategory: "general" as SettingsCategory,
	onCategoryChange: vi.fn(),
	canManageSettings: true,
};

describe("SettingsSidebar", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockAuthStatus = { authEnabled: true };
	});

	describe("Rendering categories", () => {
		it("should render all categories when canManageSettings is true and auth is enabled", () => {
			render(<SettingsSidebar {...defaultProps} />);

			expect(screen.getByText("settings:sidebar.general.title")).toBeInTheDocument();
			expect(screen.getByText("settings:sidebar.shelf-of-shame.title")).toBeInTheDocument();
			expect(screen.getByText("settings:sidebar.game-nights.title")).toBeInTheDocument();
			expect(screen.getByText("settings:sidebar.advanced.title")).toBeInTheDocument();
			expect(screen.getByText("settings:sidebar.account.title")).toBeInTheDocument();
		});

		it("should render category descriptions", () => {
			render(<SettingsSidebar {...defaultProps} />);

			expect(screen.getByText("settings:sidebar.general.description")).toBeInTheDocument();
			expect(screen.getByText("settings:sidebar.shelf-of-shame.description")).toBeInTheDocument();
		});

		it("should render categories as buttons", () => {
			render(<SettingsSidebar {...defaultProps} />);

			const buttons = screen.getAllByRole("button");
			expect(buttons).toHaveLength(6);
		});
	});

	describe("Filtering categories", () => {
		it("should hide non-account categories when canManageSettings is false", () => {
			render(<SettingsSidebar {...defaultProps} canManageSettings={false} />);

			expect(screen.queryByText("settings:sidebar.general.title")).not.toBeInTheDocument();
			expect(screen.queryByText("settings:sidebar.shelf-of-shame.title")).not.toBeInTheDocument();
			expect(screen.queryByText("settings:sidebar.game-nights.title")).not.toBeInTheDocument();
			expect(screen.queryByText("settings:sidebar.advanced.title")).not.toBeInTheDocument();
		});

		it("should still show account tab when canManageSettings is false and auth is enabled", () => {
			render(<SettingsSidebar {...defaultProps} canManageSettings={false} />);

			expect(screen.getByText("settings:sidebar.account.title")).toBeInTheDocument();
		});

		it("should hide account tab when auth is disabled", () => {
			mockAuthStatus = { authEnabled: false };
			render(<SettingsSidebar {...defaultProps} />);

			expect(screen.queryByText("settings:sidebar.account.title")).not.toBeInTheDocument();
		});

		it("should hide account tab when authStatus is null", () => {
			mockAuthStatus = null;
			render(<SettingsSidebar {...defaultProps} />);

			expect(screen.queryByText("settings:sidebar.account.title")).not.toBeInTheDocument();
		});

		it("should show only account tab when canManageSettings is false and auth is enabled", () => {
			render(<SettingsSidebar {...defaultProps} canManageSettings={false} />);

			const buttons = screen.getAllByRole("button");
			expect(buttons).toHaveLength(1);
			expect(screen.getByText("settings:sidebar.account.title")).toBeInTheDocument();
		});

		it("should show no categories when canManageSettings is false and auth is disabled", () => {
			mockAuthStatus = { authEnabled: false };
			render(<SettingsSidebar {...defaultProps} canManageSettings={false} />);

			expect(screen.queryAllByRole("button")).toHaveLength(0);
		});
	});

	describe("Active category styling", () => {
		it("should apply active styles to the active category", () => {
			render(<SettingsSidebar {...defaultProps} activeCategory="general" />);

			const buttons = screen.getAllByRole("button");
			expect(buttons[0]).toHaveClass("bg-primary/20");
		});

		it("should not apply active styles to inactive categories", () => {
			render(<SettingsSidebar {...defaultProps} activeCategory="general" />);

			const buttons = screen.getAllByRole("button");
			expect(buttons[1]).not.toHaveClass("bg-primary/20");
			expect(buttons[1]).toHaveClass("text-white/70");
		});

		it("should apply active styles to a different category", () => {
			render(<SettingsSidebar {...defaultProps} activeCategory="advanced" />);

			const buttons = screen.getAllByRole("button");
			expect(buttons[4]).toHaveClass("bg-primary/20");
		});
	});

	describe("Category change handler", () => {
		it("should call onCategoryChange when a category is clicked", async () => {
			const user = userEvent.setup();
			const onCategoryChange = vi.fn();
			render(<SettingsSidebar {...defaultProps} onCategoryChange={onCategoryChange} />);

			await user.click(screen.getByText("settings:sidebar.shelf-of-shame.title"));

			expect(onCategoryChange).toHaveBeenCalledWith("shelf-of-shame");
		});

		it("should call onCategoryChange with correct id for each category", async () => {
			const user = userEvent.setup();
			const onCategoryChange = vi.fn();
			render(<SettingsSidebar {...defaultProps} onCategoryChange={onCategoryChange} />);

			await user.click(screen.getByText("settings:sidebar.game-nights.title"));
			expect(onCategoryChange).toHaveBeenCalledWith("game-nights");

			await user.click(screen.getByText("settings:sidebar.advanced.title"));
			expect(onCategoryChange).toHaveBeenCalledWith("advanced");

			await user.click(screen.getByText("settings:sidebar.account.title"));
			expect(onCategoryChange).toHaveBeenCalledWith("account");
		});

		it("should call onCategoryChange when clicking the already active category", async () => {
			const user = userEvent.setup();
			const onCategoryChange = vi.fn();
			render(<SettingsSidebar {...defaultProps} activeCategory="general" onCategoryChange={onCategoryChange} />);

			await user.click(screen.getByText("settings:sidebar.general.title"));

			expect(onCategoryChange).toHaveBeenCalledWith("general");
		});
	});
});
