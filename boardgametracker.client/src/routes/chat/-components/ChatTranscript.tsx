import { useEffect, useRef } from "react";
import type { ChatExchange } from "../-hooks/useRagChat";
import { ChatMessage } from "./ChatMessage";

interface Props {
	exchanges: ChatExchange[];
	isPending: boolean;
	emptyHint: string;
	onRetry: (exchange: ChatExchange) => void;
	onSelectSource: (exchangeId: string, index: number) => void;
	focused: { id: string; index: number } | null;
}

export const ChatTranscript = ({ exchanges, isPending, emptyHint, onRetry, onSelectSource, focused }: Props) => {
	const bottomRef = useRef<HTMLDivElement>(null);

	// biome-ignore lint/correctness/useExhaustiveDependencies: scroll to the newest message whenever the exchange list changes
	useEffect(() => {
		bottomRef.current?.scrollIntoView({ block: "end" });
	}, [exchanges]);

	if (exchanges.length === 0) {
		return <div className="flex h-full items-center justify-center text-sm text-white/40">{emptyHint}</div>;
	}

	return (
		<div role="log" aria-live="polite" aria-busy={isPending} className="mx-auto flex max-w-3xl flex-col gap-4 pb-4">
			{exchanges.map((exchange) => (
				<ChatMessage
					key={exchange.id}
					exchange={exchange}
					onRetry={() => onRetry(exchange)}
					onSelectSource={(index) => onSelectSource(exchange.id, index)}
					activeSourceIndex={focused?.id === exchange.id ? focused.index : undefined}
				/>
			))}
			<div ref={bottomRef} />
		</div>
	);
};
