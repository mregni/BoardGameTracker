import { useTranslation } from "react-i18next";
import Check from "@/assets/icons/check.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightRsvpState } from "@/models";

interface Props {
	playerName: string;
	response: GameNightRsvpState;
	hostNotified: boolean;
	onChangeResponse: () => void;
}

const messages = {
	[GameNightRsvpState.Accepted]: "answered-accepted",
	[GameNightRsvpState.Pending]: "answered-maybe",
	[GameNightRsvpState.Declined]: "answered-declined",
} as const;

export const RsvpSuccessView = ({ playerName, response, hostNotified, onChangeResponse }: Props) => {
	const { t } = useTranslation("rsvp");

	return (
		<BgtCard className="gap-3" role="status">
			<div className="flex items-center gap-3">
				<div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-green-500/20">
					<Check className="size-5 text-green-400" />
				</div>
				<div className="flex min-w-0 flex-col gap-0.5">
					<BgtText size="3" weight="bold">
						{t(messages[response], { name: playerName })}
					</BgtText>
					{hostNotified && (
						<BgtText size="2" color="gray">
							{t("host-notified")}
						</BgtText>
					)}
				</div>
			</div>
			<BgtButton variant="cancel" className="self-start" onClick={onChangeResponse}>
				{t("change-response")}
			</BgtButton>
		</BgtCard>
	);
};
