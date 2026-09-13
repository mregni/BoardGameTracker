import { beforeAll, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, userEvent } from "@/test/test-utils";
import type { ChatExchange } from "../-hooks/useRagChat";
import { ChatTranscript } from "./ChatTranscript";

const buildExchange = (overrides: Partial<ChatExchange> = {}): ChatExchange => ({
	id: "exchange-1",
	question: "How many cards do I draw?",
	status: "done",
	answer: { answer: "You draw two cards.", hasContext: true, durationMs: 1234, citations: [] },
	...overrides,
});

describe("ChatTranscript", () => {
	beforeAll(() => {
		Element.prototype.scrollIntoView = vi.fn();
	});

	it("should render the empty hint when there are no exchanges", () => {
		renderWithTheme(<ChatTranscript exchanges={[]} isPending={false} emptyHint="Ask anything" onRetry={vi.fn()} onSelectSource={vi.fn()} focused={null} />);

		expect(screen.getByText("Ask anything")).toBeInTheDocument();
		expect(screen.queryByRole("log")).not.toBeInTheDocument();
	});

	it("should render a message for every exchange inside a log region", () => {
		renderWithTheme(
			<ChatTranscript
				exchanges={[buildExchange(), buildExchange({ id: "exchange-2", question: "Who starts?" })]}
				isPending={false}
				emptyHint="Ask anything"
				onRetry={vi.fn()} onSelectSource={vi.fn()} focused={null}
			/>,
		);

		const log = screen.getByRole("log");
		expect(log).toHaveAttribute("aria-busy", "false");
		expect(screen.getByText("How many cards do I draw?")).toBeInTheDocument();
		expect(screen.getByText("Who starts?")).toBeInTheDocument();
		expect(screen.queryByText("Ask anything")).not.toBeInTheDocument();
	});

	it("should mark the log as busy while a question is pending", () => {
		renderWithTheme(
			<ChatTranscript
				exchanges={[buildExchange({ status: "pending", answer: undefined })]}
				isPending={true}
				emptyHint="Ask anything"
				onRetry={vi.fn()} onSelectSource={vi.fn()} focused={null}
			/>,
		);

		expect(screen.getByRole("log")).toHaveAttribute("aria-busy", "true");
	});

	it("should call onRetry with the failed exchange", async () => {
		const user = userEvent.setup();
		const onRetry = vi.fn();
		const failed = buildExchange({ id: "exchange-2", question: "Who starts?", status: "error", answer: undefined });
		renderWithTheme(
			<ChatTranscript
				exchanges={[buildExchange(), failed]}
				isPending={false}
				emptyHint="Ask anything"
				onRetry={onRetry} onSelectSource={vi.fn()} focused={null}
			/>,
		);

		await user.click(screen.getByRole("button", { name: "retry" }));

		expect(onRetry).toHaveBeenCalledTimes(1);
		expect(onRetry).toHaveBeenCalledWith(failed);
	});
});
