import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import { BgtButton } from "@/components/BgtButton/BgtButton";
import type { ApiError } from "@/models";
import type { ChatExchange } from "../-hooks/useRagChat";
import { CitationList } from "./CitationList";

interface Props {
	exchange: ChatExchange;
	onRetry: () => void;
}

export const ChatMessage = ({ exchange, onRetry }: Props) => {
	const { t } = useTranslation(["chat", "error"]);

	const renderError = (error?: ApiError): string => {
		if (!error) {
			return t("error:something-went-wrong");
		}
		switch (error.kind) {
			case "network":
				return t("error:network");
			case "timeout":
				return t("error:timeout");
			case "server":
				return t("error:server");
			case "client":
				return error.message;
			default:
				return t("error:something-went-wrong");
		}
	};

	return (
		<div className="flex flex-col gap-2">
			<div className="flex justify-end">
				<div className="max-w-[85%] rounded-2xl rounded-br-sm bg-primary/60 text-white px-4 py-2">
					<span className="sr-only">{t("you")}: </span>
					<span className="whitespace-pre-wrap break-words">{exchange.question}</span>
				</div>
			</div>
			<div className="flex justify-start">
				<div className="max-w-[85%] rounded-2xl rounded-bl-sm bg-card text-white px-4 py-3">
					<span className="sr-only">{t("assistant")}: </span>
					{exchange.status === "pending" && (
						<div className="flex items-center gap-2 text-white/60">
							<Bars className="size-4 text-primary" />
							<span>{t("thinking")}</span>
						</div>
					)}
					{exchange.status === "error" && (
						<div className="flex flex-col items-start gap-2">
							<span className="text-error">{renderError(exchange.error)}</span>
							<BgtButton variant="text" size="1" onClick={onRetry}>
								{t("retry")}
							</BgtButton>
						</div>
					)}
					{exchange.status === "done" && exchange.answer && (
						<div className="flex flex-col gap-3">
							<span className="whitespace-pre-wrap break-words">{exchange.answer.answer}</span>
							{exchange.answer.citations.length > 0 && <CitationList citations={exchange.answer.citations} />}
						</div>
					)}
				</div>
			</div>
		</div>
	);
};
