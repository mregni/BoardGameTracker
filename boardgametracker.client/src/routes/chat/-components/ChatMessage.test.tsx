import { describe, expect, it, vi } from "vitest";
import type { ApiError } from "@/models";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import type { ChatExchange } from "../-hooks/useRagChat";
import { ChatMessage } from "./ChatMessage";

const buildExchange = (overrides: Partial<ChatExchange> = {}): ChatExchange => ({
	id: "exchange-1",
	question: "How many cards do I draw?",
	status: "done",
	...overrides,
});

const buildError = (overrides: Partial<ApiError> = {}): ApiError => ({
	kind: "unknown",
	status: null,
	message: "Something broke",
	url: undefined,
	...overrides,
});

describe("ChatMessage", () => {
	describe("Question", () => {
		it("should render the question with a screen reader label", () => {
			renderWithTheme(<ChatMessage exchange={buildExchange()} onRetry={vi.fn()} onSelectSource={vi.fn()} />);

			expect(screen.getByText("How many cards do I draw?")).toBeInTheDocument();
			expect(screen.getByText(/you:/)).toBeInTheDocument();
			expect(screen.getByText(/assistant:/)).toBeInTheDocument();
		});
	});

	describe("Pending", () => {
		it("should show the thinking indicator", () => {
			renderWithTheme(<ChatMessage exchange={buildExchange({ status: "pending" })} onRetry={vi.fn()} onSelectSource={vi.fn()} />);

			expect(screen.getByText("thinking")).toBeInTheDocument();
			expect(screen.queryByRole("button", { name: "retry" })).not.toBeInTheDocument();
		});
	});

	describe("Error", () => {
		it("should show the generic error when no error is set", () => {
			renderWithTheme(<ChatMessage exchange={buildExchange({ status: "error" })} onRetry={vi.fn()} onSelectSource={vi.fn()} />);

			expect(screen.getByText("error:something-went-wrong")).toBeInTheDocument();
		});

		it("should show the network error", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({ status: "error", error: buildError({ kind: "network" }) })}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.getByText("error:network")).toBeInTheDocument();
		});

		it("should show the timeout error", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({ status: "error", error: buildError({ kind: "timeout" }) })}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.getByText("error:timeout")).toBeInTheDocument();
		});

		it("should show the server error", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({ status: "error", error: buildError({ kind: "server" }) })}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.getByText("error:server")).toBeInTheDocument();
		});

		it("should show the api message for client errors", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({
						status: "error",
						error: buildError({ kind: "client", message: "Manual not indexed yet" }),
					})}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.getByText("Manual not indexed yet")).toBeInTheDocument();
		});

		it("should show the generic error for unknown error kinds", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({ status: "error", error: buildError({ kind: "unknown" }) })}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.getByText("error:something-went-wrong")).toBeInTheDocument();
		});

		it("should call onRetry when the retry button is clicked", async () => {
			const user = userEvent.setup();
			const onRetry = vi.fn();
			renderWithTheme(
				<ChatMessage exchange={buildExchange({ status: "error" })} onRetry={onRetry} onSelectSource={vi.fn()} />,
			);

			await user.click(screen.getByRole("button", { name: "retry" }));

			expect(onRetry).toHaveBeenCalledTimes(1);
		});
	});

	describe("Done", () => {
		it("should render the answer text", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({
						status: "done",
						answer: { answer: "You draw two cards.", hasContext: true, durationMs: 1234, citations: [] },
					})}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.getByText("You draw two cards.")).toBeInTheDocument();
		});

		it("should not render sources when there are no citations", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({
						status: "done",
						answer: { answer: "You draw two cards.", hasContext: true, durationMs: 1234, citations: [] },
					})}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.queryByText("sources")).not.toBeInTheDocument();
		});

		it("should render sources when there are citations", () => {
			renderWithTheme(
				<ChatMessage
					exchange={buildExchange({
						status: "done",
						answer: {
							answer: "You draw two cards.",
							hasContext: true,
							durationMs: 1234,
							citations: [
								{ manualId: 1, title: "Base rules", page: 4, snippet: "Draw two", score: 0.8, imageUrl: null },
							],
						},
					})}
					onRetry={vi.fn()} onSelectSource={vi.fn()}
				/>,
			);

			expect(screen.getByText("sources")).toBeInTheDocument();
			expect(screen.getByText("page")).toBeInTheDocument();
		});

		it("should render an empty assistant bubble when done without an answer", () => {
			renderWithTheme(<ChatMessage exchange={buildExchange({ status: "done" })} onRetry={vi.fn()} onSelectSource={vi.fn()} />);

			expect(screen.queryByText("thinking")).not.toBeInTheDocument();
			expect(screen.queryByRole("button", { name: "retry" })).not.toBeInTheDocument();
			expect(screen.queryByText("sources")).not.toBeInTheDocument();
		});
	});
});
