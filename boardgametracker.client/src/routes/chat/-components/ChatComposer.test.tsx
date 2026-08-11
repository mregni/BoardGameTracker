import { describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import { ChatComposer } from "./ChatComposer";

describe("ChatComposer", () => {
	const defaultProps = {
		disabled: false,
		pending: false,
		placeholder: "Ask a question",
		onSend: vi.fn(),
	};

	describe("Rendering", () => {
		it("should render textarea with placeholder", () => {
			renderWithTheme(<ChatComposer {...defaultProps} />);
			expect(screen.getByPlaceholderText("Ask a question")).toBeInTheDocument();
		});

		it("should render send button", () => {
			renderWithTheme(<ChatComposer {...defaultProps} />);
			expect(screen.getByRole("button", { name: "composer.send" })).toBeInTheDocument();
		});
	});

	describe("Send Button State", () => {
		it("should disable send button when input is empty", () => {
			renderWithTheme(<ChatComposer {...defaultProps} />);
			expect(screen.getByRole("button", { name: "composer.send" })).toBeDisabled();
		});

		it("should disable send button when input is only whitespace", async () => {
			const user = userEvent.setup();
			renderWithTheme(<ChatComposer {...defaultProps} />);

			await user.type(screen.getByRole("textbox"), "   ");

			expect(screen.getByRole("button", { name: "composer.send" })).toBeDisabled();
		});

		it("should enable send button when input has text", async () => {
			const user = userEvent.setup();
			renderWithTheme(<ChatComposer {...defaultProps} />);

			await user.type(screen.getByRole("textbox"), "How do I win?");

			expect(screen.getByRole("button", { name: "composer.send" })).not.toBeDisabled();
		});

		it("should disable send button when pending", async () => {
			const user = userEvent.setup();
			renderWithTheme(<ChatComposer {...defaultProps} pending={true} />);

			await user.type(screen.getByRole("textbox"), "How do I win?");

			expect(screen.getByRole("button", { name: "composer.send" })).toBeDisabled();
		});

		it("should disable textarea and send button when disabled", () => {
			renderWithTheme(<ChatComposer {...defaultProps} disabled={true} />);

			expect(screen.getByRole("textbox")).toBeDisabled();
			expect(screen.getByRole("button", { name: "composer.send" })).toBeDisabled();
		});
	});

	describe("Submitting", () => {
		it("should call onSend with trimmed value on button click", async () => {
			const user = userEvent.setup();
			const onSend = vi.fn();
			renderWithTheme(<ChatComposer {...defaultProps} onSend={onSend} />);

			await user.type(screen.getByRole("textbox"), "  How do I win?  ");
			await user.click(screen.getByRole("button", { name: "composer.send" }));

			expect(onSend).toHaveBeenCalledTimes(1);
			expect(onSend).toHaveBeenCalledWith("How do I win?");
		});

		it("should clear the textarea after sending", async () => {
			const user = userEvent.setup();
			renderWithTheme(<ChatComposer {...defaultProps} onSend={vi.fn()} />);
			const textarea = screen.getByRole("textbox");

			await user.type(textarea, "How do I win?");
			await user.click(screen.getByRole("button", { name: "composer.send" }));

			expect(textarea).toHaveValue("");
		});

		it("should call onSend when Enter is pressed", async () => {
			const user = userEvent.setup();
			const onSend = vi.fn();
			renderWithTheme(<ChatComposer {...defaultProps} onSend={onSend} />);

			await user.type(screen.getByRole("textbox"), "How do I win?{Enter}");

			expect(onSend).toHaveBeenCalledTimes(1);
			expect(onSend).toHaveBeenCalledWith("How do I win?");
		});

		it("should insert a newline instead of sending on Shift+Enter", async () => {
			const user = userEvent.setup();
			const onSend = vi.fn();
			renderWithTheme(<ChatComposer {...defaultProps} onSend={onSend} />);
			const textarea = screen.getByRole("textbox");

			await user.type(textarea, "line one{Shift>}{Enter}{/Shift}line two");

			expect(onSend).not.toHaveBeenCalled();
			expect(textarea).toHaveValue("line one\nline two");
		});

		it("should not call onSend on Enter when input is empty", async () => {
			const user = userEvent.setup();
			const onSend = vi.fn();
			renderWithTheme(<ChatComposer {...defaultProps} onSend={onSend} />);

			await user.type(screen.getByRole("textbox"), "{Enter}");

			expect(onSend).not.toHaveBeenCalled();
		});

		it("should not call onSend on Enter when input is only whitespace", async () => {
			const user = userEvent.setup();
			const onSend = vi.fn();
			renderWithTheme(<ChatComposer {...defaultProps} onSend={onSend} />);

			await user.type(screen.getByRole("textbox"), "   {Enter}");

			expect(onSend).not.toHaveBeenCalled();
		});

		it("should not call onSend on Enter when pending", async () => {
			const user = userEvent.setup();
			const onSend = vi.fn();
			renderWithTheme(<ChatComposer {...defaultProps} pending={true} onSend={onSend} />);

			await user.type(screen.getByRole("textbox"), "How do I win?{Enter}");

			expect(onSend).not.toHaveBeenCalled();
		});

		it("should keep the value when submit is blocked by pending", async () => {
			const user = userEvent.setup();
			renderWithTheme(<ChatComposer {...defaultProps} pending={true} onSend={vi.fn()} />);
			const textarea = screen.getByRole("textbox");

			await user.type(textarea, "How do I win?{Enter}");

			expect(textarea).toHaveValue("How do I win?");
		});
	});
});
