import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ApiError, RagAnswer } from "@/models";

vi.mock("@/services/ragService", () => ({
	askRagCall: vi.fn(),
}));

import { askRagCall } from "@/services/ragService";
import { useRagChat } from "./useRagChat";

const askRagCallMock = vi.mocked(askRagCall);

const answer: RagAnswer = {
	answer: "Roll two dice and move.",
	hasContext: true,
	durationMs: 1234,
	citations: [],
};

const apiError: ApiError = {
	kind: "server",
	status: 500,
	message: "boom",
	url: "rag/game/1/ask",
};

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

const renderUseRagChat = () => renderHook(() => useRagChat(), { wrapper: createWrapper() });

afterEach(() => {
	vi.clearAllMocks();
});

describe("useRagChat", () => {
	it("adds a pending exchange and marks it done with the answer on success", async () => {
		askRagCallMock.mockResolvedValue(answer);
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "How do I win?", 5));

		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("done"));
		const exchange = result.current.getExchanges(1)[0];
		expect(exchange.question).toBe("How do I win?");
		expect(exchange.manualId).toBe(5);
		expect(exchange.answer).toEqual(answer);
		expect(exchange.error).toBeUndefined();
		expect(askRagCallMock).toHaveBeenCalledTimes(1);
		expect(askRagCallMock).toHaveBeenCalledWith(1, "How do I win?", 5);
	});

	it("exposes the exchange as pending while the request is in flight", async () => {
		askRagCallMock.mockImplementation(() => new Promise(() => {}));
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "How do I win?"));

		await waitFor(() => expect(result.current.isPending).toBe(true));
		const exchange = result.current.getExchanges(1)[0];
		expect(exchange.status).toBe("pending");
		expect(exchange.answer).toBeUndefined();
	});

	it("trims the question before storing and sending it", async () => {
		askRagCallMock.mockResolvedValue(answer);
		const { result } = renderUseRagChat();

		act(() => result.current.ask(2, "  spaced question  "));

		await waitFor(() => expect(result.current.getExchanges(2)[0]?.status).toBe("done"));
		expect(result.current.getExchanges(2)[0].question).toBe("spaced question");
		expect(askRagCallMock).toHaveBeenCalledWith(2, "spaced question", undefined);
	});

	it("ignores questions that are empty or whitespace only", () => {
		const { result } = renderUseRagChat();

		act(() => {
			result.current.ask(1, "");
			result.current.ask(1, "   ");
		});

		expect(result.current.getExchanges(1)).toEqual([]);
		expect(askRagCallMock).not.toHaveBeenCalled();
	});

	it("ignores a new ask while a request is pending", async () => {
		askRagCallMock.mockImplementation(() => new Promise(() => {}));
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "first"));
		await waitFor(() => expect(result.current.isPending).toBe(true));

		act(() => result.current.ask(1, "second"));

		expect(result.current.getExchanges(1)).toHaveLength(1);
		expect(result.current.getExchanges(1)[0].question).toBe("first");
		expect(askRagCallMock).toHaveBeenCalledTimes(1);
	});

	it("stores the api error on the exchange when the request fails with an ApiError", async () => {
		askRagCallMock.mockRejectedValue(apiError);
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "How do I win?"));

		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("error"));
		expect(result.current.getExchanges(1)[0].error).toEqual(apiError);
	});

	it("wraps a non ApiError failure in an unknown ApiError", async () => {
		askRagCallMock.mockRejectedValue(new Error("network down"));
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "How do I win?"));

		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("error"));
		expect(result.current.getExchanges(1)[0].error).toEqual({
			kind: "unknown",
			status: null,
			message: "Error: network down",
			url: undefined,
		});
	});

	it("retry resets the failed exchange to pending and clears the error", async () => {
		askRagCallMock.mockRejectedValueOnce(apiError);
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "How do I win?", 3));
		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("error"));
		const failed = result.current.getExchanges(1)[0];

		askRagCallMock.mockImplementation(() => new Promise(() => {}));
		act(() => result.current.retry(1, failed));

		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("pending"));
		expect(result.current.getExchanges(1)[0].error).toBeUndefined();
		expect(askRagCallMock).toHaveBeenLastCalledWith(1, "How do I win?", 3);
	});

	it("retry replaces the failed exchange with the answer instead of adding a new one", async () => {
		askRagCallMock.mockRejectedValueOnce(apiError).mockResolvedValueOnce(answer);
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "How do I win?"));
		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("error"));
		const failed = result.current.getExchanges(1)[0];

		act(() => result.current.retry(1, failed));

		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("done"));
		expect(result.current.getExchanges(1)).toHaveLength(1);
		expect(result.current.getExchanges(1)[0].id).toBe(failed.id);
		expect(result.current.getExchanges(1)[0].answer).toEqual(answer);
	});

	it("ignores retry while a request is pending", async () => {
		askRagCallMock.mockImplementation(() => new Promise(() => {}));
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "first"));
		await waitFor(() => expect(result.current.isPending).toBe(true));
		const pending = result.current.getExchanges(1)[0];

		act(() => result.current.retry(1, pending));

		expect(askRagCallMock).toHaveBeenCalledTimes(1);
	});

	it("keeps transcripts separate per game", async () => {
		askRagCallMock.mockResolvedValue(answer);
		const { result } = renderUseRagChat();

		act(() => result.current.ask(1, "question one"));
		await waitFor(() => expect(result.current.getExchanges(1)[0]?.status).toBe("done"));
		act(() => result.current.ask(2, "question two"));
		await waitFor(() => expect(result.current.getExchanges(2)[0]?.status).toBe("done"));

		expect(result.current.getExchanges(1).map((exchange) => exchange.question)).toEqual(["question one"]);
		expect(result.current.getExchanges(2).map((exchange) => exchange.question)).toEqual(["question two"]);
	});

	it("returns an empty list for an undefined or unknown game id", () => {
		const { result } = renderUseRagChat();

		expect(result.current.getExchanges(undefined)).toEqual([]);
		expect(result.current.getExchanges(99)).toEqual([]);
	});
});
