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
			<BgtButton size="1" onClick={() => navigate({ to: "/chat", search: { gameId } })}>
				<Chat className="size-4" />
				{t("ask-button")}
			</BgtButton>
		);
	}

	return (
		<Tooltip.Provider delayDuration={150}>
			<Tooltip.Root>
				<Tooltip.Trigger asChild>
					{/* biome-ignore lint/a11y/noNoninteractiveTabindex: focusable wrapper so keyboard users can reveal the disabled-reason tooltip */}
					<span tabIndex={0} className="inline-flex">
						<BgtButton size="1" disabled aria-disabled className="pointer-events-none">
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
