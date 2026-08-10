import { useMutation } from "@tanstack/react-query";
import { useCallback, useState } from "react";
import { type ApiError, isApiError, type RagAnswer } from "@/models";
import { askRagCall } from "@/services/ragService";

export type ChatExchangeStatus = "pending" | "done" | "error";

export interface ChatExchange {
	id: string;
	question: string;
	manualId?: number;
	status: ChatExchangeStatus;
	answer?: RagAnswer;
	error?: ApiError;
}

interface AskVariables {
	gameId: number;
	exchangeId: string;
	question: string;
	manualId?: number;
}

const toApiError = (error: unknown): ApiError =>
	isApiError(error) ? error : { kind: "unknown", status: null, message: String(error), url: undefined };

export const useRagChat = () => {
	const [transcripts, setTranscripts] = useState<Map<number, ChatExchange[]>>(new Map());

	const patchExchange = useCallback((gameId: number, exchangeId: string, patch: Partial<ChatExchange>) => {
		setTranscripts((prev) => {
			const next = new Map(prev);
			const list = (next.get(gameId) ?? []).map((exchange) =>
				exchange.id === exchangeId ? { ...exchange, ...patch } : exchange,
			);
			next.set(gameId, list);
			return next;
		});
	}, []);

	const mutation = useMutation({
		mutationFn: (variables: AskVariables) => askRagCall(variables.gameId, variables.question, variables.manualId),
		onSuccess: (answer, variables) => {
			patchExchange(variables.gameId, variables.exchangeId, { status: "done", answer });
		},
		onError: (error, variables) => {
			patchExchange(variables.gameId, variables.exchangeId, { status: "error", error: toApiError(error) });
		},
	});

	const ask = useCallback(
		(gameId: number, question: string, manualId?: number) => {
			const trimmed = question.trim();
			if (trimmed.length === 0 || mutation.isPending) {
				return;
			}

			const exchangeId = crypto.randomUUID();
			setTranscripts((prev) => {
				const next = new Map(prev);
				const list = [
					...(next.get(gameId) ?? []),
					{ id: exchangeId, question: trimmed, manualId, status: "pending" as const },
				];
				next.set(gameId, list);
				return next;
			});
			mutation.mutate({ gameId, exchangeId, question: trimmed, manualId });
		},
		[mutation],
	);

	const retry = useCallback(
		(gameId: number, exchange: ChatExchange) => {
			if (mutation.isPending) {
				return;
			}
			patchExchange(gameId, exchange.id, { status: "pending", error: undefined });
			mutation.mutate({ gameId, exchangeId: exchange.id, question: exchange.question, manualId: exchange.manualId });
		},
		[mutation, patchExchange],
	);

	const getExchanges = useCallback(
		(gameId: number | undefined): ChatExchange[] => (gameId === undefined ? [] : (transcripts.get(gameId) ?? [])),
		[transcripts],
	);

	return { ask, retry, getExchanges, isPending: mutation.isPending };
};
