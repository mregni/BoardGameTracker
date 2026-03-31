import { cx } from "class-variance-authority";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import Check from "@/assets/icons/check.svg?react";
import Sparkles from "@/assets/icons/sparkles.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";

interface Props {
	invitedPlayers: GameNightRsvps[];
	onSubmit: (rsvpId: number, playerId: number, playerName: string, state: GameNightRsvpState) => void;
	isSubmitting: boolean;
}

const responseOptions = [
	{
		state: GameNightRsvpState.Accepted,
		labelKey: "rsvp:yes-ill-be-there",
		Icon: Check,
		active: "border-green-500 bg-green-500/20",
		inactive: "border-white/10 bg-white/5 hover:border-green-500/50",
		iconActive: "text-green-400",
		iconInactive: "text-gray-400",
	},
	{
		state: GameNightRsvpState.Pending,
		labelKey: "rsvp:maybe",
		Icon: Sparkles,
		active: "border-yellow-500 bg-yellow-500/20",
		inactive: "border-white/10 bg-white/5 hover:border-yellow-500/50",
		iconActive: "text-yellow-400",
		iconInactive: "text-gray-400",
	},
	{
		state: GameNightRsvpState.Declined,
		labelKey: "rsvp:cant-make-it",
		Icon: XIcon,
		active: "border-red-500 bg-red-500/20",
		inactive: "border-white/10 bg-white/5 hover:border-red-500/50",
		iconActive: "text-red-400",
		iconInactive: "text-gray-400",
	},
] as const;

export const RsvpResponseForm = ({ invitedPlayers, onSubmit, isSubmitting }: Props) => {
	const { t } = useTranslation("rsvp");
	const [selectedPlayerId, setSelectedPlayerId] = useState<number | null>(null);
	const [selectedResponse, setSelectedResponse] = useState<GameNightRsvpState | null>(null);

	const canSubmit = selectedPlayerId !== null && selectedResponse !== null && !isSubmitting;

	const handleSubmit = () => {
		if (selectedPlayerId === null || selectedResponse === null) return;
		const rsvp = invitedPlayers.find((p) => p.playerId === selectedPlayerId);
		if (!rsvp) return;
		onSubmit(rsvp.id, rsvp.playerId, rsvp.player.name, selectedResponse);
	};

	return (
		<BgtCard className="gap-3">
			<BgtText size="5" weight="bold">
				{t("your-response")}
			</BgtText>

			<BgtText size="2" weight="medium" className="mb-2">
				{t("who-are-you")}
			</BgtText>
			<select
				value={selectedPlayerId ?? ""}
				onChange={(e) => setSelectedPlayerId(e.target.value ? Number(e.target.value) : null)}
				className="w-full px-4 py-2.5 bg-input border border-primary/30 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent text-white text-sm"
			>
				<option value="" className="bg-input">
					{t("select-your-name")}
				</option>
				{invitedPlayers.map((rsvp) => (
					<option key={rsvp.playerId} value={rsvp.playerId} className="bg-input">
						{rsvp.player.name}
					</option>
				))}
			</select>

			<BgtText size="2" weight="medium" className="mb-2">
				{t("can-you-make-it")}
			</BgtText>
			<div className="grid grid-cols-1 md:grid-cols-3 gap-2.5">
				{responseOptions.map((option) => {
					const isActive = selectedResponse === option.state;
					return (
						<button
							key={option.state}
							onClick={() => setSelectedResponse(option.state)}
							className={cx(
								"p-3 rounded-lg border-2 transition-all cursor-pointer",
								isActive ? option.active : option.inactive,
							)}
						>
							<div className="flex items-center justify-center mb-1">
								<option.Icon className={cx("size-5", isActive ? option.iconActive : option.iconInactive)} />
							</div>
							<BgtText size="2" weight="medium" className="text-center">
								{t(option.labelKey)}
							</BgtText>
						</button>
					);
				})}
			</div>

			<BgtButton onClick={handleSubmit} disabled={!canSubmit} variant="primary" size="3" className="w-full">
				{t("submit-rsvp")}
			</BgtButton>
		</BgtCard>
	);
};
