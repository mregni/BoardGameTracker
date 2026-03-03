import { useTranslation } from "react-i18next";
import Check from "@/assets/icons/check.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightRsvpState } from "@/models";

interface Props {
	playerName: string;
	response: GameNightRsvpState | null;
}

export const RsvpSuccessView = ({ playerName, response }: Props) => {
	const { t } = useTranslation();

	return (
		<div className="min-h-screen flex items-center justify-center p-4">
			<div className="max-w-md w-full">
				<BgtCard className="text-center">
					<div className="flex flex-col items-center gap-4">
						<div className="w-16 h-16 bg-green-500/20 rounded-full flex items-center justify-center">
							<Check className="size-8 text-green-400" />
						</div>

						<div className="space-y-2">
							<BgtText size="6" weight="bold" color="white">
								{t("rsvp.submitted-title")}
							</BgtText>
							<BgtText color="gray">{t("rsvp.submitted-thanks", { name: playerName })}</BgtText>
						</div>

						<div className="bg-primary/10 border border-primary/30 rounded-lg p-4 w-full">
							<BgtText size="2" color="white">
								{response === GameNightRsvpState.Accepted && t("rsvp.submitted-accepted")}
								{response === GameNightRsvpState.Declined && t("rsvp.submitted-declined")}
								{response === GameNightRsvpState.Pending && t("rsvp.submitted-maybe")}
							</BgtText>
						</div>
					</div>
				</BgtCard>
			</div>
		</div>
	);
};
