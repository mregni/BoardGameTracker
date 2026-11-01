import * as Tooltip from "@radix-ui/react-tooltip";
import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Chat from "@/assets/icons/chat.svg?react";
import { BgtButton } from "@/components/BgtButton/BgtButton";

interface Props {
	gameId: number;
	disabled: boolean;
}

export const RulebookChatButton = ({ gameId, disabled }: Props) => {
	const { t } = useTranslation("chat");
	const navigate = useNavigate();

	if (!disabled) {
		return (
			<BgtButton
				variant="secondary"
				size="1"
				className="pointer-coarse:min-h-11"
				onClick={() => navigate({ to: "/chat", search: { gameId } })}
			>
				<Chat className="size-4" />
				{t("ask-button")}
			</BgtButton>
		);
	}

	return (
		<Tooltip.Provider delayDuration={150}>
			<Tooltip.Root>
				<Tooltip.Trigger asChild>
					{/* biome-ignore lint/a11y/useSemanticElements: a disabled native button cannot take focus to reveal the reason */}
					<span
						role="button"
						tabIndex={0}
						aria-disabled="true"
						aria-label={`${t("ask-button")}. ${t("ask-button-disabled")}`}
						className="inline-flex cursor-not-allowed"
					>
						<BgtButton
							variant="secondary"
							size="1"
							disabled
							aria-hidden="true"
							tabIndex={-1}
							className="pointer-events-none pointer-coarse:min-h-11"
						>
							<Chat className="size-4" />
							{t("ask-button")}
						</BgtButton>
					</span>
				</Tooltip.Trigger>
				<Tooltip.Portal>
					<Tooltip.Content
						sideOffset={5}
						className="select-none rounded-md border-2 border-card-border bg-card-black px-3 py-2 text-xs text-white shadow-lg"
					>
						{t("ask-button-disabled")}
						<Tooltip.Arrow className="fill-card-black" />
					</Tooltip.Content>
				</Tooltip.Portal>
			</Tooltip.Root>
		</Tooltip.Provider>
	);
};
