import * as Tooltip from "@radix-ui/react-tooltip";
import { useId } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";

interface Props {
	hasEnded: boolean;
	hasRunningGames: boolean;
	onEnd: () => void;
	onReopen: () => void;
}

export const NightEndAction = (props: Props) => {
	const { hasEnded, hasRunningGames, onEnd, onReopen } = props;
	const { t } = useTranslation("game-nights");
	const hintId = useId();

	if (hasEnded) {
		return (
			<BgtButton variant="cancel" size="1" onClick={onReopen}>
				{t("end.reopen")}
			</BgtButton>
		);
	}

	if (!hasRunningGames) {
		return (
			<BgtButton variant="cancel" size="1" onClick={onEnd}>
				{t("end.button")}
			</BgtButton>
		);
	}

	return (
		<Tooltip.Provider delayDuration={150}>
			<Tooltip.Root>
				<Tooltip.Trigger asChild>
					<span className="inline-flex cursor-not-allowed" title={t("end.running-hint")}>
						<BgtButton variant="cancel" size="1" disabled aria-describedby={hintId} className="pointer-events-none">
							{t("end.button")}
						</BgtButton>
						<span id={hintId} className="sr-only">
							{t("end.running-hint")}
						</span>
					</span>
				</Tooltip.Trigger>
				<Tooltip.Portal>
					<Tooltip.Content
						sideOffset={5}
						className="select-none rounded-md border-2 border-card-border bg-card-black px-3 py-2 text-xs text-white shadow-lg"
					>
						{t("end.running-hint")}
						<Tooltip.Arrow className="fill-card-black" />
					</Tooltip.Content>
				</Tooltip.Portal>
			</Tooltip.Root>
		</Tooltip.Provider>
	);
};
